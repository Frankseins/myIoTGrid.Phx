/**
 * @file power_manager.cpp
 * @brief Power Management Implementation
 *
 * @version 1.0.0
 * @date 2025-12-10
 *
 * Sprint: LoRa-02 - Grid.Sensor LoRaWAN Firmware
 */

#ifdef PLATFORM_ESP32
#include <Arduino.h>
#endif

#include "power_manager.h"
#include "config.h"
#include "hal/hal.h"

#ifdef PLATFORM_ESP32
#include <esp_sleep.h>
#include <esp_system.h>
#include <driver/rtc_io.h>
#include <driver/adc.h>
#include <esp_adc_cal.h>
#endif

// ============================================================
// STATIC MEMBER INITIALIZATION
// ============================================================

float PowerManager::minVoltage_ = BATTERY_MIN_VOLTAGE;
float PowerManager::maxVoltage_ = BATTERY_MAX_VOLTAGE;
uint8_t PowerManager::lowBatteryPercent_ = BATTERY_LOW_THRESHOLD;
uint8_t PowerManager::criticalBatteryPercent_ = 10;
PowerState PowerManager::currentState_ = PowerState::ACTIVE;
bool PowerManager::initialized_ = false;
float PowerManager::adcCalibrationFactor_ = 1.0f;

// ============================================================
// INITIALIZATION
// ============================================================

bool PowerManager::init() {
    if (initialized_) return true;

#ifdef PLATFORM_ESP32
    // Configure ADC for battery monitoring
    adc1_config_width(ADC_WIDTH_BIT_12);
    adc1_config_channel_atten(ADC1_CHANNEL_0, ADC_ATTEN_DB_12);

    // Check if this is a deep sleep wake-up
    esp_sleep_wakeup_cause_t cause = esp_sleep_get_wakeup_cause();
    if (cause == ESP_SLEEP_WAKEUP_TIMER) {
        LOG_INFO("Woke up from deep sleep (timer)");
    } else if (cause == ESP_SLEEP_WAKEUP_EXT0) {
        LOG_INFO("Woke up from deep sleep (button)");
    }
#endif

    initialized_ = true;
    LOG_INFO("Power manager initialized");
    return true;
}

// ============================================================
// DEEP SLEEP
// ============================================================

void PowerManager::deepSleep(uint32_t seconds) {
    if (seconds < MIN_DEEP_SLEEP_SECONDS) {
        seconds = MIN_DEEP_SLEEP_SECONDS;
    }

    LOG_INFO("Entering deep sleep for %u seconds", seconds);

    // Flush serial output
#ifdef PLATFORM_ESP32
    Serial.flush();
    delay(10);

    // Configure wake-up source
    esp_sleep_enable_timer_wakeup((uint64_t)seconds * 1000000ULL);

    // Configure button wake-up (GPIO0 on Heltec)
    esp_sleep_enable_ext0_wakeup((gpio_num_t)USER_BUTTON_PIN, 0);  // Wake on LOW

    // Enter deep sleep
    esp_deep_sleep_start();
#else
    LOG_INFO("[SIM] Deep sleep for %u seconds", seconds);
    hal::delay_ms(1000);  // Simulate short sleep
#endif
}

uint32_t PowerManager::deepSleepAdaptive(uint32_t baseSeconds) {
    uint32_t sleepSeconds = baseSeconds;

    // Get battery level
    uint8_t batteryPercent = getBatteryPercent();

    // Adjust sleep duration based on battery
    if (batteryPercent < criticalBatteryPercent_) {
        // Critical battery: sleep much longer
        sleepSeconds = baseSeconds * 4;
        LOG_WARN("Critical battery (%u%%), extending sleep to %us",
                 batteryPercent, sleepSeconds);
    } else if (batteryPercent < lowBatteryPercent_) {
        // Low battery: sleep longer
        sleepSeconds = (uint32_t)(baseSeconds * LOW_BATTERY_SLEEP_MULTIPLIER);
        LOG_WARN("Low battery (%u%%), extending sleep to %us",
                 batteryPercent, sleepSeconds);
    }

    // Limit maximum sleep time
    if (sleepSeconds > MAX_TX_INTERVAL_SECONDS) {
        sleepSeconds = MAX_TX_INTERVAL_SECONDS;
    }

    // Enter deep sleep
    deepSleep(sleepSeconds);

    return sleepSeconds;
}

