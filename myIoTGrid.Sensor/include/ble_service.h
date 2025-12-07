/**
 * myIoTGrid.Sensor - BLE Provisioning Service
 * Bluetooth Low Energy service for node provisioning
 *
 * Uses NimBLE for ESP32 BLE stack.
 * Provides characteristics for:
 * - Node registration (MAC address, firmware version)
 * - WiFi configuration (SSID, password)
 * - API configuration (node ID, API key, Hub URL)
 */

#ifndef BLE_SERVICE_H
#define BLE_SERVICE_H

#include <Arduino.h>
#include <functional>

#ifdef PLATFORM_ESP32
#include <NimBLEDevice.h>
#endif

/**
 * Node configuration received via BLE
 */
struct BLEConfig {
    String nodeId;
    String apiKey;
    String wifiSsid;
    String wifiPassword;
    String hubApiUrl;
    bool isValid;

    BLEConfig() : isValid(false) {}
};

/**
 * Callbacks for BLE events
 */
using OnBLEConfigReceived = std::function<void(const BLEConfig& config)>;
using OnPairingStarted = std::function<void()>;
using OnPairingCompleted = std::function<void()>;

/**
 * BLE Provisioning Service for Node Setup
 * Named to avoid conflict with NimBLE's BLEService macro
 */
class BLEProvisioningService {
public:
    BLEProvisioningService();
    ~BLEProvisioningService();

    /**
     * Initialize BLE with device name
     */
    bool init(const String& deviceName);

    /**
     * Start advertising for pairing
     */
    void startAdvertising();

    /**
     * Stop advertising and deinit BLE
     */
    void stop();

    /**
     * Process BLE events (call in loop)
     */
    void loop();

    /**
     * Check if device is connected
     */
    bool isConnected() const;

    /**
     * Check if advertising
     */
    bool isAdvertising() const;

    /**
     * Check if BLE is initialized
     */
    bool isInitialized() const { return _initialized; }

    /**
     * Check if WiFi credentials are pending (waiting for API config)
     */
    bool hasWifiPending() const {
        return _pendingConfig.wifiSsid.length() > 0 &&
               _pendingConfig.wifiPassword.length() > 0 &&
               _pendingConfig.hubApiUrl.length() == 0;
    }

    /**
     * Stop BLE for WPS mode (doesn't deinit, just stops advertising)
     */
    void stopForWPS();

    /**
     * Get MAC address
     */
    String getMacAddress() const;

    /**
     * Set callback for configuration received
     */
    void setConfigCallback(OnBLEConfigReceived callback);

    /**
     * Set callback for pairing started
     */
    void setPairingStartedCallback(OnPairingStarted callback);

    /**
     * Set callback for pairing completed
     */
    void setPairingCompletedCallback(OnPairingCompleted callback);

    /**
     * Set firmware version and update registration characteristic
     * Call this after init() to set the registration data
     */
    void setFirmwareVersion(const String& firmwareVersion);

    /**
     * Send registration notification to connected client
     * (node_id, mac_address, firmware_version)
     */
    void sendRegistration(const String& macAddress, const String& firmwareVersion);

    /**
     * Get the generated Node ID (ESP32-<WiFi-MAC>)
     */
    String getNodeId() const { return "ESP32-" + _macAddress; }

    /**
     * Finalize WiFi-only configuration (no Hub URL)
     * Call this after timeout if no API config is received
     */
    void finalizeWifiOnlyConfig();

    /**
     * Service UUID for myIoTGrid Node Provisioning
     */
    static constexpr const char* SERVICE_UUID = "4fafc201-1fb5-459e-8fcc-c5c9c331914b";

    /**
     * Characteristic UUIDs
     */
    static constexpr const char* CHAR_REGISTRATION_UUID = "beb5483e-36e1-4688-b7f5-ea07361b26a8";
    static constexpr const char* CHAR_WIFI_CONFIG_UUID = "beb5483e-36e1-4688-b7f5-ea07361b26a9";
    static constexpr const char* CHAR_API_CONFIG_UUID = "beb5483e-36e1-4688-b7f5-ea07361b26aa";
    static constexpr const char* CHAR_STATUS_UUID = "beb5483e-36e1-4688-b7f5-ea07361b26ab";

private:
#ifdef PLATFORM_ESP32
    NimBLEServer* _server;
    NimBLEService* _nimbleService;
    NimBLECharacteristic* _regChar;
    NimBLECharacteristic* _wifiChar;
    NimBLECharacteristic* _apiChar;
    NimBLECharacteristic* _statusChar;
    NimBLEAdvertising* _advertising;
#endif

    bool _initialized;
    bool _connected;
    bool _advertising_active;
    String _macAddress;
    String _firmwareVersion;

    OnBLEConfigReceived _onConfigReceived;
    OnPairingStarted _onPairingStarted;
    OnPairingCompleted _onPairingCompleted;

    BLEConfig _pendingConfig;

    /**
     * Parse WiFi configuration from JSON
     */
    bool parseWifiConfig(const String& json);

    /**
     * Parse API configuration from JSON
     */
    bool parseApiConfig(const String& json);

    /**
     * Check if configuration is complete
     */
    void checkConfiguration();

#ifdef PLATFORM_ESP32
    /**
     * Server callbacks (NimBLE 1.4.x signatures)
     */
    class ServerCallbacks : public NimBLEServerCallbacks {
    public:
        ServerCallbacks(BLEProvisioningService* parent) : _parent(parent) {}
        void onConnect(NimBLEServer* server) override;
        void onDisconnect(NimBLEServer* server) override;
    private:
        BLEProvisioningService* _parent;
    };

    /**
     * Characteristic callbacks (NimBLE 1.4.x signatures)
     */
    class CharacteristicCallbacks : public NimBLECharacteristicCallbacks {
    public:
        CharacteristicCallbacks(BLEProvisioningService* parent) : _parent(parent) {}
        void onWrite(NimBLECharacteristic* characteristic) override;
    private:
        BLEProvisioningService* _parent;
    };

    friend class ServerCallbacks;
    friend class CharacteristicCallbacks;
#endif
};

#endif // BLE_SERVICE_H
