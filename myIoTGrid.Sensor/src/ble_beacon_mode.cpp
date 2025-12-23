/**
 * myIoTGrid.Sensor - BLE Hybrid Mode Implementation
 * Combines:
 * 1. Beacon Mode: Broadcasts sensor data via advertising
 * 2. GATT Services: For bidirectional config exchange
 */

#include "ble_beacon_mode.h"

#ifdef PLATFORM_ESP32
#include <esp_mac.h>
#include <Preferences.h>

// Forward declaration for callbacks
static BleBeaconMode* g_beaconInstance = nullptr;

// NVS namespace for bonding data
static Preferences bondingPrefs;

/**
 * Server callbacks for connection events
 */
class BleBeaconMode::ServerCallbacks : public NimBLEServerCallbacks {
public:
    ServerCallbacks(BleBeaconMode* parent) : _parent(parent) {}

    void onConnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo) override {
        Serial.println("[BLE-Hybrid] Client connected!");
        Serial.printf("[BLE-Hybrid] Address: %s\n", connInfo.getAddress().toString().c_str());
        // Continue advertising for other clients (optional)
        // pServer->startAdvertising();
    }

    void onDisconnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo, int reason) override {
        Serial.printf("[BLE-Hybrid] Client disconnected, reason: %d\n", reason);
        // Reset authentication on disconnect
        _parent->resetAuthentication();
        Serial.println("[BLE-Hybrid] Authentication reset");
        // Restart advertising
        if (_parent->_advertising) {
            NimBLEDevice::startAdvertising();
        }
    }

private:
    BleBeaconMode* _parent;
};

/**
 * Config Write characteristic callbacks
 */
class BleBeaconMode::ConfigWriteCallbacks : public NimBLECharacteristicCallbacks {
public:
    ConfigWriteCallbacks(BleBeaconMode* parent) : _parent(parent) {}

    void onWrite(NimBLECharacteristic* pCharacteristic, NimBLEConnInfo& connInfo) override {
        NimBLEAttValue value = pCharacteristic->getValue();
        size_t len = value.size();

        if (len < 1) {
            Serial.println("[BLE-Hybrid] Empty config command received");
            _parent->sendResponse(RESP_INVALID_DATA);
            return;
        }

        uint8_t cmd = value[0];
        Serial.printf("[BLE-Hybrid] Config command received: 0x%02X, len=%d\n", cmd, len);

        // Handle AUTH command - always allowed
        if (cmd == CMD_AUTH) {
            if (len < 5) {  // CMD + 4 bytes hash
                Serial.println("[BLE-Auth] Invalid auth data (need 4 bytes hash)");
                _parent->sendResponse(RESP_INVALID_DATA);
                return;
            }
            if (_parent->authenticate(value.data() + 1, len - 1)) {
                Serial.println("[BLE-Auth] Authentication SUCCESS!");
                _parent->sendResponse(RESP_OK);
            } else {
                Serial.println("[BLE-Auth] Authentication FAILED!");
                _parent->sendResponse(RESP_ERROR);
            }
            return;
        }

        // All other commands require authentication
        if (!_parent->isAuthenticated()) {
            Serial.println("[BLE-Hybrid] Command rejected - not authenticated!");
            Serial.println("[BLE-Hybrid] Send CMD_AUTH (0x00) with node ID hash first");
            _parent->sendResponse(RESP_NOT_AUTHENTICATED);
            return;
        }

        // Call the config callback if set
        if (_parent->_configCallback) {
            _parent->_configCallback(cmd, value.data() + 1, len - 1);
        } else {
            // Default handling
            switch (cmd) {
                case CMD_REBOOT:
                    Serial.println("[BLE-Hybrid] Reboot command - restarting...");
                    _parent->sendResponse(RESP_OK);
                    delay(500);
                    ESP.restart();
                    break;

                case CMD_FACTORY_RESET:
                    Serial.println("[BLE-Hybrid] Factory reset command received");
                    _parent->sendResponse(RESP_OK);
                    // Factory reset would be handled by main.cpp
                    break;

                default:
                    Serial.printf("[BLE-Hybrid] Unknown command: 0x%02X\n", cmd);
                    _parent->sendResponse(RESP_INVALID_CMD);
                    break;
            }
        }
    }

private:
    BleBeaconMode* _parent;
};

