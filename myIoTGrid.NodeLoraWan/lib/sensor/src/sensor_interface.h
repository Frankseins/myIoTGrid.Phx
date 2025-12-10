/**
 * @file sensor_interface.h
 * @brief Abstract Sensor Interface
 *
 * Defines the ISensor interface for all sensor implementations.
 *
 * @version 1.0.0
 * @date 2025-12-10
 *
 * Sprint: LoRa-02 - Grid.Sensor LoRaWAN Firmware
 */

#pragma once

#include <string>
#include <cstdint>

/**
 * @brief Abstract Sensor Interface
 *
 * Base class for all sensor implementations.
 */
class ISensor {
public:
    virtual ~ISensor() = default;

    // === Sensor Identification ===

    /**
     * @brief Get sensor type identifier
     * @return Type string (e.g., "temperature")
     */
    virtual std::string getType() const = 0;

    /**
     * @brief Get measurement unit
     * @return Unit string (e.g., "°C")
     */
    virtual std::string getUnit() const = 0;

    /**
     * @brief Get minimum valid value
     * @return Minimum value
     */
    virtual float getMinValue() const = 0;

    /**
     * @brief Get maximum valid value
     * @return Maximum value
     */
    virtual float getMaxValue() const = 0;

    // === Lifecycle ===

    /**
     * @brief Initialize sensor hardware
     * @return true if initialization successful
     */
    virtual bool begin() = 0;

    /**
     * @brief Check if sensor is ready for reading
     * @return true if ready
     */
    virtual bool isReady() const = 0;

    // === Reading ===

    /**
     * @brief Read current sensor value
     * @return Measured value in sensor's native unit
     */
    virtual float read() = 0;

    /**
     * @brief Check if last reading is valid
     * @return true if value is within valid range
     */
    virtual bool isValid() const {
        float value = const_cast<ISensor*>(this)->read();
        return value >= getMinValue() && value <= getMaxValue();
    }
};
