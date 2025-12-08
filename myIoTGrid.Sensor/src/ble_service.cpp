/**
 * myIoTGrid.Sensor - BLE Provisioning Service Implementation
 */

#include "ble_service.h"
#include <ArduinoJson.h>
#include <string>
#ifdef PLATFORM_ESP32
#include <WiFi.h>
#endif
#ifdef PLATFORM_NATIVE
#include "ArduinoJsonString.h"
#endif

BLEProvisioningService::BLEProvisioningService()
    : _initialized(false)
    , _connected(false)
    , _advertising_active(false)
    , _isReProvisioning(false)
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

    // Store device name for potential re-initialization
    _deviceName = deviceName;
    _isReProvisioning = false;

    NimBLEDevice::init(deviceName.c_str());
    NimBLEDevice::setPower(ESP_PWR_LVL_P9);

    // IMPORTANT: WiFi must be initialized before reading MAC address!
    // Initialize WiFi in STA mode temporarily if not already done
    // This is required for WiFi.macAddress() to return valid data
    WiFi.mode(WIFI_STA);
    delay(10);  // Brief delay for WiFi subsystem to initialize

    // Use WiFi MAC address (not BLE MAC) for consistent device identification
    uint8_t mac[6];
    WiFi.macAddress(mac);
    char macBuf[24];
    snprintf(macBuf, sizeof(macBuf), "%02X%02X%02X%02X%02X%02X",
             mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);
    _macAddress = String(macBuf);
    Serial.printf("[BLE] WiFi MAC Address: %s\n", _macAddress.c_str());

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

    // Set registration value with just the node ID as plain string
    // Format: ESP32-<WiFi-MAC> (e.g., ESP32-0070078492CC)
    // This is simple, fits in any MTU, and is all the frontend needs
    // IMPORTANT: Use std::string to ensure NimBLE copies the data properly
    // Using c_str() with temporary String can cause pointer invalidation
    std::string nodeId = std::string("ESP32-") + _macAddress.c_str();
    _regChar->setValue(nodeId);
    Serial.printf("[BLE] Registration set (nodeId): %s (length: %d)\n", nodeId.c_str(), nodeId.length());

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
    Serial.println("[BLE] Stopping BLE service...");
    if (_advertising) {
        Serial.println("[BLE] Stopping advertising...");
        _advertising->stop();
    }
    // Note: We don't call NimBLEDevice::deinit() here because it can crash
    // when WiFi events are being processed. Just stop advertising instead.
    // The BLE stack will be cleaned up on next reboot if needed.
#endif
    _advertising_active = false;
    _connected = false;
    Serial.println("[BLE] Service stopped (advertising disabled)");
}

void BLEProvisioningService::stopForWPS() {
#ifdef PLATFORM_ESP32
    Serial.println("[BLE] Stopping BLE for WPS mode...");
    if (_advertising) {
        _advertising->stop();
    }
    // Disconnect any connected clients
    if (_server && _server->getConnectedCount() > 0) {
        Serial.println("[BLE] Disconnecting clients...");
        _server->disconnect(0);
    }
    _advertising_active = false;
    _connected = false;
    // Give BLE stack time to settle
    delay(200);
    Serial.println("[BLE] BLE paused for WPS");
#endif
}