/**
 * Security callbacks for bonding/pairing
 * NimBLE 2.x API - only these methods are available for server:
 * - onPassKeyDisplay(): Display passkey for user to enter on client
 * - onAuthenticationComplete(): Called when pairing finishes
 * - onConnect() / onDisconnect(): Connection events
 */
class BleBeaconMode::SecurityCallbacks : public NimBLEServerCallbacks {
public:
    SecurityCallbacks(BleBeaconMode* parent) : _parent(parent) {}

    // Called when pairing passkey should be displayed
    uint32_t onPassKeyDisplay() override {
        Serial.println("[BLE-Security] =====================================");
        Serial.println("[BLE-Security] PAIRING REQUEST!");
        Serial.printf("[BLE-Security] Passkey: %06d\n", BLE_PASSKEY);
        Serial.println("[BLE-Security] Enter this code on your Hub to pair.");
        Serial.println("[BLE-Security] =====================================");
        return BLE_PASSKEY;
    }

    // Called when authentication/pairing completes
    void onAuthenticationComplete(NimBLEConnInfo& connInfo) override {
        if (connInfo.isEncrypted()) {
            Serial.println("[BLE-Security] =====================================");
            Serial.println("[BLE-Security] PAIRING SUCCESSFUL!");
            Serial.printf("[BLE-Security] Bonded: %s\n", connInfo.isBonded() ? "YES" : "NO");
            Serial.printf("[BLE-Security] Encrypted: %s\n", connInfo.isEncrypted() ? "YES" : "NO");
            Serial.printf("[BLE-Security] Address: %s\n", connInfo.getAddress().toString().c_str());
            Serial.println("[BLE-Security] =====================================");

            if (connInfo.isBonded()) {
                _parent->updateBondedFlag();
                Serial.printf("[BLE-Security] Total bonded devices: %d\n", NimBLEDevice::getNumBonds());
            }
        } else {
            Serial.println("[BLE-Security] Authentication FAILED - connection not encrypted");
            Serial.println("[BLE-Security] Disconnecting unauthenticated client...");
            // Disconnect unauthenticated client
            NimBLEDevice::getServer()->disconnect(connInfo.getConnHandle());
        }
    }

    void onConnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo) override {
        Serial.println("[BLE-Security] Client connected!");
        Serial.printf("[BLE-Security] Address: %s\n", connInfo.getAddress().toString().c_str());
        Serial.printf("[BLE-Security] Already bonded: %s\n", connInfo.isBonded() ? "YES" : "NO");

        if (connInfo.isBonded()) {
            Serial.println("[BLE-Security] Known device - skipping pairing");
        } else {
            Serial.println("[BLE-Security] New device - pairing will be required");
        }
    }

    void onDisconnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo, int reason) override {
        Serial.printf("[BLE-Security] Client disconnected, reason: %d\n", reason);
        // Restart advertising
        if (_parent->_advertising) {
            NimBLEDevice::startAdvertising();
        }
    }

private:
    BleBeaconMode* _parent;
};

#endif // PLATFORM_ESP32

BleBeaconMode::BleBeaconMode()
    : _initialized(false)
    , _advertising(false)
    , _configCallback(nullptr)
#ifdef PLATFORM_ESP32
    , _pAdvertising(nullptr)
    , _pServer(nullptr)
    , _pConfigService(nullptr)
    , _pConfigWriteChar(nullptr)
    , _pConfigReadChar(nullptr)
    , _pSensorDataChar(nullptr)
#endif
{
    memset(&_sensorData, 0, sizeof(_sensorData));
    memset(_nodeIdHash, 0, sizeof(_nodeIdHash));
}

BleBeaconMode::~BleBeaconMode() {
#ifdef PLATFORM_ESP32
    if (_initialized) {
        stop();
        NimBLEDevice::deinit(true);
    }
    g_beaconInstance = nullptr;
#endif
}

