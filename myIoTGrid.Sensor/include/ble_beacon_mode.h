/**
 * myIoTGrid.Sensor - BLE Hybrid Mode
 * Combines:
 * 1. Beacon Mode: Broadcasts sensor data via advertising (no connection needed)
 * 2. GATT Services: For bidirectional config exchange (connection required)
 *
 * Sprint BT-01: Bluetooth Infrastructure
 */

#ifndef BLE_BEACON_MODE_H
#define BLE_BEACON_MODE_H

#include <Arduino.h>
#include "config.h"
#include <functional>

#ifdef PLATFORM_ESP32
#include <NimBLEDevice.h>
#endif

// GATT Service UUIDs for config exchange
// Using custom UUIDs to avoid conflicts
constexpr const char* CONFIG_SERVICE_UUID = "4d494f54-4752-4944-434f-4e4649470000";  // "MIOTGRIDCONFIG"
constexpr const char* CONFIG_WRITE_CHAR_UUID = "4d494f54-4752-4944-434f-4e4649470001"; // Hub writes config
constexpr const char* CONFIG_READ_CHAR_UUID = "4d494f54-4752-4944-434f-4e4649470002";  // Hub reads device info
constexpr const char* SENSOR_DATA_CHAR_UUID = "4d494f54-4752-4944-434f-4e4649470003";  // Hub reads sensor data

// Config command types
constexpr uint8_t CMD_AUTH = 0x00;          // Authenticate Hub (send node ID hash)
constexpr uint8_t CMD_SET_WIFI = 0x01;       // Set WiFi credentials
constexpr uint8_t CMD_SET_HUB_URL = 0x02;    // Set Hub API URL
constexpr uint8_t CMD_SET_NODE_ID = 0x03;    // Set Node ID
constexpr uint8_t CMD_SET_INTERVAL = 0x04;  // Set sensor interval
constexpr uint8_t CMD_FACTORY_RESET = 0xFE; // Factory reset
constexpr uint8_t CMD_REBOOT = 0xFF;        // Reboot sensor

// Response codes
constexpr uint8_t RESP_OK = 0x00;
constexpr uint8_t RESP_ERROR = 0x01;
constexpr uint8_t RESP_INVALID_CMD = 0x02;
constexpr uint8_t RESP_INVALID_DATA = 0x03;
constexpr uint8_t RESP_NOT_AUTHENTICATED = 0x04;  // Hub must authenticate first

/**
 * Sensor data structure for beacon transmission
 * Packed into BLE manufacturer data (max ~25 bytes in advertising packet)
 */
struct __attribute__((packed)) BeaconSensorData {
    uint16_t companyId;      // 0xFFFF = test/development
    uint8_t  deviceType;     // 0x01 = myIoTGrid sensor
    uint8_t  version;        // Protocol version (1)
    uint8_t  nodeIdHash[4];  // First 4 bytes of node ID hash
    int16_t  temperature;    // Temperature * 100 (e.g., 2150 = 21.50°C)
    uint16_t humidity;       // Humidity * 100 (e.g., 6500 = 65.00%)
    uint16_t pressure;       // Pressure - 50000 (e.g., 51325 = 101325 hPa)
    uint16_t battery;        // Battery mV (e.g., 3300 = 3.3V)
    uint8_t  flags;          // Bit flags for additional info
};

// Company ID for manufacturer data (0xFFFF = reserved for testing)
constexpr uint16_t MYIOTGRID_COMPANY_ID = 0xFFFF;
constexpr uint8_t  MYIOTGRID_DEVICE_TYPE = 0x01;
constexpr uint8_t  BEACON_PROTOCOL_VERSION = 0x01;

// Flags
constexpr uint8_t FLAG_HAS_GPS = 0x01;
constexpr uint8_t FLAG_LOW_BATTERY = 0x02;
constexpr uint8_t FLAG_ERROR = 0x04;
constexpr uint8_t FLAG_BONDED = 0x08;  // Device is bonded with a Hub

