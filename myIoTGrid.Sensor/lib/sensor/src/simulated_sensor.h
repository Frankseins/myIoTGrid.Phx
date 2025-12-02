#ifndef SIMULATED_SENSOR_H
#define SIMULATED_SENSOR_H

#include "sensor_interface.h"
#include <string>
#include <cmath>

namespace sensor {

/**
 * SimulatedSensor - Generates realistic sensor values for testing
 *
 * Features:
 * - Day/night cycle simulation (24h sine wave)
 * - Random noise for realistic variation
 * - Value clamping to valid range
 * - Supports all standard sensor types
 */
class SimulatedSensor : public ISensor {
public:
    /**
     * Create a simulated sensor
     * @param typeCode Sensor type code (e.g., "temperature")
     * @throws std::invalid_argument if type is unknown
     */
    explicit SimulatedSensor(const std::string& typeCode);

    /**
     * Create a simulated sensor with custom parameters
     * @param typeCode Sensor type code
     * @param baseValue Center value for simulation
     * @param amplitude Variation range (±)
     * @param noiseRange Random noise range (±)
     */
    SimulatedSensor(const std::string& typeCode,
                    float baseValue,
                    float amplitude,
                    float noiseRange);

    ~SimulatedSensor() override = default;

    // ISensor interface implementation
    std::string getType() const override;
    std::string getUnit() const override;
    float getMinValue() const override;
    float getMaxValue() const override;
    bool begin() override;
    float read() override;
    bool isReady() const override;
    std::string getName() const override;

    /**
     * Set the time offset for simulation (useful for testing)
     * @param offsetSeconds Offset in seconds
     */
    void setTimeOffset(int32_t offsetSeconds);

private:
    std::string typeCode_;
    const SensorTypeInfo* typeInfo_;

    float baseValue_;
    float amplitude_;
    float noiseRange_;

    bool initialized_;
    int32_t timeOffset_;

    /**
     * Generate random float in range [-range, +range]
     */
    float randomNoise(float range) const;

    /**
     * Clamp value to valid sensor range
     */
    float clamp(float value) const;

    /**
     * Calculate day cycle factor (0.0 to 1.0)
     * Peak at 14:00, low at 02:00
     */
    float getDayCycleFactor() const;
};

} // namespace sensor

#endif // SIMULATED_SENSOR_H