bool BleBeaconMode::init(const String& nodeId) {
#ifdef PLATFORM_ESP32
    _nodeId = nodeId;
    g_beaconInstance = this;

    // Get MAC address for device name
    uint8_t mac[6];
    esp_read_mac(mac, ESP_MAC_WIFI_STA);

    char deviceNameBuf[32];
    snprintf(deviceNameBuf, sizeof(deviceNameBuf), "myIoTGrid-%02X%02X", mac[4], mac[5]);
    _deviceName = String(deviceNameBuf);

    Serial.println("[BLE-Hybrid] =====================================");
    Serial.println("[BLE-Hybrid] Initializing BLE Hybrid Mode");
    Serial.println("[BLE-Hybrid] - Beacon: Sensor data in advertising");
    Serial.println("[BLE-Hybrid] - GATT: Config exchange via connection");
    Serial.println("[BLE-Hybrid] =====================================");
    Serial.printf("[BLE-Hybrid] Device name: %s\n", _deviceName.c_str());
    Serial.printf("[BLE-Hybrid] Node ID: %s\n", nodeId.c_str());

    // Compute node ID hash for identification
    computeNodeIdHash(nodeId);

    // Initialize NimBLE
    NimBLEDevice::init(_deviceName.c_str());
    NimBLEDevice::setPower(9);  // Max power
    NimBLEDevice::setMTU(256);  // Larger MTU for config data

    // NOTE: BLE-Level Security (Bonding) causes connection timeouts with BlueZ
    // Workaround: Use application-level authentication instead
    // The Hub will send an auth token via CONFIG_WRITE before config changes are accepted
    // This provides security without the compatibility issues of BLE pairing
    Serial.println("[BLE-Security] Using application-level authentication (no BLE bonding)");
    Serial.println("[BLE-Security] Hub must authenticate via CONFIG_WRITE before config changes");

    // Initialize sensor data structure
    _sensorData.companyId = MYIOTGRID_COMPANY_ID;
    _sensorData.deviceType = MYIOTGRID_DEVICE_TYPE;
    _sensorData.version = BEACON_PROTOCOL_VERSION;
    memcpy(_sensorData.nodeIdHash, _nodeIdHash, 4);
    _sensorData.temperature = 0;
    _sensorData.humidity = 0;
    _sensorData.pressure = 0;
    _sensorData.battery = 3300;
    _sensorData.flags = 0;

    // Setup GATT server and services
    setupGattServices();

    // Get advertising instance
    _pAdvertising = NimBLEDevice::getAdvertising();

    _initialized = true;
    Serial.println("[BLE-Hybrid] Initialization complete");
    return true;

#else
    _nodeId = nodeId;
    _deviceName = "myIoTGrid-SIM";
    _initialized = true;
    Serial.println("[BLE-Hybrid] Initialized (simulation mode)");
    return true;
#endif
}

#ifdef PLATFORM_ESP32

void BleBeaconMode::setupSecurity() {
    Serial.println("[BLE-Security] Setting up security/bonding...");

    // Set security settings - start with "Just Works" pairing (no passkey)
    // This is simpler for initial testing and works with most clients
    // BLE_SM_PAIR_AUTHREQ_BOND = bonding enabled (store keys)
    // BLE_SM_PAIR_AUTHREQ_SC = Secure Connections (BLE 4.2+)
    // Note: Removed MITM for now - can add passkey later if needed
    NimBLEDevice::setSecurityAuth(BLE_SM_PAIR_AUTHREQ_BOND | BLE_SM_PAIR_AUTHREQ_SC);

    // Set I/O capabilities - NoInputNoOutput for "Just Works" pairing
    // This allows automatic pairing without user interaction
    NimBLEDevice::setSecurityIOCap(BLE_HS_IO_NO_INPUT_OUTPUT);

    // Enable bonding key distribution
    NimBLEDevice::setSecurityInitKey(BLE_SM_PAIR_KEY_DIST_ENC | BLE_SM_PAIR_KEY_DIST_ID);
    NimBLEDevice::setSecurityRespKey(BLE_SM_PAIR_KEY_DIST_ENC | BLE_SM_PAIR_KEY_DIST_ID);

    // Check existing bonds
    int bondedCount = NimBLEDevice::getNumBonds();
    Serial.printf("[BLE-Security] Existing bonded devices: %d\n", bondedCount);

    if (bondedCount > 0) {
        _sensorData.flags |= FLAG_BONDED;
        Serial.println("[BLE-Security] Bonded addresses:");
        for (int i = 0; i < bondedCount; i++) {
            NimBLEAddress addr = NimBLEDevice::getBondedAddress(i);
            Serial.printf("[BLE-Security]   %d: %s\n", i, addr.toString().c_str());
        }
    }

    Serial.println("[BLE-Security] Mode: Just Works (automatic pairing)");
    Serial.println("[BLE-Security] Security setup complete");
}

