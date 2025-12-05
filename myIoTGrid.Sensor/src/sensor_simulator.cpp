/**
 * myIoTGrid.Sensor - Sensor Simulator Implementation
 */

#include "sensor_simulator.h"

// Use C math header to avoid ARM64 libstdc++ bug with cmath and numeric_limits
extern "C" {
#include <math.h>
}

#ifndef M_PI
#define M_PI 3.14159265358979323846
#endif

SensorSimulator::SensorSimulator()
    : _profile(SimulationProfile::NORMAL)
    , _dailyCycleEnabled(true)
    , _simulatedHour(-1)
    , _lastUpdate(0) {
    _current = {0, 0, 0, 0, 0, 0, 0};
}

void SensorSimulator::init(SimulationProfile profile) {
    _profile = profile;
    reset();
    Serial.printf("[Simulator] Initialized with profile: %s\n", getProfileName(profile));
}

void SensorSimulator::setProfile(SimulationProfile profile) {
    if (_profile != profile) {
        _profile = profile;
        Serial.printf("[Simulator] Profile changed to: %s\n", getProfileName(profile));
        // Gradually transition to new profile (don't reset)
    }
}

SimulationProfile SensorSimulator::getProfile() const {
    return _profile;
}

const char* SensorSimulator::getProfileName(SimulationProfile profile) {
    switch (profile) {
        case SimulationProfile::NORMAL:  return "Normal";
        case SimulationProfile::WINTER:  return "Winter";
        case SimulationProfile::SUMMER:  return "Summer";
        case SimulationProfile::STORM:   return "Storm";
        case SimulationProfile::STRESS:  return "Stress";
        default:                         return "Unknown";
    }
}

SensorSimulator::ProfileRange SensorSimulator::getProfileRange() const {
    ProfileRange range;

    switch (_profile) {
        case SimulationProfile::NORMAL:
            range = {
                .tempMin = 18.0f, .tempMax = 25.0f,
                .humidMin = 40.0f, .humidMax = 70.0f,
                .pressMin = 1010.0f, .pressMax = 1025.0f,
                .co2Min = 400.0f, .co2Max = 800.0f,
                .lightMin = 100.0f, .lightMax = 500.0f,
                .soilMin = 30.0f, .soilMax = 70.0f
            };
            break;

        case SimulationProfile::WINTER:
            range = {
                .tempMin = -5.0f, .tempMax = 10.0f,
                .humidMin = 60.0f, .humidMax = 90.0f,
                .pressMin = 990.0f, .pressMax = 1020.0f,
                .co2Min = 350.0f, .co2Max = 500.0f,
                .lightMin = 50.0f, .lightMax = 200.0f,
                .soilMin = 50.0f, .soilMax = 90.0f
            };
            break;

        case SimulationProfile::SUMMER:
            range = {
                .tempMin = 25.0f, .tempMax = 35.0f,
                .humidMin = 30.0f, .humidMax = 50.0f,
                .pressMin = 1005.0f, .pressMax = 1020.0f,
                .co2Min = 380.0f, .co2Max = 600.0f,
                .lightMin = 500.0f, .lightMax = 2000.0f,
                .soilMin = 10.0f, .soilMax = 40.0f
            };
            break;

        case SimulationProfile::STORM:
            range = {
                .tempMin = 18.0f, .tempMax = 22.0f,
                .humidMin = 80.0f, .humidMax = 95.0f,
                .pressMin = 980.0f, .pressMax = 1000.0f,
                .co2Min = 400.0f, .co2Max = 700.0f,
                .lightMin = 20.0f, .lightMax = 100.0f,
                .soilMin = 70.0f, .soilMax = 100.0f
            };
            break;

        case SimulationProfile::STRESS:
            range = {
                .tempMin = 0.0f, .tempMax = 50.0f,
                .humidMin = 0.0f, .humidMax = 100.0f,
                .pressMin = 950.0f, .pressMax = 1050.0f,
                .co2Min = 300.0f, .co2Max = 2000.0f,
                .lightMin = 0.0f, .lightMax = 10000.0f,
                .soilMin = 0.0f, .soilMax = 100.0f
            };
            break;

        default:
            // Default to normal
            range = {
                .tempMin = 18.0f, .tempMax = 25.0f,
                .humidMin = 40.0f, .humidMax = 70.0f,
                .pressMin = 1010.0f, .pressMax = 1025.0f,
                .co2Min = 400.0f, .co2Max = 800.0f,
                .lightMin = 100.0f, .lightMax = 500.0f,
                .soilMin = 30.0f, .soilMax = 70.0f
            };
    }

    return range;
}

void SensorSimulator::reset() {
    ProfileRange range = getProfileRange();

    // Initialize to middle of range
    _current.temperature = (range.tempMin + range.tempMax) / 2.0f;
    _current.humidity = (range.humidMin + range.humidMax) / 2.0f;
    _current.pressure = (range.pressMin + range.pressMax) / 2.0f;
    _current.co2 = (range.co2Min + range.co2Max) / 2.0f;
    _current.light = (range.lightMin + range.lightMax) / 2.0f;
    _current.soilMoisture = (range.soilMin + range.soilMax) / 2.0f;
    _current.timestamp = millis();

    Serial.println("[Simulator] Values reset to profile defaults");
}