WakeReason PowerManager::getWakeReason() {
#ifdef PLATFORM_ESP32
    esp_sleep_wakeup_cause_t cause = esp_sleep_get_wakeup_cause();

    switch (cause) {
        case ESP_SLEEP_WAKEUP_TIMER:
            return WakeReason::TIMER;
        case ESP_SLEEP_WAKEUP_EXT0:
        case ESP_SLEEP_WAKEUP_EXT1:
            return WakeReason::BUTTON;
        case ESP_SLEEP_WAKEUP_UNDEFINED:
            return WakeReason::RESET;
        default:
            return WakeReason::UNKNOWN;
    }
#else
    return WakeReason::RESET;
#endif
}

bool PowerManager::wasDeepSleep() {
#ifdef PLATFORM_ESP32
    esp_sleep_wakeup_cause_t cause = esp_sleep_get_wakeup_cause();
    return cause != ESP_SLEEP_WAKEUP_UNDEFINED;
#else
    return false;
#endif
}

// ============================================================
// BATTERY MONITORING
// ============================================================

uint16_t PowerManager::readBatteryAdc() {
#ifdef PLATFORM_ESP32
    // Read ADC multiple times and average
    uint32_t sum = 0;
    const int samples = 10;

    for (int i = 0; i < samples; i++) {
        sum += adc1_get_raw(ADC1_CHANNEL_0);
        delay(1);
    }

    return sum / samples;
#else
    // Simulate ~3.8V battery (about 67%)
    return 2500;
#endif
}

float PowerManager::getBatteryVoltage() {
    uint16_t raw = readBatteryAdc();

    // Convert ADC reading to voltage
    // Heltec LoRa32 V3 uses voltage divider: 390k / 100k = 4.9
    const float ADC_REF = 3.3f;
    const float ADC_MAX = 4095.0f;

    float voltage = (raw / ADC_MAX) * ADC_REF * BATTERY_DIVIDER_RATIO;
    voltage *= adcCalibrationFactor_;

    return voltage;
}

uint8_t PowerManager::getBatteryPercent() {
    float voltage = getBatteryVoltage();

    // Clamp to valid range
    if (voltage >= maxVoltage_) return 100;
    if (voltage <= minVoltage_) return 0;

    // Linear interpolation
    float percent = (voltage - minVoltage_) / (maxVoltage_ - minVoltage_) * 100.0f;

    return (uint8_t)percent;
}

bool PowerManager::isBatteryLow() {
    return getBatteryPercent() < lowBatteryPercent_;
}

bool PowerManager::isBatteryCritical() {
    return getBatteryPercent() < criticalBatteryPercent_;
}

// ============================================================
// POWER MODES
// ============================================================

void PowerManager::enableLowPower() {
    if (currentState_ == PowerState::LOW_POWER) return;

#ifdef PLATFORM_ESP32
    // Reduce CPU frequency
    // setCpuFrequencyMhz(80);  // Reduce from 240MHz to 80MHz

    // Disable WiFi (not used in LoRaWAN mode)
    // WiFi.mode(WIFI_OFF);

    // Disable Bluetooth
    // btStop();
#endif

    currentState_ = PowerState::LOW_POWER;
    LOG_INFO("Low power mode enabled");
}

void PowerManager::disableLowPower() {
    if (currentState_ != PowerState::LOW_POWER) return;

#ifdef PLATFORM_ESP32
    // Restore CPU frequency
    // setCpuFrequencyMhz(240);
#endif

    currentState_ = PowerState::ACTIVE;
    LOG_INFO("Low power mode disabled");
}

PowerState PowerManager::getState() {
    return currentState_;
}

// ============================================================
// PERIPHERAL CONTROL
// ============================================================

void PowerManager::setLedEnabled(bool enable) {
#ifdef PLATFORM_ESP32
    if (enable) {
        pinMode(LED_PIN, OUTPUT);
    } else {
        digitalWrite(LED_PIN, LOW);
        // Could also configure as input to save power
    }
#endif
    LOG_DEBUG("LED %s", enable ? "enabled" : "disabled");
}

void PowerManager::setDisplayEnabled(bool enable) {
    // Display is controlled separately by OledDisplay class
    LOG_DEBUG("Display %s", enable ? "enabled" : "disabled");
}

void PowerManager::setSensorsEnabled(bool enable) {
    // Sensors are controlled separately
    LOG_DEBUG("Sensors %s", enable ? "enabled" : "disabled");
}

// ============================================================
// CONFIGURATION
// ============================================================

void PowerManager::setBatteryThresholds(float minVoltage, float maxVoltage) {
    minVoltage_ = minVoltage;
    maxVoltage_ = maxVoltage;
}

void PowerManager::setLowBatteryThreshold(uint8_t percent) {
    lowBatteryPercent_ = percent;
}

void PowerManager::setCriticalBatteryThreshold(uint8_t percent) {
    criticalBatteryPercent_ = percent;
}
