/**
 * @file power_manager.h
 * @brief Power Management for Battery-Powered Sensor Nodes
 *
 * Provides deep sleep, battery monitoring, and low power modes
 * for battery-powered LoRaWAN sensor nodes.
 *
 * @version 1.0.0
 * @date 2025-12-10
 *
 * Sprint: LoRa-02 - Grid.Sensor LoRaWAN Firmware
 */

#pragma once

#include <cstdint>

/**
 * @brief Power State
 */
enum class PowerState {
    ACTIVE,         ///< Normal operation
    LOW_POWER,      ///< Reduced clock, peripherals off
    DEEP_SLEEP      ///< Minimum power consumption
};

/**
 * @brief Wake-up Reason
 */
enum class WakeReason {
    TIMER,          ///< RTC timer wake-up
    BUTTON,         ///< User button press
    RESET,          ///< Power-on or hardware reset
    UNKNOWN         ///< Unknown wake source
};

/**
 * @brief Power Manager Class
 *
 * Manages power consumption for battery-operated sensor nodes.
 * Features:
 * - Battery voltage monitoring
 * - Deep sleep with RTC wake-up
 * - Adaptive sleep duration based on battery level
 * - Peripheral power control
 */
class PowerManager {
public:
    // === Initialization ===

    /**
     * @brief Initialize power management
     * @return true if initialization successful
     */
    static bool init();

    // === Deep Sleep ===

    /**
     * @brief Enter deep sleep mode
     *
     * Puts ESP32 into minimum power consumption mode.
     * Wake-up via RTC timer.
     *
     * @param seconds Sleep duration in seconds
     */
    static void deepSleep(uint32_t seconds);

    /**
     * @brief Enter deep sleep with adaptive duration
     *
     * Adjusts sleep duration based on battery level.
     * Low battery = longer sleep to conserve power.
     *
     * @param baseSeconds Base sleep duration
     * @return Actual sleep duration used
     */
    static uint32_t deepSleepAdaptive(uint32_t baseSeconds);

    /**
     * @brief Get wake-up reason
     * @return Wake-up reason
     */
    static WakeReason getWakeReason();

    /**
     * @brief Check if wake-up was from deep sleep
     * @return true if woken from deep sleep
     */
    static bool wasDeepSleep();

    // === Battery Monitoring ===

    /**
     * @brief Get battery voltage
     * @return Voltage in volts
     */
    static float getBatteryVoltage();

    /**
     * @brief Get battery percentage
     * @return Percentage (0-100)
     */
    static uint8_t getBatteryPercent();

    /**
     * @brief Check if battery is low
     * @return true if below threshold
     */
    static bool isBatteryLow();

    /**
     * @brief Check if battery is critical
     * @return true if critically low
     */
    static bool isBatteryCritical();

    // === Power Modes ===

    /**
     * @brief Enable low power mode
     *
     * Reduces CPU frequency and disables unused peripherals.
     */
    static void enableLowPower();

    /**
     * @brief Disable low power mode
     *
     * Restores normal operation.
     */
    static void disableLowPower();

    /**
     * @brief Get current power state
     * @return Current PowerState
     */
    static PowerState getState();

    // === Peripheral Control ===

    /**
     * @brief Enable or disable LED
     * @param enable true to enable
     */
    static void setLedEnabled(bool enable);

    /**
     * @brief Enable or disable display
     * @param enable true to enable
     */
    static void setDisplayEnabled(bool enable);

    /**
     * @brief Enable or disable sensors
     * @param enable true to enable
     */
    static void setSensorsEnabled(bool enable);

    // === Configuration ===

    /**
     * @brief Set battery voltage thresholds
     * @param minVoltage Minimum voltage (0%)
     * @param maxVoltage Maximum voltage (100%)
     */
    static void setBatteryThresholds(float minVoltage, float maxVoltage);

    /**
     * @brief Set low battery threshold percentage
     * @param percent Threshold percentage
     */
    static void setLowBatteryThreshold(uint8_t percent);

    /**
     * @brief Set critical battery threshold percentage
     * @param percent Threshold percentage
     */
    static void setCriticalBatteryThreshold(uint8_t percent);

private:
    // Battery thresholds
    static float minVoltage_;
    static float maxVoltage_;
    static uint8_t lowBatteryPercent_;
    static uint8_t criticalBatteryPercent_;

    // State
    static PowerState currentState_;
    static bool initialized_;

    // ADC calibration
    static float adcCalibrationFactor_;

    /**
     * @brief Read raw ADC value
     * @return Raw ADC value (0-4095)
     */
    static uint16_t readBatteryAdc();
};
