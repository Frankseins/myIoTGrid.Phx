/**
 * myIoTGrid.Sensor - Bluetooth Sensor Mode Implementation
 * BLE GATT Server for transmitting sensor data to BluetoothHub
 *
 * Sprint BT-01: Bluetooth Infrastructure
 */

#include "bluetooth_sensor_mode.h"
#include <ArduinoJson.h>

#ifdef PLATFORM_ESP32
#include <WiFi.h>
#include <esp_mac.h>
#include <esp_bt.h>
#endif

BluetoothSensorMode::BluetoothSensorMode()
    : _initialized(false)
    , _connected(false)
    , _advertising_active(false)
    , _lastTransmitSuccess(false)
    , _connectionCount(0)
    , _transmissionCount(0)
#ifdef PLATFORM_ESP32
    , _server(nullptr)
    , _service(nullptr)
    , _sensorDataChar(nullptr)
    , _deviceInfoChar(nullptr)
    , _advertising(nullptr)
#endif
{
}

BluetoothSensorMode::~BluetoothSensorMode() {
#ifdef PLATFORM_ESP32
    if (_initialized) {
        NimBLEDevice::deinit(true);
    }
#endif
}

bool BluetoothSensorMode::init(const String& nodeId) {
#ifdef PLATFORM_ESP32
    _nodeId = nodeId;

    // Get base MAC address using ESP-IDF API (no WiFi needed)
    // This is the base MAC, WiFi MAC = base, BLE MAC = base + 2
    uint8_t mac[6];
    esp_read_mac(mac, ESP_MAC_WIFI_STA);  // Get base station MAC

    char macBuf[18];
    snprintf(macBuf, sizeof(macBuf), "%02X:%02X:%02X:%02X:%02X:%02X",
             mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);
    _macAddress = String(macBuf);

    // Create device name: myIoTGrid-<last 4 hex chars of MAC>
    // This MUST match the pairing service name so the Hub can find us!
    // e.g., "myIoTGrid-92CC" for MAC 00:70:07:84:92:CC
    char deviceNameBuf[32];
    snprintf(deviceNameBuf, sizeof(deviceNameBuf), "%s%02X%02X",
             config::ble_sensor::BLE_DEVICE_NAME_PREFIX, mac[4], mac[5]);
    _deviceName = String(deviceNameBuf);

    Serial.printf("[BLE-Sensor] Initializing with name: %s\n", _deviceName.c_str());
    Serial.printf("[BLE-Sensor] MAC Address: %s\n", _macAddress.c_str());

    // Initialize NimBLE with the device name
    NimBLEDevice::init(_deviceName.c_str());
    NimBLEDevice::setPower(9);  // Max power (9 dBm) for better range

    // NimBLE 2.x: Security is disabled by default, no need for explicit calls
    Serial.println("[BLE-Sensor] NimBLE 2.x initialized - security disabled by default");

    // Create server
    _server = NimBLEDevice::createServer();
    _server->setCallbacks(new ServerCallbacks(this));

    // Create Sensor Data Service
    _service = _server->createService(config::ble_sensor::SERVICE_UUID);

    // Create Sensor Data Characteristic (NOTIFY)
    // BluetoothHub subscribes to notifications to receive sensor data
    _sensorDataChar = _service->createCharacteristic(
        config::ble_sensor::CHAR_SENSOR_DATA_UUID,
        NIMBLE_PROPERTY::NOTIFY
    );

    // Create Device Info Characteristic (READ)
    // BluetoothHub reads this to get node identification
    _deviceInfoChar = _service->createCharacteristic(
        config::ble_sensor::CHAR_DEVICE_INFO_UUID,
        NIMBLE_PROPERTY::READ
    );

    // Set initial device info
    String deviceInfo = buildDeviceInfoJson();
    _deviceInfoChar->setValue(deviceInfo.c_str());

    // Start service
    _service->start();

    // Configure advertising (NimBLE 2.x API)
    _advertising = NimBLEDevice::getAdvertising();
    _advertising->addServiceUUID(config::ble_sensor::SERVICE_UUID);
    _advertising->enableScanResponse(true);
    // NimBLE 2.x: Set connection interval to match what Linux/BlueZ prefers (30-50ms)
    // Values are in units of 1.25ms: 0x18 = 30ms, 0x28 = 50ms
    _advertising->setPreferredParams(0x18, 0x28);  // 30-50ms - matches BlueZ default

    _initialized = true;
    Serial.println("[BLE-Sensor] Initialization complete");

    return true;
#else
    // Native platform - just mark as initialized for testing
    _nodeId = nodeId;
    _deviceName = String(config::ble_sensor::BLE_DEVICE_NAME_PREFIX) + nodeId;
    _macAddress = "00:00:00:00:00:00";
    _initialized = true;
    Serial.println("[BLE-Sensor] Initialized (Native - simulation mode)");
    return true;
#endif
}