void BleBeaconMode::setupGattServices() {
    Serial.println("[BLE-Hybrid] Setting up GATT services...");

    // Create server with callbacks
    // TODO: Use SecurityCallbacks when security is re-enabled
    _pServer = NimBLEDevice::createServer();
    _pServer->setCallbacks(new ServerCallbacks(this));

    // Create config service
    _pConfigService = _pServer->createService(CONFIG_SERVICE_UUID);

    // Config Write characteristic - Hub writes config commands here
    // Note: Security is handled at connection level, not characteristic level
    _pConfigWriteChar = _pConfigService->createCharacteristic(
        CONFIG_WRITE_CHAR_UUID,
        NIMBLE_PROPERTY::WRITE | NIMBLE_PROPERTY::WRITE_NR
    );
    _pConfigWriteChar->setCallbacks(new ConfigWriteCallbacks(this));

    // Config Read characteristic - Hub reads device info here
    _pConfigReadChar = _pConfigService->createCharacteristic(
        CONFIG_READ_CHAR_UUID,
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY
    );

    // Build device info JSON for the read characteristic
    String deviceInfo = "{";
    deviceInfo += "\"nodeId\":\"" + _nodeId + "\",";
    deviceInfo += "\"deviceName\":\"" + _deviceName + "\",";
    deviceInfo += "\"firmware\":\"" + String(FIRMWARE_VERSION) + "\",";
    deviceInfo += "\"hash\":\"";
    char hashStr[9];
    snprintf(hashStr, sizeof(hashStr), "%02X%02X%02X%02X",
             _nodeIdHash[0], _nodeIdHash[1], _nodeIdHash[2], _nodeIdHash[3]);
    deviceInfo += hashStr;
    deviceInfo += "\"}";
    _pConfigReadChar->setValue(deviceInfo.c_str());

    // Sensor Data characteristic - Hub reads current sensor values
    // No encryption required for sensor data - available without pairing
    // Config characteristics require pairing, sensor data is public (like beacon)
    _pSensorDataChar = _pConfigService->createCharacteristic(
        SENSOR_DATA_CHAR_UUID,
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY
    );

    // Start the service
    _pConfigService->start();

    Serial.println("[BLE-Hybrid] GATT services configured:");
    Serial.printf("[BLE-Hybrid] - Service: %s\n", CONFIG_SERVICE_UUID);
    Serial.printf("[BLE-Hybrid] - Config Write: %s\n", CONFIG_WRITE_CHAR_UUID);
    Serial.printf("[BLE-Hybrid] - Config Read: %s\n", CONFIG_READ_CHAR_UUID);
    Serial.printf("[BLE-Hybrid] - Sensor Data: %s\n", SENSOR_DATA_CHAR_UUID);
}

void BleBeaconMode::updateSensorDataCharacteristic() {
    if (!_pSensorDataChar) return;

    // Build sensor data JSON
    String sensorJson = "{";
    sensorJson += "\"t\":" + String(_sensorData.temperature / 100.0, 2) + ",";
    sensorJson += "\"h\":" + String(_sensorData.humidity / 100.0, 1) + ",";
    sensorJson += "\"p\":" + String((_sensorData.pressure + 50000) / 100.0, 1) + ",";
    sensorJson += "\"b\":" + String(_sensorData.battery) + ",";
    sensorJson += "\"f\":" + String(_sensorData.flags);
    sensorJson += "}";

    _pSensorDataChar->setValue(sensorJson.c_str());

    // Notify connected clients
    if (_pServer && _pServer->getConnectedCount() > 0) {
        _pSensorDataChar->notify();
    }
}
#endif

void BleBeaconMode::computeNodeIdHash(const String& nodeId) {
    // Simple hash of node ID to fit in 4 bytes
    uint32_t hash = 0;
    for (size_t i = 0; i < nodeId.length(); i++) {
        hash = hash * 31 + nodeId.charAt(i);
    }
    _nodeIdHash[0] = (hash >> 24) & 0xFF;
    _nodeIdHash[1] = (hash >> 16) & 0xFF;
    _nodeIdHash[2] = (hash >> 8) & 0xFF;
    _nodeIdHash[3] = hash & 0xFF;

    Serial.printf("[BLE-Hybrid] Node ID hash: %02X%02X%02X%02X\n",
                  _nodeIdHash[0], _nodeIdHash[1], _nodeIdHash[2], _nodeIdHash[3]);
}

