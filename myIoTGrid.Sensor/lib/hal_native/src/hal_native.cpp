#ifdef PLATFORM_NATIVE

#include "hal/hal.h"
#include "config.h"

#include <iostream>
#include <fstream>
#include <sstream>
#include <chrono>
#include <thread>
#include <ctime>
#include <iomanip>
#include <cstdlib>
#include <sys/stat.h>

#include <curl/curl.h>
#include <uuid/uuid.h>

namespace {

// Start time for millis() calculation
auto startTime = std::chrono::steady_clock::now();

// CURL callback for writing response
size_t writeCallback(char* ptr, size_t size, size_t nmemb, std::string* data) {
    data->append(ptr, size * nmemb);
    return size * nmemb;
}

// Get current timestamp string for logging
std::string getTimestampStr() {
    auto now = std::chrono::system_clock::now();
    auto time = std::chrono::system_clock::to_time_t(now);
    auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(
        now.time_since_epoch()) % 1000;

    std::stringstream ss;
    ss << std::put_time(std::localtime(&time), "%Y-%m-%d %H:%M:%S");
    ss << '.' << std::setfill('0') << std::setw(3) << ms.count();
    return ss.str();
}

// Get storage file path for a key
std::string getStoragePath(const std::string& key) {
    return std::string(config::DATA_DIR) + "/" + key + ".dat";
}

// Ensure data directory exists
void ensureDataDir() {
    struct stat st;
    if (stat(config::DATA_DIR, &st) != 0) {
        mkdir(config::DATA_DIR, 0755);
    }
}

// Generate UUID string
std::string generateUUID() {
    uuid_t uuid;
    uuid_generate(uuid);
    char uuidStr[37];
    uuid_unparse_upper(uuid, uuidStr);
    return std::string(uuidStr);
}

} // anonymous namespace

namespace hal {

void init() {
    ensureDataDir();
    curl_global_init(CURL_GLOBAL_ALL);
    log_info("HAL Native initialized");
}

// ============================================
// Timing Functions
// ============================================

void delay_ms(uint32_t ms) {
    std::this_thread::sleep_for(std::chrono::milliseconds(ms));
}

uint32_t millis() {
    auto now = std::chrono::steady_clock::now();
    return std::chrono::duration_cast<std::chrono::milliseconds>(
        now - startTime).count();
}

uint64_t timestamp() {
    return std::chrono::duration_cast<std::chrono::seconds>(
        std::chrono::system_clock::now().time_since_epoch()).count();
}

// ============================================
// Device Identification
// ============================================

std::string get_device_serial() {
    // Try to load existing serial
    if (storage_exists(config::STORAGE_KEY_SERIAL)) {
        std::string serial = storage_load(config::STORAGE_KEY_SERIAL);
        if (!serial.empty()) {
            return serial;
        }
    }

    // Generate new serial
    std::string uuid = generateUUID();
    // Take first 8 chars of UUID and format as SIM-XXXXXXXX-0001
    std::string shortUuid = uuid.substr(0, 8);
    std::string serial = std::string(config::SERIAL_PREFIX_SIM) + shortUuid + "-0001";

    // Save for persistence
    storage_save(config::STORAGE_KEY_SERIAL, serial);

    log_info("Generated new serial: " + serial);
    return serial;
}

// ============================================
// Persistent Storage
// ============================================

bool storage_save(const std::string& key, const std::string& value) {
    ensureDataDir();
    std::string path = getStoragePath(key);
    std::ofstream file(path);
    if (!file.is_open()) {
        log_error("Failed to open file for writing: " + path);
        return false;
    }
    file << value;
    file.close();
    return true;
}

std::string storage_load(const std::string& key) {
    std::string path = getStoragePath(key);
    std::ifstream file(path);
    if (!file.is_open()) {
        return "";
    }
    std::stringstream buffer;
    buffer << file.rdbuf();
    return buffer.str();
}

bool storage_exists(const std::string& key) {
    std::string path = getStoragePath(key);
    struct stat st;
    return stat(path.c_str(), &st) == 0;
}

bool storage_delete(const std::string& key) {
    std::string path = getStoragePath(key);
    return remove(path.c_str()) == 0 || !storage_exists(key);
}

// ============================================
// Network
// ============================================

bool network_connect(const std::string& ssid, const std::string& password) {
    // Native always has network available
    (void)ssid;
    (void)password;
    log_info("Network: Native environment - network always available");
    return true;
}

bool network_is_connected() {
    // Native always has network
    return true;
}

std::string network_get_ip() {
    return "127.0.0.1";
}

// ============================================
// HTTP Client
// ============================================

HttpResponse http_post(const std::string& url, const std::string& json, uint32_t timeoutMs) {
    HttpResponse response;
    response.success = false;
    response.statusCode = 0;

    CURL* curl = curl_easy_init();
    if (!curl) {
        response.errorMessage = "Failed to initialize CURL";
        log_error(response.errorMessage);
        return response;
    }

    struct curl_slist* headers = nullptr;
    headers = curl_slist_append(headers, "Content-Type: application/json");
    headers = curl_slist_append(headers, "Accept: application/json");

    curl_easy_setopt(curl, CURLOPT_URL, url.c_str());
    curl_easy_setopt(curl, CURLOPT_POST, 1L);
    curl_easy_setopt(curl, CURLOPT_POSTFIELDS, json.c_str());
    curl_easy_setopt(curl, CURLOPT_HTTPHEADER, headers);
    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, writeCallback);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA, &response.body);
    curl_easy_setopt(curl, CURLOPT_TIMEOUT_MS, static_cast<long>(timeoutMs));
    curl_easy_setopt(curl, CURLOPT_CONNECTTIMEOUT_MS, static_cast<long>(timeoutMs / 2));

    CURLcode res = curl_easy_perform(curl);

    if (res != CURLE_OK) {
        response.errorMessage = curl_easy_strerror(res);
        log_error("HTTP POST failed: " + response.errorMessage);
    } else {
        curl_easy_getinfo(curl, CURLINFO_RESPONSE_CODE, &response.statusCode);
        response.success = (response.statusCode >= 200 && response.statusCode < 300);
    }

    curl_slist_free_all(headers);
    curl_easy_cleanup(curl);

    return response;
}