void BluetoothSensorMode::startAdvertising() {
#ifdef PLATFORM_ESP32
    if (!_initialized || !_advertising) {
        Serial.println("[BLE-Sensor] Cannot start advertising - not initialized");
        return;
    }

    if (_advertising_active) {
        Serial.println("[BLE-Sensor] Already advertising");
        return;
    }

    Serial.println("[BLE-Sensor] Starting advertising...");
    _advertising->start();
    _advertising_active = true;
    Serial.printf("[BLE-Sensor] Advertising as: %s\n", _deviceName.c_str());
#else
    _advertising_active = true;
    Serial.println("[BLE-Sensor] Advertising started (simulation)");
#endif
}

void BluetoothSensorMode::stop() {
#ifdef PLATFORM_ESP32
    if (_advertising_active && _advertising) {
        _advertising->stop();
        _advertising_active = false;
    }

    if (_server) {
        _server->disconnect(_server->getConnectedCount());
    }

    _connected = false;
    Serial.println("[BLE-Sensor] Stopped");
#else
    _advertising_active = false;
    _connected = false;
    Serial.println("[BLE-Sensor] Stopped (simulation)");
#endif
}

void BluetoothSensorMode::loop() {
#ifdef PLATFORM_ESP32
    // NimBLE handles events internally via FreeRTOS tasks
    // Debug: Log state periodically
    static unsigned long lastDebug = 0;
    int clientCount = _server ? _server->getConnectedCount() : -1;

    if (millis() - lastDebug > 10000) {  // Every 10 seconds
        lastDebug = millis();
        Serial.printf("[BLE-Sensor] State: init=%d, conn=%d, adv=%d, clients=%d\n",
                      _initialized, _connected, _advertising_active, clientCount);
    }

    // Detect if client connected but our flag not set (callback missed)
    if (clientCount > 0 && !_connected) {
        Serial.println("[BLE-Sensor] !!! Client connected but callback missed - syncing state !!!");
        _connected = true;
        _advertising_active = false;
    }

    if (!_connected && _initialized && !_advertising_active) {
        // Restart advertising if disconnected and not already advertising
        Serial.println("[BLE-Sensor] Connection lost, restarting advertising...");
        startAdvertising();
    }
#endif
}

bool BluetoothSensorMode::sendSensorData(const std::vector<BleSensorReading>& readings,
                                          const BleGpsData* gps) {
#ifdef PLATFORM_ESP32
    if (!_connected || !_sensorDataChar) {
        Serial.println("[BLE-Sensor] Cannot send data - not connected");
        _lastTransmitSuccess = false;
        return false;
    }

    // Build JSON payload
    String jsonPayload = buildSensorDataJson(readings, gps);

    Serial.printf("[BLE-Sensor] Sending %d readings (%d bytes)\n",
                  readings.size(), jsonPayload.length());

    // Send via notification
    _sensorDataChar->setValue(jsonPayload.c_str());
    _sensorDataChar->notify();

    _transmissionCount++;
    _lastTransmitSuccess = true;

    if (_onTransmitComplete) {
        _onTransmitComplete(true);
    }

    Serial.println("[BLE-Sensor] Data sent successfully");
    return true;
#else
    // Native simulation
    Serial.printf("[BLE-Sensor] Simulating send of %d readings\n", readings.size());
    _transmissionCount++;
    _lastTransmitSuccess = true;
    if (_onTransmitComplete) {
        _onTransmitComplete(true);
    }
    return true;
#endif
}

