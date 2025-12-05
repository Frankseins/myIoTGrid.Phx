/**
 * myIoTGrid.Sensor - Configuration Manager
 * Stores and retrieves node configuration from NVS (Non-Volatile Storage)
 */

#ifndef CONFIG_MANAGER_H
#define CONFIG_MANAGER_H

#include <Arduino.h>

/**
 * Stored node configuration
 */
struct StoredConfig {
    String nodeId;
    String apiKey;
    String wifiSsid;
    String wifiPassword;
    String hubApiUrl;
    bool isValid;

    StoredConfig() : isValid(false) {}
};

/**
 * Configuration Manager for persistent storage
 */
class ConfigManager {
public:
    ConfigManager();

    /**
     * Initialize NVS storage
     */
    bool init();

    /**
     * Save configuration to NVS
     */
    bool saveConfig(const StoredConfig& config);

    /**
     * Load configuration from NVS
     */
    StoredConfig loadConfig();

    /**
     * Check if configuration exists
     */
    bool hasConfig();

    /**
     * Clear all stored configuration
     */
    bool clearConfig();

    /**
     * Factory reset (clear config and reboot)
     */
    void factoryReset();

    /**
     * Get unique device serial number (derived from MAC address)
     */
    String getSerial();

private:
    static constexpr const char* NVS_NAMESPACE = "myiotgrid";

    // NVS keys
    static constexpr const char* KEY_NODE_ID = "node_id";
    static constexpr const char* KEY_API_KEY = "api_key";
    static constexpr const char* KEY_WIFI_SSID = "wifi_ssid";
    static constexpr const char* KEY_WIFI_PASS = "wifi_pass";
    static constexpr const char* KEY_HUB_URL = "hub_url";
    static constexpr const char* KEY_CONFIGURED = "configured";

    bool _initialized;
};

#endif // CONFIG_MANAGER_H
