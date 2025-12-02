#include "sensor_factory.h"
#include "simulated_sensor.h"
#include "hal/hal.h"

namespace sensor {

std::unique_ptr<ISensor> SensorFactory::create(
    const std::string& type,
    int pin,
    bool simulate
) {
    // Check if type is valid
    const SensorTypeInfo* info = SensorTypes::getInfo(type);
    if (!info) {
        hal::log_error("SensorFactory: Unknown sensor type: " + type);
        return nullptr;
    }

    // On native platform or when simulation is forced, always use SimulatedSensor
#ifdef PLATFORM_NATIVE
    (void)pin;  // Unused on native
    (void)simulate;  // Always simulate on native
    hal::log_info("SensorFactory: Creating SimulatedSensor for type: " + type);
    return std::make_unique<SimulatedSensor>(type);
#else
    // ESP32 platform
    if (simulate || pin < 0) {
        hal::log_info("SensorFactory: Creating SimulatedSensor for type: " + type);
        return std::make_unique<SimulatedSensor>(type);
    }

    // Hardware sensor creation for ESP32
    // TODO: Implement hardware sensor classes in Sprint S2+
    hal::log_warn("SensorFactory: Hardware sensors not yet implemented, using simulation for: " + type);
    return std::make_unique<SimulatedSensor>(type);
#endif
}

bool SensorFactory::isTypeSupported(const std::string& type) {
    return SensorTypes::getInfo(type) != nullptr;
}

std::vector<std::string> SensorFactory::getSupportedTypes() {
    return {
        "temperature",
        "humidity",
        "pressure",
        "water_level",
        "co2",
        "pm25",
        "pm10",
        "soil_moisture",
        "light",
        "uv",
        "wind_speed",
        "rainfall",
        "battery",
        "rssi"
    };
}

} // namespace sensor
