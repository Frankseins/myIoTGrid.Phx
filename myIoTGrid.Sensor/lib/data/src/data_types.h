#ifndef DATA_TYPES_H
#define DATA_TYPES_H

#include <string>
#include <vector>
#include <cstdint>

namespace data {

/**
 * Sensor reading data structure
 * Represents a single measurement from a sensor
 */
struct Reading {
    std::string deviceId;     // Hub-assigned device ID
    std::string type;         // Sensor type code (e.g., "temperature")
    float value;              // Measurement value
    std::string unit;         // Unit of measurement (e.g., "°C")
    uint64_t timestamp;       // Unix timestamp in seconds

    Reading() : value(0.0f), timestamp(0) {}

    Reading(const std::string& devId, const std::string& sensorType,
            float val, const std::string& unitStr, uint64_t ts)
        : deviceId(devId), type(sensorType), value(val), unit(unitStr), timestamp(ts) {}
};

/**
 * Node information sent during registration
 * Contains hardware capabilities and firmware info
 */
struct NodeInfo {
    std::string serialNumber;               // Unique hardware serial (e.g., "SIM-A1B2C3D4-0001")
    std::vector<std::string> capabilities;  // Supported sensor types
    std::string firmwareVersion;            // Current firmware version
    std::string hardwareType;               // Hardware type (ESP32, SIM, LORA32)

    NodeInfo() = default;

    NodeInfo(const std::string& serial,
             const std::vector<std::string>& caps,
             const std::string& fwVersion,
             const std::string& hwType)
        : serialNumber(serial)
        , capabilities(caps)
        , firmwareVersion(fwVersion)
        , hardwareType(hwType) {}
};

/**
 * Sensor configuration from Hub
 * Defines which sensors are active and their pins
 */
struct SensorConfig {
    std::string type;         // Sensor type code
    bool enabled;             // Is sensor active?
    int pin;                  // GPIO pin (-1 for simulation)

    SensorConfig() : enabled(false), pin(-1) {}

    SensorConfig(const std::string& sensorType, bool isEnabled, int gpioPin)
        : type(sensorType), enabled(isEnabled), pin(gpioPin) {}
};

/**
 * Connection configuration
 * Defines how to connect to the Hub
 */
struct ConnectionConfig {
    std::string mode;         // Connection mode: "http", "mqtt", "lorawan"
    std::string endpoint;     // Hub endpoint URL/address

    ConnectionConfig() : mode("http") {}

    ConnectionConfig(const std::string& connMode, const std::string& connEndpoint)
        : mode(connMode), endpoint(connEndpoint) {}
};

/**
 * Complete node configuration received from Hub
 * Contains device identity, sensors, and connection settings
 */
struct NodeConfig {
    std::string deviceId;                   // Hub-assigned device ID
    std::string name;                       // Human-readable name
    std::string location;                   // Location string
    uint32_t intervalSeconds;               // Reading interval in seconds
    std::vector<SensorConfig> sensors;      // Configured sensors
    ConnectionConfig connection;             // Connection settings

    NodeConfig() : intervalSeconds(60) {}

    bool isValid() const {
        return !deviceId.empty() && intervalSeconds > 0;
    }

    /**
     * Get list of enabled sensor types
     */
    std::vector<std::string> getEnabledSensorTypes() const {
        std::vector<std::string> types;
        for (const auto& sensor : sensors) {
            if (sensor.enabled) {
                types.push_back(sensor.type);
            }
        }
        return types;
    }
};

} // namespace data

#endif // DATA_TYPES_H
