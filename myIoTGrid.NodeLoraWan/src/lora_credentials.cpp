/**
 * @file lora_credentials.cpp
 * @brief LoRaWAN Credentials Management Implementation
 *
 * @version 1.0.0
 * @date 2025-12-10
 */

#ifdef PLATFORM_ESP32
#include <Arduino.h>
#endif

#include "lora_credentials.h"
#include "hal/hal.h"
#include "config.h"

// Include secrets if available (for compile-time credentials)
#if __has_include("secrets.h")
#include "secrets.h"
#define HAS_SECRETS 1
#else
#define HAS_SECRETS 0
#endif

#include <cstring>
#include <algorithm>
#include <cctype>

// ============================================================
// LORACREDENTIALS METHODS
// ============================================================

bool LoRaCredentials::hasOtaaCredentials() const {
    // Check if AppKey is non-zero (AppEUI can be all zeros for private networks)
    for (auto k : appKey) {
        if (k != 0) return true;
    }
    return false;
}

bool LoRaCredentials::hasAbpCredentials() const {
    // Check if DevAddr, NwkSKey, AppSKey are non-zero
    for (auto b : devAddr) if (b != 0) {
        for (auto k : nwkSKey) if (k != 0) {
            for (auto s : appSKey) if (s != 0) return true;
            break;
        }
        break;
    }
    return false;
}

std::string LoRaCredentials::getDevEuiString() const {
    char buf[17];
    snprintf(buf, sizeof(buf), "%02X%02X%02X%02X%02X%02X%02X%02X",
             devEui[0], devEui[1], devEui[2], devEui[3],
             devEui[4], devEui[5], devEui[6], devEui[7]);
    return std::string(buf);
}

std::string LoRaCredentials::getAppEuiString() const {
    char buf[17];
    snprintf(buf, sizeof(buf), "%02X%02X%02X%02X%02X%02X%02X%02X",
             appEui[0], appEui[1], appEui[2], appEui[3],
             appEui[4], appEui[5], appEui[6], appEui[7]);
    return std::string(buf);
}

void LoRaCredentials::clear() {
    devEui.fill(0);
    appEui.fill(0);
    appKey.fill(0);
    devAddr.fill(0);
    nwkSKey.fill(0);
    appSKey.fill(0);
    frameCounterUp = 0;
    frameCounterDown = 0;
}

// ============================================================
// CREDENTIALMANAGER CONSTRUCTOR
// ============================================================

CredentialManager::CredentialManager() {
    credentials_.clear();
}

// ============================================================
// INITIALIZATION
// ============================================================

bool CredentialManager::init() {
    if (initialized_) return true;

    LOG_INFO("Initializing credential manager...");

    // Generate DevEUI from MAC
    if (!generateDevEui()) {
        LOG_ERROR("Failed to generate DevEUI");
        return false;
    }

    // Try to load stored credentials from NVS first
    if (loadFromNvs()) {
        LOG_INFO("Loaded credentials from NVS");
    } else {
        LOG_INFO("No stored credentials in NVS");

        // Try to load from secrets.h (compile-time credentials)
        if (loadFromSecrets()) {
            LOG_INFO("Loaded credentials from secrets.h");
            // Auto-save to NVS for persistence
            saveToNvs();
        } else {
            LOG_WARN("No credentials configured");
        }
    }

    // Load frame counters
    loadFrameCounters();

    initialized_ = true;
    return true;
}

// ============================================================
// DEVEUI GENERATION
// ============================================================

bool CredentialManager::generateDevEui() {
    uint8_t mac[6];
    hal::get_device_id(mac);

    // Create EUI-64 from 48-bit MAC
    // Format: MAC[0-2] + 0xFF + 0xFE + MAC[3-5]
    credentials_.devEui[0] = mac[0];
    credentials_.devEui[1] = mac[1];
    credentials_.devEui[2] = mac[2];
    credentials_.devEui[3] = 0xFF;
    credentials_.devEui[4] = 0xFE;
    credentials_.devEui[5] = mac[3];
    credentials_.devEui[6] = mac[4];
    credentials_.devEui[7] = mac[5];

    LOG_INFO("Generated DevEUI: %s", credentials_.getDevEuiString().c_str());
    return true;
}

// ============================================================
// CREDENTIAL SETTERS
// ============================================================

bool CredentialManager::setDevEui(const std::string& hexString) {
    if (hexString.length() != 16) {
        LOG_ERROR("Invalid DevEUI length (expected 16 hex chars)");
        return false;
    }

    uint8_t parsed[8];
    if (!parseHexString(hexString, parsed, 8)) {
        LOG_ERROR("Invalid DevEUI hex string");
        return false;
    }

    std::copy(parsed, parsed + 8, credentials_.devEui.begin());
    return true;
}

bool CredentialManager::setAppEui(const std::string& hexString) {
    if (hexString.length() != 16) {
        LOG_ERROR("Invalid AppEUI length (expected 16 hex chars)");
        return false;
    }

    uint8_t parsed[8];
    if (!parseHexString(hexString, parsed, 8)) {
        LOG_ERROR("Invalid AppEUI hex string");
        return false;
    }

    std::copy(parsed, parsed + 8, credentials_.appEui.begin());
    LOG_INFO("AppEUI set: %s", credentials_.getAppEuiString().c_str());
    return true;
}