void BleBeaconMode::updateSensorData(float temperature, float humidity, float pressure, uint16_t batteryMv) {
#ifdef PLATFORM_ESP32
    if (!_initialized) return;

    // Convert to packed format
    _sensorData.temperature = (int16_t)(temperature * 100);  // 21.50°C -> 2150
    _sensorData.humidity = (uint16_t)(humidity * 100);       // 65.00% -> 6500
    _sensorData.pressure = (uint16_t)(pressure - 50000);     // 101325 -> 51325
    _sensorData.battery = batteryMv;

    Serial.printf("[BLE-Hybrid] Updating: T=%.2f°C, H=%.1f%%, P=%.0f hPa, Bat=%dmV\n",
                  temperature, humidity, pressure, batteryMv);

    // Update advertising data (beacon)
    updateAdvertisingData();

    // Update GATT characteristic
    updateSensorDataCharacteristic();

    // Restart advertising with new data
    if (_advertising) {
        _pAdvertising->stop();
        _pAdvertising->start();
        Serial.println("[BLE-Hybrid] Advertising restarted with new data");
    }
#else
    Serial.printf("[BLE-Hybrid] SIM: T=%.2f, H=%.1f, P=%.0f\n", temperature, humidity, pressure);
#endif
}

void BleBeaconMode::updateAdvertisingData() {
#ifdef PLATFORM_ESP32
    // Simplified manufacturer data format (max 31 bytes in advertising packet!)
    // Format: [temp:2][humidity:2][pressure:2][battery:2] = 8 bytes
    // Company ID 0xFFFF is included automatically by NimBLE
    std::vector<uint8_t> mfgData;

    // Add sensor data (8 bytes) - company ID is added by NimBLE
    mfgData.push_back(_sensorData.temperature & 0xFF);
    mfgData.push_back((_sensorData.temperature >> 8) & 0xFF);
    mfgData.push_back(_sensorData.humidity & 0xFF);
    mfgData.push_back((_sensorData.humidity >> 8) & 0xFF);
    mfgData.push_back(_sensorData.pressure & 0xFF);
    mfgData.push_back((_sensorData.pressure >> 8) & 0xFF);
    mfgData.push_back(_sensorData.battery & 0xFF);
    mfgData.push_back((_sensorData.battery >> 8) & 0xFF);

    Serial.printf("[BLE-Hybrid] Manufacturer data: %d bytes\n", mfgData.size());

    // Advertising data: flags + manufacturer data
    NimBLEAdvertisementData advData;
    advData.setFlags(BLE_HS_ADV_F_DISC_GEN | BLE_HS_ADV_F_BREDR_UNSUP);
    advData.setManufacturerData(mfgData);
    _pAdvertising->setAdvertisementData(advData);

    // Scan response: device name + service UUID (for GATT discovery)
    NimBLEAdvertisementData scanResp;
    scanResp.setName(_deviceName.c_str());
    scanResp.setCompleteServices16({NimBLEUUID((uint16_t)0xFFFF)});  // Indicate custom service available
    _pAdvertising->setScanResponseData(scanResp);

    // Connectable advertising interval
    _pAdvertising->setMinInterval(160);  // 100ms
    _pAdvertising->setMaxInterval(320);  // 200ms
#endif
}

void BleBeaconMode::startAdvertising() {
#ifdef PLATFORM_ESP32
    if (!_initialized || !_pAdvertising) {
        Serial.println("[BLE-Hybrid] Cannot start - not initialized");
        return;
    }

    if (_advertising) {
        Serial.println("[BLE-Hybrid] Already advertising");
        return;
    }

    updateAdvertisingData();
    _pAdvertising->start();
    _advertising = true;

    Serial.println("[BLE-Hybrid] =====================================");
    Serial.println("[BLE-Hybrid] ADVERTISING STARTED");
    Serial.printf("[BLE-Hybrid] Device: %s\n", _deviceName.c_str());
    Serial.println("[BLE-Hybrid] - Beacon: Sensor data in advertising");
    Serial.println("[BLE-Hybrid] - GATT: Connect for config exchange");
    Serial.println("[BLE-Hybrid] =====================================");
#else
    _advertising = true;
    Serial.println("[BLE-Hybrid] Advertising started (simulation)");
#endif
}