HttpResponse http_get(const std::string& url, uint32_t timeoutMs) {
    HttpResponse response;
    response.success = false;
    response.statusCode = 0;

    CURL* curl = curl_easy_init();
    if (!curl) {
        response.errorMessage = "Failed to initialize CURL";
        log_error(response.errorMessage);
        return response;
    }

    struct curl_slist* headers = nullptr;
    headers = curl_slist_append(headers, "Accept: application/json");

    curl_easy_setopt(curl, CURLOPT_URL, url.c_str());
    curl_easy_setopt(curl, CURLOPT_HTTPGET, 1L);
    curl_easy_setopt(curl, CURLOPT_HTTPHEADER, headers);
    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, writeCallback);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA, &response.body);
    curl_easy_setopt(curl, CURLOPT_TIMEOUT_MS, static_cast<long>(timeoutMs));
    curl_easy_setopt(curl, CURLOPT_CONNECTTIMEOUT_MS, static_cast<long>(timeoutMs / 2));

    CURLcode res = curl_easy_perform(curl);

    if (res != CURLE_OK) {
        response.errorMessage = curl_easy_strerror(res);
        log_error("HTTP GET failed: " + response.errorMessage);
    } else {
        curl_easy_getinfo(curl, CURLINFO_RESPONSE_CODE, &response.statusCode);
        response.success = (response.statusCode >= 200 && response.statusCode < 300);
    }

    curl_slist_free_all(headers);
    curl_easy_cleanup(curl);

    return response;
}

// ============================================
// Logging
// ============================================

void log_info(const std::string& message) {
    std::cout << "[" << getTimestampStr() << "] [INFO]  " << message << std::endl;
}

void log_warn(const std::string& message) {
    std::cout << "[" << getTimestampStr() << "] [WARN]  " << message << std::endl;
}

void log_error(const std::string& message) {
    std::cerr << "[" << getTimestampStr() << "] [ERROR] " << message << std::endl;
}

void log_debug(const std::string& message) {
#ifdef DEBUG
    std::cout << "[" << getTimestampStr() << "] [DEBUG] " << message << std::endl;
#else
    (void)message;
#endif
}

// ============================================
// System
// ============================================

uint32_t get_free_heap() {
    // Not really meaningful on native, return a large value
    return 1024 * 1024 * 100; // 100 MB
}

void restart() {
    log_info("Restart requested - exiting process");
    curl_global_cleanup();
    exit(0);
}

std::string get_env(const std::string& name, const std::string& defaultValue) {
    const char* value = std::getenv(name.c_str());
    return value ? std::string(value) : defaultValue;
}

} // namespace hal

#endif // PLATFORM_NATIVE
