#include "sensor_interface.h"
#include <unordered_map>

namespace sensor {
namespace SensorTypes {

namespace {

// Lookup table for sensor types
const std::unordered_map<std::string, const SensorTypeInfo*> sensorTypeMap = {
    {"temperature", &TEMPERATURE},
    {"humidity", &HUMIDITY},
    {"pressure", &PRESSURE},
    {"water_level", &WATER_LEVEL},
    {"co2", &CO2},
    {"pm25", &PM25},
    {"pm10", &PM10},
    {"soil_moisture", &SOIL_MOISTURE},
    {"light", &LIGHT},
    {"uv", &UV},
    {"wind_speed", &WIND_SPEED},
    {"rainfall", &RAINFALL},
    {"battery", &BATTERY},
    {"rssi", &RSSI}
};

} // anonymous namespace

const SensorTypeInfo* getInfo(const std::string& type) {
    auto it = sensorTypeMap.find(type);
    if (it != sensorTypeMap.end()) {
        return it->second;
    }
    return nullptr;
}

} // namespace SensorTypes
} // namespace sensor
