/**
 * @file water_level_sensor.h
 * @brief Ultrasonic Water Level Sensor
 *
 * Measures water level using HC-SR04 or JSN-SR04T ultrasonic sensor.
 * Uses median filtering for stable readings.
 *
 * @version 1.0.0
 * @date 2025-12-10
 *
 * Sprint: LoRa-02 - Grid.Sensor LoRaWAN Firmware
 */

#pragma once

#include "sensor_interface.h"
#include "config.h"

#include <array>
#include <cstdint>

/**
 * @brief Ultrasonic Water Level Sensor
 *
 * Measures water level using distance-to-water measurement.
 * The sensor is mounted at a known height above the water.
 * Water level = Mount Height - Distance to Water
 *
 * Features:
 * - Median filter for noise reduction
 * - Configurable mount height and alarm level
 * - Supports HC-SR04 and JSN-SR04T sensors
 */
class WaterLevelSensor : public ISensor {
public:
    /**
     * @brief Construct water level sensor
     * @param triggerPin GPIO pin for trigger pulse
     * @param echoPin GPIO pin for echo measurement
     */
    WaterLevelSensor(uint8_t triggerPin = ULTRASONIC_TRIG_PIN,
                     uint8_t echoPin = ULTRASONIC_ECHO_PIN);
    ~WaterLevelSensor() override = default;

    // === ISensor Interface ===

    std::string getType() const override { return "water_level"; }
    std::string getUnit() const override { return "cm"; }
    float getMinValue() const override { return 0.0f; }
    float getMaxValue() const override { return 400.0f; }

    bool begin() override;
    bool isReady() const override { return initialized_; }
    float read() override;

    // === Configuration ===

    /**
     * @brief Set sensor mount height above ground
     * @param cm Height in centimeters
     */
    void setMountHeight(float cm) { mountHeight_ = cm; }

    /**
     * @brief Get sensor mount height
     * @return Height in centimeters
     */
    float getMountHeight() const { return mountHeight_; }

    /**
     * @brief Set water level alarm threshold
     * @param cm Water level in centimeters that triggers alarm
     */
    void setAlarmLevel(float cm) { alarmLevel_ = cm; }

    /**
     * @brief Get alarm threshold
     * @return Alarm level in centimeters
     */
    float getAlarmLevel() const { return alarmLevel_; }

    /**
     * @brief Check if water level is at or above alarm threshold
     * @return true if alarm condition is met
     */
    bool isAlarmActive();

    // === Raw Measurements ===

    /**
     * @brief Get raw distance to water surface
     * @return Distance in centimeters
     */
    float getDistanceToWater();

    /**
     * @brief Get last raw distance measurement
     * @return Last distance in centimeters
     */
    float getLastDistance() const { return lastDistance_; }

    /**
     * @brief Get last filtered water level
     * @return Last water level in centimeters
     */
    float getLastWaterLevel() const { return lastWaterLevel_; }

private:
    uint8_t triggerPin_;
    uint8_t echoPin_;
    bool initialized_ = false;

    // Configuration
    float mountHeight_ = WATER_LEVEL_MOUNT_HEIGHT_CM;
    float alarmLevel_ = WATER_LEVEL_ALARM_THRESHOLD_CM;

    // Last readings
    float lastDistance_ = 0.0f;
    float lastWaterLevel_ = 0.0f;

    // Median filter
    static constexpr size_t FILTER_SIZE = WATER_LEVEL_FILTER_SIZE;
    std::array<float, FILTER_SIZE> readings_;
    size_t readingIndex_ = 0;
    bool filterFilled_ = false;

    /**
     * @brief Measure distance to water surface
     * @return Distance in centimeters, -1 on error
     */
    float measureDistance();

    /**
     * @brief Get median of filter buffer
     * @return Median value
     */
    float getMedian();

    /**
     * @brief Add reading to filter buffer
     * @param reading Distance reading
     */
    void addToFilter(float reading);
};