bool BLEProvisioningService::startForReProvisioning() {
#ifdef PLATFORM_ESP32
    Serial.println("[BLE] ========================================");
    Serial.println("[BLE] Starting RE_PAIRING Mode");
    Serial.println("[BLE] ========================================");

    // Set re-provisioning flag
    _isReProvisioning = true;

    // If already initialized, we need to restart with new name
    if (_initialized) {
        // Stop current advertising
        if (_advertising) {
            _advertising->stop();
        }
        _advertising_active = false;

        // Create new device name with -SETUP suffix
        String reProvisioningName = _deviceName + "-SETUP";
        Serial.printf("[BLE] Re-initializing with name: %s\n", reProvisioningName.c_str());

        // NimBLE doesn't support changing device name without deinit
        // So we'll just update the advertising data with new name
        // The -SETUP suffix is added to help frontend distinguish RE_PAIRING mode

        // Actually, we need to deinit and reinit to change the name
        // This is a limitation of NimBLE
        NimBLEDevice::deinit(true);
        delay(100);

        // Reinitialize with new name
        NimBLEDevice::init(reProvisioningName.c_str());
        NimBLEDevice::setPower(ESP_PWR_LVL_P9);

        // Recreate server
        _server = NimBLEDevice::createServer();
        _server->setCallbacks(new ServerCallbacks(this));

        // Recreate service
        _nimbleService = _server->createService(SERVICE_UUID);

        // Recreate characteristics
        _regChar = _nimbleService->createCharacteristic(
            CHAR_REGISTRATION_UUID,
            NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY
        );

        // Set registration value
        std::string nodeId = std::string("ESP32-") + _macAddress.c_str();
        _regChar->setValue(nodeId);

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
    } else {
        // Not initialized yet - initialize with -SETUP suffix
        String reProvisioningName = "myIoTGrid-SETUP";

        // Get MAC for naming
        WiFi.mode(WIFI_STA);
        delay(10);
        uint8_t mac[6];
        WiFi.macAddress(mac);
        char macBuf[8];
        snprintf(macBuf, sizeof(macBuf), "%02X%02X", mac[4], mac[5]);
        reProvisioningName = String("myIoTGrid-") + macBuf + "-SETUP";

        if (!init(reProvisioningName)) {
            Serial.println("[BLE] Failed to initialize for RE_PAIRING");
            _isReProvisioning = false;
            return false;
        }
    }

    // Start advertising
    Serial.println("[BLE] Starting RE_PAIRING advertising...");
    _advertising->start();
    _advertising_active = true;

    if (_onPairingStarted) {
        _onPairingStarted();
    }

    Serial.println("[BLE] RE_PAIRING mode active - waiting for new WiFi credentials");
    Serial.println("[BLE] Device name has '-SETUP' suffix for frontend detection");
    return true;
#else
    Serial.println("[BLE] RE_PAIRING not supported on this platform");
    _isReProvisioning = true;
    _advertising_active = true;
    return true;
#endif
}

