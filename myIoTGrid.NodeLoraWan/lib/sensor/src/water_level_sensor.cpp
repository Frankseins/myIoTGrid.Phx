/**
 * @file water_level_sensor.cpp
 * @brief Ultrasonic Water Level Sensor Implementation
 *
 * @version 1.0.0
 * @date 2025-12-10
 */

#ifdef PLATFORM_ESP32
#include <Arduino.h>
#endif

#include "water_level_sensor.h"
#include "hal/hal.h"
#include "config.h"

#include <algorithm>
#include <cmath>

// ============================================================
// CONSTRUCTOR
// ============================================================

WaterLevelSensor::WaterLevelSensor(uint8_t triggerPin, uint8_t echoPin)
    : triggerPin_(triggerPin), echoPin_(echoPin) {
    // Initialize filter buffer
    readings_.fill(0.0f);
}

// ============================================================
// INITIALIZATION
// ============================================================

bool WaterLevelSensor::begin() {
    LOG_INFO("Initializing water level sensor (Trig=%d, Echo=%d)",
             triggerPin_, echoPin_);

#ifdef PLATFORM_ESP32
    // Configure GPIO pins
    pinMode(triggerPin_, OUTPUT);
    pinMode(echoPin_, INPUT);

    // Ensure trigger is low
    digitalWrite(triggerPin_, LOW);
    delay(50);

    // Test measurement
    float testDistance = measureDistance();
    if (testDistance < 0 || testDistance > 400) {
        LOG_WARN("Water level sensor test measurement failed (%.1f cm)", testDistance);
        // Don't fail initialization - sensor might work later
    } else {
        LOG_INFO("Water level sensor test: %.1f cm", testDistance);
    }
#else
    LOG_INFO("[SIM] Water level sensor initialized");
#endif

    initialized_ = true;
    return true;
}

// ============================================================
// MEASUREMENT
// ============================================================

float WaterLevelSensor::read() {
    if (!initialized_) {
        LOG_WARN("Water level sensor not initialized");
        return -1.0f;
    }

    // Get distance to water
    float distance = measureDistance();

    if (distance < 0) {
        LOG_WARN("Water level measurement failed");
        return lastWaterLevel_;  // Return last known value
    }

    // Add to median filter
    addToFilter(distance);
    lastDistance_ = distance;

    // Get filtered distance
    float filteredDistance = getMedian();

    // Calculate water level
    // Water Level = Mount Height - Distance to Water
    float waterLevel = mountHeight_ - filteredDistance;

    // Clamp to valid range
    if (waterLevel < 0) waterLevel = 0;
    if (waterLevel > mountHeight_) waterLevel = mountHeight_;

    lastWaterLevel_ = waterLevel;

    LOG_DEBUG("Water level: %.1f cm (distance: %.1f cm, filtered: %.1f cm)",
              waterLevel, distance, filteredDistance);

    return waterLevel;
}

float WaterLevelSensor::measureDistance() {
#ifdef PLATFORM_ESP32
    // Send trigger pulse
    digitalWrite(triggerPin_, LOW);
    delayMicroseconds(2);
    digitalWrite(triggerPin_, HIGH);
    delayMicroseconds(10);
    digitalWrite(triggerPin_, LOW);

    // Measure echo duration
    unsigned long duration = pulseIn(echoPin_, HIGH, WATER_LEVEL_TIMEOUT_US);

    if (duration == 0) {
        // Timeout - no echo received
        LOG_DEBUG("Ultrasonic timeout");
        return -1.0f;
    }

    // Calculate distance
    // Speed of sound = 343 m/s = 0.0343 cm/µs
    // Distance = (duration * 0.0343) / 2 (divide by 2 for round trip)
    float distance = (duration * 0.0343f) / 2.0f;

    // Validate range (HC-SR04 range: 2-400 cm)
    if (distance < 2.0f || distance > 400.0f) {
        LOG_DEBUG("Ultrasonic out of range: %.1f cm", distance);
        return -1.0f;
    }

    return distance;
#else
    // Simulate distance (50-150 cm with slow variation)
    float baseDistance = 100.0f;
    float variation = 30.0f * sinf(hal::millis() / 60000.0f);  // Slow wave
    float noise = ((rand() % 100) - 50) / 50.0f;  // ±1 cm noise

    return baseDistance + variation + noise;
#endif
}

float WaterLevelSensor::getDistanceToWater() {
    if (!initialized_) return -1.0f;

    // Take a single measurement
    float distance = measureDistance();
    if (distance < 0) return lastDistance_;

    return distance;
}

bool WaterLevelSensor::isAlarmActive() {
    float level = read();
    return level >= alarmLevel_;
}

// ============================================================
// MEDIAN FILTER
// ============================================================

void WaterLevelSensor::addToFilter(float reading) {
    readings_[readingIndex_] = reading;
    readingIndex_ = (readingIndex_ + 1) % FILTER_SIZE;

    if (readingIndex_ == 0) {
        filterFilled_ = true;
    }
}

float WaterLevelSensor::getMedian() {
    // Copy to temporary array for sorting
    std::array<float, FILTER_SIZE> sorted = readings_;

    // Determine how many valid readings we have
    size_t count = filterFilled_ ? FILTER_SIZE : readingIndex_;
    if (count == 0) return 0.0f;
    if (count == 1) return sorted[0];

    // Sort the valid portion
    std::sort(sorted.begin(), sorted.begin() + count);

    // Return median
    if (count % 2 == 0) {
        // Even count: average of two middle values
        return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0f;
    } else {
        // Odd count: middle value
        return sorted[count / 2];
    }
}
