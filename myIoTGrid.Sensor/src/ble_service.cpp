/**
 * myIoTGrid.Sensor - BLE Provisioning Service Implementation
 */

#include "ble_service.h"
#include <ArduinoJson.h>

BLEProvisioningService::BLEProvisioningService()
    : _initialized(false)
    , _connected(false)
    , _advertising_active(false)
#ifdef PLATFORM_ESP32
    , _server(nullptr)
    , _nimbleService(nullptr)
    , _regChar(nullptr)
    , _wifiChar(nullptr)
    , _apiChar(nullptr)
    , _statusChar(nullptr)
    , _advertising(nullptr)
#endif
{
}

BLEProvisioningService::~BLEProvisioningService() {
#ifdef PLATFORM_ESP32
    if (_initialized) {
        NimBLEDevice::deinit(true);
    }
#endif
}

bool BLEProvisioningService::init(const String& deviceName) {
#ifdef PLATFORM_ESP32
    Serial.printf("[BLE] Initializing with name: %s\n", deviceName.c_str());

    NimBLEDevice::init(deviceName.c_str());
    NimBLEDevice::setPower(ESP_PWR_LVL_P9);

    _macAddress = NimBLEDevice::getAddress().toString().c_str();
    Serial.printf("[BLE] MAC Address: %s\n", _macAddress.c_str());

    // Create server
    _server = NimBLEDevice::createServer();
    _server->setCallbacks(new ServerCallbacks(this));

    // Create service
    _nimbleService = _server->createService(SERVICE_UUID);

    // Create characteristics
    _regChar = _nimbleService->createCharacteristic(
        CHAR_REGISTRATION_UUID,
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY
    );

    _wifiChar = _nimbleService->createCharacteristic(
        CHAR_WIFI_CONFIG_UUID,
        NIMBLE_PROPERTY::WRITE
    );
    _wifiChar->setCallbacks(new CharacteristicCallbacks(this));

    _apiChar = _nimbleService->createCharacteristic(
        CHAR_API_CONFIG_UUID,
        NIMBLE_PROPERTY::WRITE
    );
    _apiChar->setCallbacks(new CharacteristicCallbacks(this));

    _statusChar = _nimbleService->createCharacteristic(
        CHAR_STATUS_UUID,
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY
    );

    // Start service
    _nimbleService->start();

    // Configure advertising
    _advertising = NimBLEDevice::getAdvertising();
    _advertising->addServiceUUID(SERVICE_UUID);
    _advertising->setAppearance(0x0540); // Generic Sensor
    _advertising->setScanResponse(true);

    _initialized = true;
    Serial.println("[BLE] Initialized successfully");
    return true;
#else
    Serial.println("[BLE] BLE not supported on this platform");
    _macAddress = "00:00:00:00:00:00";
    _initialized = true;
    return true;
#endif
}

void BLEProvisioningService::startAdvertising() {
#ifdef PLATFORM_ESP32
    if (!_initialized) {
        Serial.println("[BLE] Not initialized, cannot start advertising");
        return;
    }

    Serial.println("[BLE] Starting advertising...");
    _advertising->start();
    _advertising_active = true;

    if (_onPairingStarted) {
        _onPairingStarted();
    }
#else
    Serial.println("[BLE] Simulated advertising start");
    _advertising_active = true;
    if (_onPairingStarted) {
        _onPairingStarted();
    }
#endif
}

void BLEProvisioningService::stop() {
#ifdef PLATFORM_ESP32
    if (_advertising) {
        _advertising->stop();
    }
    if (_initialized) {
        NimBLEDevice::deinit(true);
        _initialized = false;
    }
#endif
    _advertising_active = false;
    _connected = false;
    Serial.println("[BLE] Service stopped");
}

void BLEProvisioningService::loop() {
    // Process any pending BLE events
    // NimBLE handles this internally, but we can add periodic checks here if needed
}

bool BLEProvisioningService::isConnected() const {
    return _connected;
}

bool BLEProvisioningService::isAdvertising() const {
    return _advertising_active;
}

String BLEProvisioningService::getMacAddress() const {
    return _macAddress;
}

void BLEProvisioningService::setConfigCallback(OnBLEConfigReceived callback) {
    _onConfigReceived = callback;
}

void BLEProvisioningService::setPairingStartedCallback(OnPairingStarted callback) {
    _onPairingStarted = callback;
}

void BLEProvisioningService::setPairingCompletedCallback(OnPairingCompleted callback) {
    _onPairingCompleted = callback;
}