String BluetoothSensorMode::buildSensorDataJson(const std::vector<BleSensorReading>& readings,
                                                  const BleGpsData* gps) {
    JsonDocument doc;

    // Node identification
    doc["nodeId"] = _nodeId;
    doc["timestamp"] = millis();  // Will be converted to UTC by BluetoothHub

    // Sensor readings array
    JsonArray sensorsArray = doc["sensors"].to<JsonArray>();
    for (const auto& reading : readings) {
        JsonObject sensor = sensorsArray.add<JsonObject>();
        sensor["type"] = reading.sensorType;
        sensor["value"] = reading.value;
        sensor["unit"] = reading.unit;
    }

    // GPS data (if available)
    if (gps && gps->valid) {
        JsonObject gpsObj = doc["gps"].to<JsonObject>();
        gpsObj["latitude"] = gps->latitude;
        gpsObj["longitude"] = gps->longitude;
        gpsObj["altitude"] = gps->altitude;
        gpsObj["speed"] = gps->speed;
        gpsObj["course"] = gps->course;
        gpsObj["satellites"] = gps->satellites;
    }

    String output;
    serializeJson(doc, output);
    return output;
}

String BluetoothSensorMode::buildDeviceInfoJson() {
    JsonDocument doc;

    doc["nodeId"] = _nodeId;
    doc["macAddress"] = _macAddress;
    doc["firmwareVersion"] = FIRMWARE_VERSION;
    doc["hardwareType"] = HARDWARE_TYPE;
    doc["protocol"] = "bluetooth";

    String output;
    serializeJson(doc, output);
    return output;
}

#ifdef PLATFORM_ESP32
void BluetoothSensorMode::ServerCallbacks::onConnect(NimBLEServer* server, NimBLEConnInfo& connInfo) {
    Serial.println("========================================");
    Serial.println("[BLE-Sensor] >>> CONNECTION ESTABLISHED <<<");
    Serial.printf("[BLE-Sensor] Connected clients: %d\n", server->getConnectedCount());

    // Get peer info from connInfo parameter (NimBLE 2.x)
    Serial.printf("[BLE-Sensor] Peer address: %s\n", connInfo.getAddress().toString().c_str());
    Serial.printf("[BLE-Sensor] Connection handle: %d\n", connInfo.getConnHandle());
    Serial.printf("[BLE-Sensor] MTU: %d\n", connInfo.getMTU());
    Serial.println("========================================");

    _parent->_connected = true;
    _parent->_advertising_active = false;
    _parent->_connectionCount++;

    if (_parent->_onConnected) {
        _parent->_onConnected();
    }
}

void BluetoothSensorMode::ServerCallbacks::onDisconnect(NimBLEServer* server, NimBLEConnInfo& connInfo, int reason) {
    Serial.println("========================================");
    Serial.println("[BLE-Sensor] >>> DISCONNECTED <<<");
    Serial.printf("[BLE-Sensor] Remaining clients: %d\n", server->getConnectedCount());
    Serial.printf("[BLE-Sensor] Disconnect reason: 0x%02X\n", reason);
    Serial.println("========================================");
    _parent->_connected = false;

    if (_parent->_onDisconnected) {
        _parent->_onDisconnected();
    }

    // Restart advertising after disconnect
    Serial.println("[BLE-Sensor] Restarting advertising...");
    _parent->startAdvertising();
}
#endif
