#ifdef PLATFORM_ESP32

#include "hal/hal.h"
#include "config.h"

#include <Arduino.h>
#include <WiFi.h>
#include <HTTPClient.h>
#include <Preferences.h>

namespace {

// Preferences for persistent storage
Preferences preferences;
const char* PREF_NAMESPACE = "myiotgrid";

// WiFi connection status
bool wifiConnected = false;

// Serial number storage
String deviceSerial;

// Generate unique serial from ESP32 MAC address
String generateSerial() {
    uint64_t chipId = ESP.getEfuseMac();
    char serialBuf[24];
    snprintf(serialBuf, sizeof(serialBuf), "ESP-%08X-%04X",
             (uint32_t)(chipId >> 16),
             (uint16_t)(chipId & 0xFFFF));
    return String(serialBuf);
}

} // anonymous namespace

namespace hal {

void init() {
    // Initialize preferences
    preferences.begin(PREF_NAMESPACE, false);

    // Generate or load serial
    if (preferences.isKey(config::STORAGE_KEY_SERIAL)) {
        deviceSerial = preferences.getString(config::STORAGE_KEY_SERIAL, "");
    }

    if (deviceSerial.isEmpty()) {
        deviceSerial = generateSerial();
        preferences.putString(config::STORAGE_KEY_SERIAL, deviceSerial);
    }

    log_info("HAL ESP32 initialized");
}

// ============================================
// Timing Functions
// ============================================

void delay_ms(uint32_t ms) {
    delay(ms);
}

uint32_t millis() {
    return ::millis();
}

uint64_t timestamp() {
    // Get time from NTP if available, otherwise use millis
    struct timeval tv;
    gettimeofday(&tv, NULL);
    return tv.tv_sec;
}

// ============================================
// Device Identification
// ============================================

std::string get_device_serial() {
    if (deviceSerial.isEmpty()) {
        deviceSerial = generateSerial();
    }
    return std::string(deviceSerial.c_str());
}

// ============================================
// Persistent Storage
// ============================================

bool storage_save(const std::string& key, const std::string& value) {
    return preferences.putString(key.c_str(), value.c_str()) > 0;
}

std::string storage_load(const std::string& key) {
    String value = preferences.getString(key.c_str(), "");
    return std::string(value.c_str());
}

bool storage_exists(const std::string& key) {
    return preferences.isKey(key.c_str());
}

bool storage_delete(const std::string& key) {
    return preferences.remove(key.c_str());
}

// ============================================
// Network (WiFi)
// ============================================

bool network_connect(const std::string& ssid, const std::string& password) {
    if (ssid.empty()) {
        log_error("WiFi SSID is empty");
        return false;
    }

    log_info("Connecting to WiFi: " + ssid);

    WiFi.mode(WIFI_STA);
    WiFi.begin(ssid.c_str(), password.c_str());

    // Wait for connection (max 30 seconds)
    int attempts = 0;
    while (WiFi.status() != WL_CONNECTED && attempts < 60) {
        delay(500);
        Serial.print(".");
        attempts++;
    }
    Serial.println();

    if (WiFi.status() == WL_CONNECTED) {
        wifiConnected = true;
        log_info("WiFi connected!");
        log_info("IP address: " + network_get_ip());

        // Configure NTP for accurate timestamps
        configTime(0, 0, "pool.ntp.org", "time.nist.gov");

        return true;
    }

    log_error("WiFi connection failed");
    wifiConnected = false;
    return false;
}

bool network_is_connected() {
    return WiFi.status() == WL_CONNECTED;
}

std::string network_get_ip() {
    if (WiFi.status() != WL_CONNECTED) {
        return "";
    }
    return std::string(WiFi.localIP().toString().c_str());
}

// ============================================
// HTTP Client
// ============================================

HttpResponse http_post(const std::string& url, const std::string& json, uint32_t timeoutMs) {
    HttpResponse response;
    response.success = false;
    response.statusCode = 0;

    if (!network_is_connected()) {
        response.errorMessage = "WiFi not connected";
        return response;
    }

    HTTPClient http;
    http.setTimeout(timeoutMs);
    http.begin(url.c_str());
    http.addHeader("Content-Type", "application/json");
    http.addHeader("Accept", "application/json");

    int httpCode = http.POST(json.c_str());

    if (httpCode > 0) {
        response.statusCode = httpCode;
        response.body = std::string(http.getString().c_str());
        response.success = (httpCode >= 200 && httpCode < 300);
    } else {
        response.errorMessage = std::string(http.errorToString(httpCode).c_str());
    }

    http.end();
    return response;
}

HttpResponse http_get(const std::string& url, uint32_t timeoutMs) {
    HttpResponse response;
    response.success = false;
    response.statusCode = 0;

    if (!network_is_connected()) {
        response.errorMessage = "WiFi not connected";
        return response;
    }

    HTTPClient http;
    http.setTimeout(timeoutMs);
    http.begin(url.c_str());
    http.addHeader("Accept", "application/json");

    int httpCode = http.GET();

    if (httpCode > 0) {
        response.statusCode = httpCode;
        response.body = std::string(http.getString().c_str());
        response.success = (httpCode >= 200 && httpCode < 300);
    } else {
        response.errorMessage = std::string(http.errorToString(httpCode).c_str());
    }

    http.end();
    return response;
}

// ============================================
// Logging
// ============================================

void log_info(const std::string& message) {
    Serial.print("[INFO]  ");
    Serial.println(message.c_str());
}

void log_warn(const std::string& message) {
    Serial.print("[WARN]  ");
    Serial.println(message.c_str());
}

void log_error(const std::string& message) {
    Serial.print("[ERROR] ");
    Serial.println(message.c_str());
}

void log_debug(const std::string& message) {
#ifdef DEBUG
    Serial.print("[DEBUG] ");
    Serial.println(message.c_str());
#else
    (void)message;
#endif
}

// ============================================
// System
// ============================================

uint32_t get_free_heap() {
    return ESP.getFreeHeap();
}

void restart() {
    log_info("Restarting...");
    delay(100);
    ESP.restart();
}

std::string get_env(const std::string& name, const std::string& defaultValue) {
    // ESP32 doesn't have environment variables
    // Could read from preferences or compile-time defines
    (void)name;
    return defaultValue;
}

} // namespace hal

#endif // PLATFORM_ESP32