// Bonding Configuration
constexpr uint32_t BLE_PASSKEY = 123456;  // 6-digit passkey for pairing
constexpr uint8_t MAX_BONDED_DEVICES = 3; // Max number of bonded Hubs

/**
 * Config received callback type
 * Parameters: command, data, data_length
 */
using ConfigReceivedCallback = std::function<void(uint8_t cmd, const uint8_t* data, size_t len)>;

/**
 * BLE Hybrid Mode - combines beacon broadcasting with GATT services
 */
class BleBeaconMode {
public:
    BleBeaconMode();
    ~BleBeaconMode();

    /**
     * Initialize hybrid mode (beacon + GATT)
     * @param nodeId Node identifier for hashing
     * @return true if successful
     */
    bool init(const String& nodeId);

    /**
     * Update sensor values and restart advertising with new data
     * Call this periodically (e.g., every 60 seconds) with new readings
     */
    void updateSensorData(float temperature, float humidity, float pressure, uint16_t batteryMv);

    /**
     * Start advertising (both beacon data and GATT services)
     */
    void startAdvertising();

    /**
     * Stop advertising and disconnect clients
     */
    void stop();

    /**
     * Check if advertising
     */
    bool isAdvertising() const { return _advertising; }

    /**
     * Check if a client is connected
     */
    bool isConnected() const;

    /**
     * Get device name
     */
    String getDeviceName() const { return _deviceName; }

    /**
     * Get firmware version
     */
    String getFirmwareVersion() const { return FIRMWARE_VERSION; }

    /**
     * Set error flag
     */
    void setErrorFlag(bool error);

    /**
     * Set low battery flag
     */
    void setLowBatteryFlag(bool lowBattery);

    /**
     * Set callback for config received via GATT
     */
    void setConfigCallback(ConfigReceivedCallback callback);

    /**
     * Send response to Hub via GATT
     */
    void sendResponse(uint8_t responseCode, const uint8_t* data = nullptr, size_t len = 0);

    /**
     * Process loop - handle GATT events
     */
    void loop();

    /**
     * Check if device is bonded with any Hub
     */
    bool isBonded() const;

    /**
     * Get number of bonded devices
     */
    uint8_t getBondedDeviceCount() const;

    /**
     * Delete all bonding information (factory reset)
     */
    void deleteBonds();

    /**
     * Get passkey for display during pairing
     */
    uint32_t getPasskey() const { return BLE_PASSKEY; }

    /**
     * Check if current connection is authenticated
     * Hub must send CMD_AUTH with correct node ID hash first
     */
    bool isAuthenticated() const { return _authenticated; }

    /**
     * Reset authentication (called on disconnect)
     */
    void resetAuthentication() { _authenticated = false; }

    /**
     * Authenticate with node ID hash (4 bytes)
     * @return true if hash matches
     */
    bool authenticate(const uint8_t* hash, size_t len);

private:
    bool _authenticated = false;
    bool _initialized;
    bool _advertising;
    String _nodeId;
    String _deviceName;
    uint8_t _nodeIdHash[4];
    BeaconSensorData _sensorData;
    ConfigReceivedCallback _configCallback;

#ifdef PLATFORM_ESP32
    NimBLEAdvertising* _pAdvertising;
    NimBLEServer* _pServer;
    NimBLEService* _pConfigService;
    NimBLECharacteristic* _pConfigWriteChar;
    NimBLECharacteristic* _pConfigReadChar;
    NimBLECharacteristic* _pSensorDataChar;

    // Server callbacks
    class ServerCallbacks;
    class ConfigWriteCallbacks;
    class SecurityCallbacks;
    friend class ServerCallbacks;
    friend class ConfigWriteCallbacks;
    friend class SecurityCallbacks;
#endif

    void setupSecurity();
    void updateBondedFlag();

    void computeNodeIdHash(const String& nodeId);
    void updateAdvertisingData();
    void setupGattServices();
    void updateSensorDataCharacteristic();
};

#endif // BLE_BEACON_MODE_H
