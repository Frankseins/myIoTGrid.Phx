#ifndef SENSOR_FACTORY_H
#define SENSOR_FACTORY_H

#include "sensor_interface.h"
#include <memory>
#include <string>
#include <vector>

namespace sensor {

/**
 * Factory for creating sensor instances
 *
 * Depending on platform and configuration:
 * - Native platform: Always creates SimulatedSensor
 * - ESP32 with SIMULATE_SENSORS=1: Creates SimulatedSensor
 * - ESP32 with SIMULATE_SENSORS=0: Creates hardware-specific sensor
 */
class SensorFactory {
public:
    /**
     * Create a sensor instance
     *
     * @param type Sensor type code (e.g., "temperature")
     * @param pin GPIO pin number (-1 for simulation)
     * @param simulate Force simulation mode (overrides platform default)
     * @return Unique pointer to ISensor, or nullptr if type unknown
     */
    static std::unique_ptr<ISensor> create(
        const std::string& type,
        int pin = -1,
        bool simulate = SIMULATE_SENSORS
    );

    /**
     * Check if a sensor type is supported
     * @param type Sensor type code
     * @return true if supported
     */
    static bool isTypeSupported(const std::string& type);

    /**
     * Get list of all supported sensor types
     * @return Vector of type codes
     */
    static std::vector<std::string> getSupportedTypes();
};

} // namespace sensor

#endif // SENSOR_FACTORY_H
