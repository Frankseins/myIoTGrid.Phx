/**
 * myIoTGrid.Sensor - Bluetooth Sensor Mode
 * BLE GATT Server for transmitting sensor data to BluetoothHub
 *
 * This mode allows ESP32 sensors to transmit data via Bluetooth Low Energy
 * to a BluetoothHub (Raspberry Pi) instead of using WiFi.
 *
 * Sprint BT-01: Bluetooth Infrastructure
 */

#ifndef BLUETOOTH_SENSOR_MODE_H
#define BLUETOOTH_SENSOR_MODE_H

#include <Arduino.h>
#include <functional>
#include "config.h"

#ifdef PLATFORM_ESP32
#include <NimBLEDevice.h>
#endif

/**
 * Sensor reading structure for BLE transmission
 */
struct BleSensorReading {
    String sensorType;      // e.g., "temperature", "humidity", "pressure"
    float value;            // Sensor value
    String unit;            // e.g., "C", "%", "hPa"

    BleSensorReading() : value(0.0f) {}
    BleSensorReading(const String& type, float val, const String& u)
        : sensorType(type), value(val), unit(u) {}
};

/**
 * GPS data structure for BLE transmission
 */
struct BleGpsData {
    double latitude;
    double longitude;
    double altitude;
    float speed;
    float course;
    int satellites;
    bool valid;

    BleGpsData() : latitude(0), longitude(0), altitude(0), speed(0),
                   course(0), satellites(0), valid(false) {}
};

/**
 * Callbacks for BLE Sensor Mode events
 */
using OnBleConnected = std::function<void()>;
using OnBleDisconnected = std::function<void()>;
using OnBleTransmitComplete = std::function<void(bool success)>;

/**
 * BLE Sensor Mode Service
 * Operates as a BLE peripheral, advertising sensor data to BluetoothHub
 */
class BluetoothSensorMode {
public:
    BluetoothSensorMode();
    ~BluetoothSensorMode();

    /**
     * Initialize BLE Sensor Mode with device name
     * @param nodeId Node identifier (e.g., "ESP32-AABBCCDD")
     * @return true if initialization successful
     */
    bool init(const String& nodeId);

    /**
     * Start advertising and waiting for BluetoothHub connection
     */
    void startAdvertising();

    /**
     * Stop advertising and disconnect
     */
    void stop();

    /**
     * Process BLE events (call in loop)
     */
    void loop();

    /**
     * Send sensor readings via BLE GATT notification
     * @param readings Vector of sensor readings
     * @param gps Optional GPS data
     * @return true if data was queued for transmission
     */
    bool sendSensorData(const std::vector<BleSensorReading>& readings,
                        const BleGpsData* gps = nullptr);

    /**
     * Check if BluetoothHub is connected
     */
    bool isConnected() const { return _connected; }

    /**
     * Check if advertising
     */
    bool isAdvertising() const { return _advertising_active; }

    /**
     * Check if initialized
     */
    bool isInitialized() const { return _initialized; }

    /**
     * Get last transmission status
     */
    bool wasLastTransmitSuccessful() const { return _lastTransmitSuccess; }

    /**
     * Get connection count (for statistics)
     */
    uint32_t getConnectionCount() const { return _connectionCount; }

    /**
     * Get transmission count (for statistics)
     */
    uint32_t getTransmissionCount() const { return _transmissionCount; }

    /**
     * Set callback for connection event
     */
    void setConnectedCallback(OnBleConnected callback) { _onConnected = callback; }

    /**
     * Set callback for disconnection event
     */
    void setDisconnectedCallback(OnBleDisconnected callback) { _onDisconnected = callback; }

    /**
     * Set callback for transmission complete event
     */
    void setTransmitCallback(OnBleTransmitComplete callback) { _onTransmitComplete = callback; }

    /**
     * Get MAC address of the BLE adapter
     */
    String getMacAddress() const { return _macAddress; }

    /**
     * Get the current device name
     */
    String getDeviceName() const { return _deviceName; }

private:
#ifdef PLATFORM_ESP32
    NimBLEServer* _server;
    NimBLEService* _service;
    NimBLECharacteristic* _sensorDataChar;
    NimBLECharacteristic* _deviceInfoChar;
    NimBLEAdvertising* _advertising;
#endif

    bool _initialized;
    bool _connected;
    bool _advertising_active;
    bool _lastTransmitSuccess;
    String _nodeId;
    String _macAddress;
    String _deviceName;
    uint32_t _connectionCount;
    uint32_t _transmissionCount;

    OnBleConnected _onConnected;
    OnBleDisconnected _onDisconnected;
    OnBleTransmitComplete _onTransmitComplete;

    /**
     * Build JSON payload for sensor data
     */
    String buildSensorDataJson(const std::vector<BleSensorReading>& readings,
                               const BleGpsData* gps);

    /**
     * Build device info JSON
     */
    String buildDeviceInfoJson();

#ifdef PLATFORM_ESP32
    /**
     * Server callbacks (NimBLE 2.x API)
     */
    class ServerCallbacks : public NimBLEServerCallbacks {
    public:
        ServerCallbacks(BluetoothSensorMode* parent) : _parent(parent) {}
        void onConnect(NimBLEServer* server, NimBLEConnInfo& connInfo) override;
        void onDisconnect(NimBLEServer* server, NimBLEConnInfo& connInfo, int reason) override;
    private:
        BluetoothSensorMode* _parent;
    };

    friend class ServerCallbacks;
#endif
};

#endif // BLUETOOTH_SENSOR_MODE_H
