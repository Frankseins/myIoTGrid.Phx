#include "json_serializer.h"
#include "hal/hal.h"
#include <ArduinoJson.h>

namespace data {

std::string JsonSerializer::serializeReading(const Reading& reading) {
    JsonDocument doc;

    doc["deviceId"] = reading.deviceId;
    doc["type"] = reading.type;
    doc["value"] = reading.value;
    doc["unit"] = reading.unit;
    doc["timestamp"] = reading.timestamp;

    std::string output;
    serializeJson(doc, output);
    return output;
}

std::string JsonSerializer::serializeNodeInfo(const NodeInfo& info) {
    JsonDocument doc;

    doc["serialNumber"] = info.serialNumber;
    doc["firmwareVersion"] = info.firmwareVersion;
    doc["hardwareType"] = info.hardwareType;

    JsonArray caps = doc["capabilities"].to<JsonArray>();
    for (const auto& cap : info.capabilities) {
        caps.add(cap);
    }

    std::string output;
    serializeJson(doc, output);
    return output;
}

std::string JsonSerializer::serializeNodeConfig(const NodeConfig& config) {
    JsonDocument doc;

    doc["deviceId"] = config.deviceId;
    doc["name"] = config.name;
    doc["location"] = config.location;
    doc["intervalSeconds"] = config.intervalSeconds;

    // Sensors array
    JsonArray sensorsArr = doc["sensors"].to<JsonArray>();
    for (const auto& sensor : config.sensors) {
        JsonObject sensorObj = sensorsArr.add<JsonObject>();
        sensorObj["type"] = sensor.type;
        sensorObj["enabled"] = sensor.enabled;
        sensorObj["pin"] = sensor.pin;
    }

    // Connection object
    JsonObject connObj = doc["connection"].to<JsonObject>();
    connObj["mode"] = config.connection.mode;
    connObj["endpoint"] = config.connection.endpoint;

    std::string output;
    serializeJson(doc, output);
    return output;
}

bool JsonSerializer::deserializeNodeConfig(const std::string& json, NodeConfig& config) {
    JsonDocument doc;
    DeserializationError error = deserializeJson(doc, json);

    if (error) {
        hal::log_error("JSON parse error: " + std::string(error.c_str()));
        return false;
    }

    // Required fields
    if (!doc["deviceId"].is<const char*>()) {
        hal::log_error("Missing required field: deviceId");
        return false;
    }

    config.deviceId = doc["deviceId"].as<std::string>();
    config.name = doc["name"] | "";
    config.location = doc["location"] | "";
    config.intervalSeconds = doc["intervalSeconds"] | 60;

    // Parse sensors array
    config.sensors.clear();
    if (doc["sensors"].is<JsonArray>()) {
        JsonArray sensorsArr = doc["sensors"].as<JsonArray>();
        for (JsonObject sensorObj : sensorsArr) {
            SensorConfig sensor;
            sensor.type = sensorObj["type"] | "";
            sensor.enabled = sensorObj["enabled"] | false;
            sensor.pin = sensorObj["pin"] | -1;

            if (!sensor.type.empty()) {
                config.sensors.push_back(sensor);
            }
        }
    }

    // Parse connection object
    if (doc["connection"].is<JsonObject>()) {
        JsonObject connObj = doc["connection"].as<JsonObject>();
        config.connection.mode = connObj["mode"] | "http";
        config.connection.endpoint = connObj["endpoint"] | "";
    }

    return config.isValid();
}

bool JsonSerializer::deserializeReading(const std::string& json, Reading& reading) {
    JsonDocument doc;
    DeserializationError error = deserializeJson(doc, json);

    if (error) {
        hal::log_error("JSON parse error: " + std::string(error.c_str()));
        return false;
    }

    reading.deviceId = doc["deviceId"] | "";
    reading.type = doc["type"] | "";
    reading.value = doc["value"] | 0.0f;
    reading.unit = doc["unit"] | "";
    reading.timestamp = doc["timestamp"] | 0ULL;

    return !reading.deviceId.empty() && !reading.type.empty();
}

} // namespace data
