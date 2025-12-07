/**
 * myIoTGrid.Sensor - Configuration Manager Implementation
 */

#include "config_manager.h"

#ifdef PLATFORM_ESP32
#include <Preferences.h>
#include <WiFi.h>
Preferences preferences;
#endif

ConfigManager::ConfigManager()
    : _initialized(false) {
}

bool ConfigManager::init() {
#ifdef PLATFORM_ESP32
    _initialized = preferences.begin(NVS_NAMESPACE, false);
    if (_initialized) {
        Serial.println("[Config] NVS initialized");
    } else {
        Serial.println("[Config] NVS initialization failed");
    }
    return _initialized;
#else
    Serial.println("[Config] NVS simulated");
    _initialized = true;
    return true;
#endif
}

bool ConfigManager::saveConfig(const StoredConfig& config) {
    if (!_initialized) {
        Serial.println("[Config] Not initialized");
        return false;
    }

#ifdef PLATFORM_ESP32
    preferences.putString(KEY_NODE_ID, config.nodeId);
    preferences.putString(KEY_API_KEY, config.apiKey);
    preferences.putString(KEY_WIFI_SSID, config.wifiSsid);
    preferences.putString(KEY_WIFI_PASS, config.wifiPassword);
    preferences.putString(KEY_HUB_URL, config.hubApiUrl);
    preferences.putBool(KEY_CONFIGURED, true);
#endif

    Serial.printf("[Config] Saved configuration: NodeID=%s\n", config.nodeId.c_str());
    return true;
}

StoredConfig ConfigManager::loadConfig() {
    StoredConfig config;

    if (!_initialized) {
        Serial.println("[Config] Not initialized");
        return config;
    }

#ifdef PLATFORM_ESP32
    config.isValid = preferences.getBool(KEY_CONFIGURED, false);

    if (config.isValid) {
        config.nodeId = preferences.getString(KEY_NODE_ID, "");
        config.apiKey = preferences.getString(KEY_API_KEY, "");
        config.wifiSsid = preferences.getString(KEY_WIFI_SSID, "");
        config.wifiPassword = preferences.getString(KEY_WIFI_PASS, "");
        config.hubApiUrl = preferences.getString(KEY_HUB_URL, "");

        // Validate loaded config
        // WiFi SSID is required, Hub URL is optional (will be discovered via UDP)
        // apiKey is also optional - assigned after registration
        config.isValid = config.wifiSsid.length() > 0;

        if (config.isValid) {
            if (config.hubApiUrl.length() > 0) {
                Serial.printf("[Config] Loaded: NodeID=%s, HubURL=%s\n",
                              config.nodeId.c_str(), config.hubApiUrl.c_str());
            } else {
                Serial.printf("[Config] Loaded WiFi-only: NodeID=%s, SSID=%s (Hub will be discovered)\n",
                              config.nodeId.c_str(), config.wifiSsid.c_str());
            }
        } else {
            Serial.println("[Config] Invalid stored configuration (no WiFi SSID)");
        }
    } else {
        Serial.println("[Config] No stored configuration found");
    }
#else
    Serial.println("[Config] Simulated load - no config");
    config.isValid = false;
#endif

    return config;
}

bool ConfigManager::hasConfig() {
#ifdef PLATFORM_ESP32
    return _initialized && preferences.getBool(KEY_CONFIGURED, false);
#else
    return false;
#endif
}

bool ConfigManager::clearConfig() {
    if (!_initialized) {
        return false;
    }

#ifdef PLATFORM_ESP32
    preferences.clear();
#endif

    Serial.println("[Config] Configuration cleared");
    return true;
}

void ConfigManager::factoryReset() {
    Serial.println("[Config] Factory reset initiated...");
    clearConfig();

#ifdef PLATFORM_ESP32
    delay(1000);
    ESP.restart();
#endif
}

String ConfigManager::getSerial() {
#ifdef PLATFORM_ESP32
    uint8_t mac[6];
    WiFi.macAddress(mac);
    char serialBuf[24];
    // Use full 6-byte WiFi MAC address for unique sensor ID
    snprintf(serialBuf, sizeof(serialBuf), "ESP32-%02X%02X%02X%02X%02X%02X",
             mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);
    return String(serialBuf);
#else
    return String("SIM-00000000-0001");
#endif
}