void CredentialManager::setAppEui(const uint8_t* eui) {
    std::copy(eui, eui + 8, credentials_.appEui.begin());
}

bool CredentialManager::setAppKey(const std::string& hexString) {
    if (hexString.length() != 32) {
        LOG_ERROR("Invalid AppKey length (expected 32 hex chars)");
        return false;
    }

    uint8_t parsed[16];
    if (!parseHexString(hexString, parsed, 16)) {
        LOG_ERROR("Invalid AppKey hex string");
        return false;
    }

    std::copy(parsed, parsed + 16, credentials_.appKey.begin());
    LOG_INFO("AppKey set (hidden)");
    return true;
}

void CredentialManager::setAppKey(const uint8_t* key) {
    std::copy(key, key + 16, credentials_.appKey.begin());
}

void CredentialManager::setAbpCredentials(const uint8_t* devAddr, const uint8_t* nwkSKey, const uint8_t* appSKey) {
    std::copy(devAddr, devAddr + 4, credentials_.devAddr.begin());
    std::copy(nwkSKey, nwkSKey + 16, credentials_.nwkSKey.begin());
    std::copy(appSKey, appSKey + 16, credentials_.appSKey.begin());
    LOG_INFO("ABP credentials set");
}

// ============================================================
// SECRETS.H LOADING (Compile-time credentials)
// ============================================================

bool CredentialManager::loadFromSecrets() {
#if HAS_SECRETS
    bool hasAppKey = false;

    // Load DevEUI from secrets.h (overrides MAC-based generation)
    #ifdef LORAWAN_DEV_EUI
    if (setDevEui(LORAWAN_DEV_EUI)) {
        LOG_INFO("DevEUI loaded from secrets.h: %s", credentials_.getDevEuiString().c_str());
    }
    #endif

    // Load AppEUI from secrets.h
    #ifdef LORAWAN_APP_EUI
    if (setAppEui(LORAWAN_APP_EUI)) {
        LOG_INFO("AppEUI loaded from secrets.h");
    }
    #endif

    // Load AppKey from secrets.h
    #ifdef LORAWAN_APP_KEY
    if (setAppKey(LORAWAN_APP_KEY)) {
        hasAppKey = true;
        LOG_INFO("AppKey loaded from secrets.h");
    }
    #endif

    return hasAppKey;
#else
    return false;
#endif
}

// ============================================================
// NVS PERSISTENCE
// ============================================================

bool CredentialManager::loadFromNvs() {
    bool success = true;

    // Load AppEUI
    if (hal::storage_exists(NvsKeys::APP_EUI)) {
        size_t read = hal::storage_load(NvsKeys::APP_EUI, credentials_.appEui.data(), 8);
        if (read != 8) success = false;
    } else {
        success = false;
    }

    // Load AppKey
    if (hal::storage_exists(NvsKeys::APP_KEY)) {
        size_t read = hal::storage_load(NvsKeys::APP_KEY, credentials_.appKey.data(), 16);
        if (read != 16) success = false;
    } else {
        success = false;
    }

    return success;
}

bool CredentialManager::saveToNvs() {
    bool success = true;

    if (!hal::storage_save(NvsKeys::APP_EUI, credentials_.appEui.data(), 8)) {
        LOG_ERROR("Failed to save AppEUI");
        success = false;
    }

    if (!hal::storage_save(NvsKeys::APP_KEY, credentials_.appKey.data(), 16)) {
        LOG_ERROR("Failed to save AppKey");
        success = false;
    }

    if (success) {
        LOG_INFO("Credentials saved to NVS");
    }

    return success;
}

bool CredentialManager::clearNvs() {
    hal::storage_delete(NvsKeys::APP_EUI);
    hal::storage_delete(NvsKeys::APP_KEY);
    hal::storage_delete(NvsKeys::FRAME_COUNTER);
    LOG_INFO("Credentials cleared from NVS");
    return true;
}

// ============================================================
// FRAME COUNTER MANAGEMENT
// ============================================================

bool CredentialManager::saveFrameCounters() {
    uint32_t counters[2] = {
        credentials_.frameCounterUp,
        credentials_.frameCounterDown
    };

    if (!hal::storage_save(NvsKeys::FRAME_COUNTER, counters, sizeof(counters))) {
        LOG_WARN("Failed to save frame counters");
        return false;
    }

    LOG_DEBUG("Frame counters saved (up=%u, down=%u)",
              credentials_.frameCounterUp, credentials_.frameCounterDown);
    return true;
}

