#include "simulated_sensor.h"
#include "hal/hal.h"
#include <cstdlib>
#include <ctime>
#include <stdexcept>
#include <cmath>

#ifndef M_PI
#define M_PI 3.14159265358979323846
#endif

namespace sensor {

SimulatedSensor::SimulatedSensor(const std::string& typeCode)
    : typeCode_(typeCode)
    , typeInfo_(nullptr)
    , baseValue_(0.0f)
    , amplitude_(0.0f)
    , noiseRange_(0.0f)
    , initialized_(false)
    , timeOffset_(0)
{
    typeInfo_ = SensorTypes::getInfo(typeCode);
    if (!typeInfo_) {
        throw std::invalid_argument("Unknown sensor type: " + typeCode);
    }

    // Use default simulation parameters from type info
    baseValue_ = typeInfo_->baseValue;
    amplitude_ = typeInfo_->amplitude;
    noiseRange_ = typeInfo_->noise;
}

SimulatedSensor::SimulatedSensor(const std::string& typeCode,
                                 float baseValue,
                                 float amplitude,
                                 float noiseRange)
    : typeCode_(typeCode)
    , typeInfo_(nullptr)
    , baseValue_(baseValue)
    , amplitude_(amplitude)
    , noiseRange_(noiseRange)
    , initialized_(false)
    , timeOffset_(0)
{
    typeInfo_ = SensorTypes::getInfo(typeCode);
    if (!typeInfo_) {
        throw std::invalid_argument("Unknown sensor type: " + typeCode);
    }
}

std::string SimulatedSensor::getType() const {
    return typeCode_;
}

std::string SimulatedSensor::getUnit() const {
    return typeInfo_ ? typeInfo_->unit : "";
}

float SimulatedSensor::getMinValue() const {
    return typeInfo_ ? typeInfo_->minValue : 0.0f;
}

float SimulatedSensor::getMaxValue() const {
    return typeInfo_ ? typeInfo_->maxValue : 0.0f;
}

bool SimulatedSensor::begin() {
    // Seed random number generator
    static bool seeded = false;
    if (!seeded) {
        std::srand(static_cast<unsigned>(std::time(nullptr)));
        seeded = true;
    }

    initialized_ = true;
    hal::log_info("SimulatedSensor [" + typeCode_ + "] initialized");
    return true;
}

float SimulatedSensor::read() {
    if (!initialized_) {
        hal::log_error("SimulatedSensor [" + typeCode_ + "] not initialized");
        return NAN;
    }

    // Calculate value based on day cycle + noise
    float dayCycle = getDayCycleFactor();
    float variation = amplitude_ * dayCycle;
    float noise = randomNoise(noiseRange_);

    float value = baseValue_ + variation + noise;

    // Clamp to valid range
    value = clamp(value);

    return value;
}

bool SimulatedSensor::isReady() const {
    return initialized_;
}

std::string SimulatedSensor::getName() const {
    return std::string("Simulated ") + (typeInfo_ ? typeInfo_->name : typeCode_);
}

void SimulatedSensor::setTimeOffset(int32_t offsetSeconds) {
    timeOffset_ = offsetSeconds;
}

float SimulatedSensor::randomNoise(float range) const {
    if (range <= 0.0f) return 0.0f;

    // Generate random float between -range and +range
    float random = static_cast<float>(std::rand()) / static_cast<float>(RAND_MAX);
    return (random * 2.0f - 1.0f) * range;
}

float SimulatedSensor::clamp(float value) const {
    if (!typeInfo_) return value;

    if (value < typeInfo_->minValue) return typeInfo_->minValue;
    if (value > typeInfo_->maxValue) return typeInfo_->maxValue;
    return value;
}

float SimulatedSensor::getDayCycleFactor() const {
    // Get current time
    uint64_t timestamp = hal::timestamp() + timeOffset_;

    // Convert to seconds since midnight (approximate)
    // Assuming UTC, adjust as needed
    uint32_t secondsInDay = timestamp % 86400;

    // Convert to hours (0-24)
    float hours = static_cast<float>(secondsInDay) / 3600.0f;

    // Sine wave with peak at 14:00 (2 PM) and trough at 02:00 (2 AM)
    // Phase shift: sin wave peaks at PI/2, we want peak at 14:00
    // 14:00 = 14 hours, normalize to 0-2PI: (14/24) * 2PI = 14PI/12
    // We need: sin(2PI * h/24 - phase) = 1 when h = 14
    // sin(x) = 1 when x = PI/2
    // 2PI * 14/24 - phase = PI/2
    // phase = 7PI/6 - PI/2 = 7PI/6 - 3PI/6 = 4PI/6 = 2PI/3

    float phase = 2.0f * M_PI / 3.0f;
    float angle = 2.0f * M_PI * hours / 24.0f - phase;

    // Return value between -1 and 1
    return std::sin(angle);
}

} // namespace sensor