void BleBeaconMode::stop() {
#ifdef PLATFORM_ESP32
    if (_pAdvertising && _advertising) {
        _pAdvertising->stop();
    }
    // Disconnect all clients
    if (_pServer) {
        _pServer->disconnect(0);  // Disconnect all
    }
#endif
    _advertising = false;
    Serial.println("[BLE-Hybrid] Stopped");
}

bool BleBeaconMode::isConnected() const {
#ifdef PLATFORM_ESP32
    return _pServer && _pServer->getConnectedCount() > 0;
#else
    return false;
#endif
}

void BleBeaconMode::setErrorFlag(bool error) {
    if (error) {
        _sensorData.flags |= FLAG_ERROR;
    } else {
        _sensorData.flags &= ~FLAG_ERROR;
    }
}

void BleBeaconMode::setLowBatteryFlag(bool lowBattery) {
    if (lowBattery) {
        _sensorData.flags |= FLAG_LOW_BATTERY;
    } else {
        _sensorData.flags &= ~FLAG_LOW_BATTERY;
    }
}

void BleBeaconMode::setConfigCallback(ConfigReceivedCallback callback) {
    _configCallback = callback;
}

void BleBeaconMode::sendResponse(uint8_t responseCode, const uint8_t* data, size_t len) {
#ifdef PLATFORM_ESP32
    if (!_pConfigReadChar) return;

    std::vector<uint8_t> response;
    response.push_back(responseCode);
    if (data && len > 0) {
        response.insert(response.end(), data, data + len);
    }

    _pConfigReadChar->setValue(response);
    _pConfigReadChar->notify();

    Serial.printf("[BLE-Hybrid] Sent response: 0x%02X\n", responseCode);
#endif
}

void BleBeaconMode::loop() {
#ifdef PLATFORM_ESP32
    // NimBLE handles events in background tasks
    // This can be used for periodic tasks if needed
#endif
}

bool BleBeaconMode::isBonded() const {
#ifdef PLATFORM_ESP32
    return NimBLEDevice::getNumBonds() > 0;
#else
    return false;
#endif
}

uint8_t BleBeaconMode::getBondedDeviceCount() const {
#ifdef PLATFORM_ESP32
    return NimBLEDevice::getNumBonds();
#else
    return 0;
#endif
}

void BleBeaconMode::deleteBonds() {
#ifdef PLATFORM_ESP32
    Serial.println("[BLE-Security] Deleting all bonds...");
    int bondCount = NimBLEDevice::getNumBonds();
    for (int i = bondCount - 1; i >= 0; i--) {
        NimBLEAddress addr = NimBLEDevice::getBondedAddress(i);
        Serial.printf("[BLE-Security] Deleting bond: %s\n", addr.toString().c_str());
        NimBLEDevice::deleteBond(addr);
    }
    _sensorData.flags &= ~FLAG_BONDED;
    Serial.println("[BLE-Security] All bonds deleted");
#endif
}

void BleBeaconMode::updateBondedFlag() {
#ifdef PLATFORM_ESP32
    if (NimBLEDevice::getNumBonds() > 0) {
        _sensorData.flags |= FLAG_BONDED;
    } else {
        _sensorData.flags &= ~FLAG_BONDED;
    }
#endif
}

bool BleBeaconMode::authenticate(const uint8_t* hash, size_t len) {
    if (len < 4) {
        Serial.println("[BLE-Auth] Hash too short (need 4 bytes)");
        return false;
    }

    Serial.printf("[BLE-Auth] Received hash: %02X%02X%02X%02X\n",
                  hash[0], hash[1], hash[2], hash[3]);
    Serial.printf("[BLE-Auth] Expected hash: %02X%02X%02X%02X\n",
                  _nodeIdHash[0], _nodeIdHash[1], _nodeIdHash[2], _nodeIdHash[3]);

    // Compare first 4 bytes of hash
    if (memcmp(hash, _nodeIdHash, 4) == 0) {
        _authenticated = true;
        Serial.println("[BLE-Auth] Hash matches - authenticated!");
        return true;
    }

    Serial.println("[BLE-Auth] Hash mismatch!");
    return false;
}