void BLEProvisioningService::setReProvisioningCallback(OnReProvisioningConfigReceived callback) {
    _onReProvisioningConfig = callback;
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

void BLEProvisioningService::setFirmwareVersion(const String& firmwareVersion) {
    _firmwareVersion = firmwareVersion;

#ifdef PLATFORM_ESP32
    if (!_regChar) return;

    // Just set the node ID as plain string - simple and reliable
    // IMPORTANT: Use std::string to ensure NimBLE copies the data properly
    std::string nodeId = std::string("ESP32-") + _macAddress.c_str();
    _regChar->setValue(nodeId);

    Serial.printf("[BLE] Registration set (nodeId): %s (length: %d)\n", nodeId.c_str(), nodeId.length());
#endif
}

void BLEProvisioningService::sendRegistration(const String& macAddress, const String& firmwareVersion) {
#ifdef PLATFORM_ESP32
    if (!_regChar) return;

    // Just send the node ID as plain string
    // IMPORTANT: Use std::string to ensure NimBLE copies the data properly
    std::string nodeId = std::string("ESP32-") + _macAddress.c_str();
    _regChar->setValue(nodeId);
    _regChar->notify();

    Serial.printf("[BLE] Sent registration (nodeId): %s (length: %d)\n", nodeId.c_str(), nodeId.length());
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
    Serial.println("[BLE] checkConfiguration() called");
    Serial.printf("[BLE] SSID length: %d, Password length: %d, HubURL length: %d\n",
                  _pendingConfig.wifiSsid.length(),
                  _pendingConfig.wifiPassword.length(),
                  _pendingConfig.hubApiUrl.length());

    // NEW FLOW: Prefer full config (WiFi + Hub URL) over WiFi-only
    // Frontend sends: 1) WiFi config, 2) API config (with Hub URL)
    //
    // Cases:
    // 1. WiFi + API config received -> Trigger callback with full config (direct Hub connection)
    // 2. WiFi only (API not yet received) -> Wait for API config
    // 3. API config received but no WiFi -> Wait for WiFi config

    // Check if we have WiFi credentials (minimum required)
    if (_pendingConfig.wifiSsid.length() > 0 &&
        _pendingConfig.wifiPassword.length() > 0) {

        // Generate nodeId from WiFi MAC address if not provided
        // Format: ESP32-<full-6-byte-MAC> for consistent identification
        if (_pendingConfig.nodeId.length() == 0) {
            _pendingConfig.nodeId = "ESP32-" + _macAddress;
            Serial.printf("[BLE] Generated NodeID from WiFi MAC: %s\n", _pendingConfig.nodeId.c_str());
        }

        // Check if we have Hub URL (preferred - direct connection)
        if (_pendingConfig.hubApiUrl.length() > 0) {
            // Full configuration with Hub URL - trigger callback
            Serial.println("[BLE] Full configuration complete with Hub URL!");
            Serial.printf("[BLE] Hub URL: %s\n", _pendingConfig.hubApiUrl.c_str());

            _pendingConfig.isValid = true;

            if (_onPairingCompleted) {
                Serial.println("[BLE] Calling onPairingCompleted callback");
                _onPairingCompleted();
            }

            // Call appropriate callback based on mode
            if (_isReProvisioning && _onReProvisioningConfig) {
                Serial.println("[BLE] RE_PAIRING: Calling onReProvisioningConfig callback");
                _onReProvisioningConfig(_pendingConfig);
                _isReProvisioning = false;  // Reset flag after successful config
            } else if (_onConfigReceived) {
                Serial.println("[BLE] Calling onConfigReceived callback");
                _onConfigReceived(_pendingConfig);
            } else {
                Serial.println("[BLE] WARNING: No config callback set!");
            }

            // Reset pending config after full config is processed
            _pendingConfig = BLEConfig();
        } else {
            // WiFi-only received - wait for API config
            // Frontend Setup Wizard will send API config right after WiFi
            Serial.println("[BLE] WiFi received, waiting for API config with Hub URL...");
            Serial.println("[BLE] (If using WPS or manual WiFi, Hub will be discovered via UDP)");
            // Don't trigger callback yet - wait for API config
            // Mark that we're waiting with WiFi ready
            _pendingConfig.isValid = false;  // Not complete yet
        }
    } else if (_pendingConfig.hubApiUrl.length() > 0) {
        // API config received but no WiFi yet
        Serial.println("[BLE] API config received, waiting for WiFi credentials...");
    } else {
        Serial.println("[BLE] Configuration incomplete - waiting for data");
    }
}

void BLEProvisioningService::finalizeWifiOnlyConfig() {
    // Called when we want to proceed with WiFi-only mode (e.g., timeout or explicit trigger)
    if (_pendingConfig.wifiSsid.length() > 0 &&
        _pendingConfig.wifiPassword.length() > 0 &&
        _pendingConfig.hubApiUrl.length() == 0) {

        Serial.println("[BLE] Finalizing WiFi-only configuration (no Hub URL)");
        Serial.println("[BLE] ESP32 will discover Hub via UDP broadcast");

        // Generate nodeId from WiFi MAC address if not provided
        // Format: ESP32-<full-6-byte-MAC> for consistent identification
        if (_pendingConfig.nodeId.length() == 0) {
            _pendingConfig.nodeId = "ESP32-" + _macAddress;
        }

        _pendingConfig.isValid = true;

        if (_onPairingCompleted) {
            _onPairingCompleted();
        }
        if (_onConfigReceived) {
            _onConfigReceived(_pendingConfig);
        }
        _pendingConfig = BLEConfig();
    }
}

#ifdef PLATFORM_ESP32

void BLEProvisioningService::ServerCallbacks::onConnect(NimBLEServer* server) {
    Serial.println("[BLE] Client connected");
    _parent->_connected = true;
    _parent->_advertising_active = false;

    // Send registration notification to client
    if (_parent->_regChar && _parent->_firmwareVersion.length() > 0) {
        // Delay slightly to ensure connection is stable
        delay(100);
        _parent->_regChar->notify();
        Serial.println("[BLE] Sent registration notification");
    }
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