bool CredentialManager::loadFrameCounters() {
    uint32_t counters[2] = {0, 0};

    if (!hal::storage_exists(NvsKeys::FRAME_COUNTER)) {
        return false;
    }

    size_t read = hal::storage_load(NvsKeys::FRAME_COUNTER, counters, sizeof(counters));
    if (read != sizeof(counters)) {
        return false;
    }

    credentials_.frameCounterUp = counters[0];
    credentials_.frameCounterDown = counters[1];

    LOG_INFO("Frame counters loaded (up=%u, down=%u)",
             credentials_.frameCounterUp, credentials_.frameCounterDown);
    return true;
}

uint32_t CredentialManager::incrementFrameCounterUp() {
    return ++credentials_.frameCounterUp;
}

void CredentialManager::setFrameCounterDown(uint32_t counter) {
    credentials_.frameCounterDown = counter;
}

// ============================================================
// SERIAL CONFIGURATION
// ============================================================

void CredentialManager::handleSerialConfig() {
    if (hal::serial_available() <= 0) return;

    char buffer[128];
    size_t len = hal::serial_read_line(buffer, sizeof(buffer), 100);
    if (len == 0) return;

    std::string line(buffer);

    // Remove whitespace
    line.erase(std::remove_if(line.begin(), line.end(), ::isspace), line.end());

    // Convert to uppercase
    std::transform(line.begin(), line.end(), line.begin(), ::toupper);

    // Parse commands
    if (line.rfind("APPEUI=", 0) == 0) {
        std::string value = line.substr(7);
        if (setAppEui(value)) {
            hal::serial_println("AppEUI set successfully");
        } else {
            hal::serial_println("Error: Invalid AppEUI");
        }
    }
    else if (line.rfind("APPKEY=", 0) == 0) {
        std::string value = line.substr(7);
        if (setAppKey(value)) {
            hal::serial_println("AppKey set successfully");
        } else {
            hal::serial_println("Error: Invalid AppKey");
        }
    }
    else if (line == "SAVE") {
        if (saveToNvs()) {
            hal::serial_println("Credentials saved");
        } else {
            hal::serial_println("Error: Save failed");
        }
    }
    else if (line == "SHOW") {
        printCredentials();
    }
    else if (line == "CLEAR") {
        clearNvs();
        credentials_.clear();
        generateDevEui();
        hal::serial_println("Credentials cleared");
    }
    else if (line == "HELP") {
        hal::serial_println("Commands:");
        hal::serial_println("  APPEUI=<16 hex chars>");
        hal::serial_println("  APPKEY=<32 hex chars>");
        hal::serial_println("  SAVE - Save to flash");
        hal::serial_println("  SHOW - Show credentials");
        hal::serial_println("  CLEAR - Clear all");
        hal::serial_println("  HELP - This help");
    }
    else if (!line.empty()) {
        hal::serial_println("Unknown command. Type HELP for help.");
    }
}

void CredentialManager::printCredentials() const {
    hal::serial_println("=== LoRaWAN Credentials ===");

    char buf[64];
    snprintf(buf, sizeof(buf), "DevEUI: %s", credentials_.getDevEuiString().c_str());
    hal::serial_println(buf);

    snprintf(buf, sizeof(buf), "AppEUI: %s", credentials_.getAppEuiString().c_str());
    hal::serial_println(buf);

    // Show AppKey partially masked
    snprintf(buf, sizeof(buf), "AppKey: %02X%02X...%02X%02X (masked)",
             credentials_.appKey[0], credentials_.appKey[1],
             credentials_.appKey[14], credentials_.appKey[15]);
    hal::serial_println(buf);

    snprintf(buf, sizeof(buf), "OTAA Ready: %s",
             isReadyForOtaa() ? "Yes" : "No");
    hal::serial_println(buf);

    snprintf(buf, sizeof(buf), "Frame Counter: %u",
             credentials_.frameCounterUp);
    hal::serial_println(buf);

    hal::serial_println("===========================");
}

// ============================================================
// VALIDATION
// ============================================================

bool CredentialManager::isReadyForOtaa() const {
    return credentials_.hasOtaaCredentials();
}

bool CredentialManager::isReadyForAbp() const {
    return credentials_.hasAbpCredentials();
}

// ============================================================
// UTILITY FUNCTIONS
// ============================================================

bool CredentialManager::parseHexString(const std::string& hex, uint8_t* output, size_t length) {
    if (hex.length() != length * 2) return false;

    for (size_t i = 0; i < length; i++) {
        char high = hex[i * 2];
        char low = hex[i * 2 + 1];

        if (!std::isxdigit(high) || !std::isxdigit(low)) {
            return false;
        }

        auto hexCharToNibble = [](char c) -> uint8_t {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return 0;
        };

        output[i] = (hexCharToNibble(high) << 4) | hexCharToNibble(low);
    }

    return true;
}

std::string CredentialManager::toHexString(const uint8_t* data, size_t length) {
    std::string result;
    result.reserve(length * 2);

    for (size_t i = 0; i < length; i++) {
        char buf[3];
        snprintf(buf, sizeof(buf), "%02X", data[i]);
        result += buf;
    }

    return result;
}

void CredentialManager::reverseBytes(uint8_t* data, size_t length) {
    for (size_t i = 0; i < length / 2; i++) {
        std::swap(data[i], data[length - 1 - i]);
    }
}
