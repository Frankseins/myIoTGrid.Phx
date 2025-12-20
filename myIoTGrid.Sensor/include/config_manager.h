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
    String targetMode;   // "local" or "cloud" - determines discovery behavior
    String tenantId;     // GUID from Hub/Cloud for multi-tenant support
    bool isValid;

    StoredConfig() : targetMode("local"), isValid(false) {}

    // Helper to check if cloud mode is enabled
    bool isCloudMode() const { return targetMode == "cloud"; }

    // Helper to check if Bluetooth sensor mode is enabled (no WiFi, BLE data only)
    bool isBluetoothMode() const { return targetMode == "bluetooth"; }
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
    static constexpr const char* KEY_TARGET_MODE = "target_mode";
    static constexpr const char* KEY_TENANT_ID = "tenant_id";
    static constexpr const char* KEY_CONFIGURED = "configured";

    bool _initialized;
};

#endif // CONFIG_MANAGER_H