void BLEProvisioningService::sendRegistration(const String& macAddress, const String& firmwareVersion) {
#ifdef PLATFORM_ESP32
    if (!_regChar) return;

    JsonDocument doc;
    doc["mac_address"] = macAddress;
    doc["firmware_version"] = firmwareVersion;

    String json;
    serializeJson(doc, json);

    _regChar->setValue(json.c_str());
    _regChar->notify();

    Serial.printf("[BLE] Sent registration: %s\n", json.c_str());
#else
    Serial.printf("[BLE] Simulated registration: MAC=%s, FW=%s\n",
                  macAddress.c_str(), firmwareVersion.c_str());
#endif
}

bool BLEProvisioningService::parseWifiConfig(const String& json) {
    JsonDocument doc;
    DeserializationError error = deserializeJson(doc, json);

    if (error) {
        Serial.printf("[BLE] Failed to parse WiFi config: %s\n", error.c_str());
        return false;
    }

    if (!doc["ssid"].is<const char*>() || !doc["password"].is<const char*>()) {
        Serial.println("[BLE] Invalid WiFi config: missing ssid or password");
        return false;
    }

    _pendingConfig.wifiSsid = doc["ssid"].as<String>();
    _pendingConfig.wifiPassword = doc["password"].as<String>();

    Serial.printf("[BLE] WiFi config received: SSID=%s\n", _pendingConfig.wifiSsid.c_str());
    return true;
}

bool BLEProvisioningService::parseApiConfig(const String& json) {
    JsonDocument doc;
    DeserializationError error = deserializeJson(doc, json);

    if (error) {
        Serial.printf("[BLE] Failed to parse API config: %s\n", error.c_str());
        return false;
    }

    if (!doc["node_id"].is<const char*>() ||
        !doc["api_key"].is<const char*>() ||
        !doc["hub_url"].is<const char*>()) {
        Serial.println("[BLE] Invalid API config: missing required fields");
        return false;
    }

    _pendingConfig.nodeId = doc["node_id"].as<String>();
    _pendingConfig.apiKey = doc["api_key"].as<String>();
    _pendingConfig.hubApiUrl = doc["hub_url"].as<String>();

    Serial.printf("[BLE] API config received: NodeID=%s, HubURL=%s\n",
                  _pendingConfig.nodeId.c_str(), _pendingConfig.hubApiUrl.c_str());
    return true;
}

void BLEProvisioningService::checkConfiguration() {
    // Check if we have all required configuration
    if (_pendingConfig.wifiSsid.length() > 0 &&
        _pendingConfig.wifiPassword.length() > 0 &&
        _pendingConfig.nodeId.length() > 0 &&
        _pendingConfig.apiKey.length() > 0 &&
        _pendingConfig.hubApiUrl.length() > 0) {

        _pendingConfig.isValid = true;

        Serial.println("[BLE] Configuration complete!");

        if (_onPairingCompleted) {
            _onPairingCompleted();
        }

        if (_onConfigReceived) {
            _onConfigReceived(_pendingConfig);
        }

        // Reset pending config
        _pendingConfig = BLEConfig();
    }
}

#ifdef PLATFORM_ESP32

void BLEProvisioningService::ServerCallbacks::onConnect(NimBLEServer* server) {
    Serial.println("[BLE] Client connected");
    _parent->_connected = true;
    _parent->_advertising_active = false;
}

void BLEProvisioningService::ServerCallbacks::onDisconnect(NimBLEServer* server) {
    Serial.println("[BLE] Client disconnected");
    _parent->_connected = false;

    // Restart advertising if not configured
    if (!_parent->_pendingConfig.isValid) {
        Serial.println("[BLE] Restarting advertising...");
        _parent->startAdvertising();
    }
}

void BLEProvisioningService::CharacteristicCallbacks::onWrite(NimBLECharacteristic* characteristic) {
    String uuid = characteristic->getUUID().toString().c_str();
    String value = characteristic->getValue().c_str();

    Serial.printf("[BLE] Received write on %s: %s\n", uuid.c_str(), value.c_str());

    if (uuid == BLEProvisioningService::CHAR_WIFI_CONFIG_UUID) {
        if (_parent->parseWifiConfig(value)) {
            _parent->checkConfiguration();
        }
    } else if (uuid == BLEProvisioningService::CHAR_API_CONFIG_UUID) {
        if (_parent->parseApiConfig(value)) {
            _parent->checkConfiguration();
        }
    }
}

#endif // PLATFORM_ESP32