int SensorSimulator::getCurrentHour() const {
    if (_simulatedHour >= 0) {
        return _simulatedHour;
    }

    // Calculate approximate hour from millis (since boot)
    // In real implementation, you'd use RTC or NTP time
    unsigned long ms = millis();
    unsigned long seconds = ms / 1000;
    unsigned long hours = (seconds / 3600) % 24;

    // Start at 6 AM for realistic daytime
    return (6 + hours) % 24;
}

float SensorSimulator::randomWalk(float current, float min, float max, float maxStep) {
    // Generate random step
    float step = ((float)random(-1000, 1001) / 1000.0f) * maxStep;

    // Apply step with some momentum/smoothing
    float newValue = current + step;

    // Apply soft boundaries (stronger pull back when near limits)
    float range = max - min;
    float center = (min + max) / 2.0f;
    float distFromCenter = newValue - center;
    float normalizedDist = distFromCenter / (range / 2.0f);

    // Pull towards center if too far out
    if (abs(normalizedDist) > 0.8f) {
        float pullStrength = (abs(normalizedDist) - 0.8f) * 0.3f;
        newValue -= distFromCenter * pullStrength;
    }

    // Hard clamp
    if (newValue < min) newValue = min;
    if (newValue > max) newValue = max;

    return newValue;
}

float SensorSimulator::applyDailyCycle(float value, float min, float max, float amplitude) {
    if (!_dailyCycleEnabled) {
        return value;
    }

    int hour = getCurrentHour();

    // Sine wave with peak at 14:00 (2 PM)
    // sin(0) = 0 at 8:00, sin(pi/2) = 1 at 14:00, sin(pi) = 0 at 20:00
    float radians = (hour - 8) * M_PI / 12.0f;
    float cycleOffset = sin(radians) * amplitude;

    float newValue = value + cycleOffset;

    // Clamp to range
    if (newValue < min) newValue = min;
    if (newValue > max) newValue = max;

    return newValue;
}

void SensorSimulator::update() {
    ProfileRange range = getProfileRange();

    // Determine step sizes based on profile
    float tempStep = (_profile == SimulationProfile::STRESS) ? 2.0f : 0.3f;
    float humidStep = (_profile == SimulationProfile::STRESS) ? 5.0f : 1.0f;
    float pressStep = (_profile == SimulationProfile::STORM) ? 1.0f : 0.2f;
    float co2Step = (_profile == SimulationProfile::STRESS) ? 50.0f : 10.0f;
    float lightStep = (_profile == SimulationProfile::STORM) ? 50.0f : 20.0f;
    float soilStep = 0.5f;

    // Update each sensor value using random walk
    _current.temperature = randomWalk(_current.temperature, range.tempMin, range.tempMax, tempStep);
    _current.humidity = randomWalk(_current.humidity, range.humidMin, range.humidMax, humidStep);
    _current.pressure = randomWalk(_current.pressure, range.pressMin, range.pressMax, pressStep);
    _current.co2 = randomWalk(_current.co2, range.co2Min, range.co2Max, co2Step);
    _current.light = randomWalk(_current.light, range.lightMin, range.lightMax, lightStep);
    _current.soilMoisture = randomWalk(_current.soilMoisture, range.soilMin, range.soilMax, soilStep);

    // Apply daily cycle effects
    float tempAmplitude = (range.tempMax - range.tempMin) * 0.3f;  // 30% of range
    _current.temperature = applyDailyCycle(_current.temperature, range.tempMin, range.tempMax, tempAmplitude);

    // Light has strongest daily cycle
    float lightAmplitude = (range.lightMax - range.lightMin) * 0.8f;
    _current.light = applyDailyCycle(_current.light, range.lightMin, range.lightMax, lightAmplitude);

    // CO2 inverse cycle (higher at night due to no photosynthesis)
    float co2Amplitude = (range.co2Max - range.co2Min) * 0.2f;
    _current.co2 = _current.co2 - applyDailyCycle(0, 0, co2Amplitude * 2, co2Amplitude);
    if (_current.co2 < range.co2Min) _current.co2 = range.co2Min;
    if (_current.co2 > range.co2Max) _current.co2 = range.co2Max;

    _current.timestamp = millis();
    _lastUpdate = millis();
}

SimulatedReading SensorSimulator::getReading() const {
    return _current;
}

float SensorSimulator::getTemperature() const {
    return _current.temperature;
}

float SensorSimulator::getHumidity() const {
    return _current.humidity;
}

float SensorSimulator::getPressure() const {
    return _current.pressure;
}

float SensorSimulator::getCO2() const {
    return _current.co2;
}

float SensorSimulator::getLight() const {
    return _current.light;
}

float SensorSimulator::getSoilMoisture() const {
    return _current.soilMoisture;
}

void SensorSimulator::setDailyCycleEnabled(bool enabled) {
    _dailyCycleEnabled = enabled;
}

bool SensorSimulator::isDailyCycleEnabled() const {
    return _dailyCycleEnabled;
}

void SensorSimulator::setSimulatedHour(int hour) {
    _simulatedHour = hour;
}
