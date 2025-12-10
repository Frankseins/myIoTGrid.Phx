/**
 * @file bme280_sensor.cpp
 * @brief BME280 Sensor Implementation
 *
 * @version 1.0.0
 * @date 2025-12-10
 */

#ifdef PLATFORM_ESP32
#include <Arduino.h>
#include <Wire.h>
#endif

#include "bme280_sensor.h"
#include "hal/hal.h"
#include "config.h"

// Shared BME280 instance to avoid re-initialization
#ifdef PLATFORM_ESP32
static Adafruit_BME280* sharedBme = nullptr;
static uint8_t sharedBmeAddress = 0;
static int sharedBmeRefCount = 0;

static Adafruit_BME280* getSharedBme(uint8_t address) {
    if (sharedBme == nullptr || sharedBmeAddress != address) {
        if (sharedBme != nullptr) {
            delete sharedBme;
        }
        sharedBme = new Adafruit_BME280();
        sharedBmeAddress = address;
        sharedBmeRefCount = 0;

        // Initialize I2C with sensor pins
        Wire.begin(I2C_SDA, I2C_SCL);

        if (!sharedBme->begin(address, &Wire)) {
            LOG_ERROR("BME280 not found at address 0x%02X", address);
            delete sharedBme;
            sharedBme = nullptr;
            return nullptr;
        }

        // Configure for weather monitoring
        sharedBme->setSampling(
            Adafruit_BME280::MODE_NORMAL,
            Adafruit_BME280::SAMPLING_X1,   // Temperature
            Adafruit_BME280::SAMPLING_X1,   // Pressure
            Adafruit_BME280::SAMPLING_X1,   // Humidity
            Adafruit_BME280::FILTER_OFF,
            Adafruit_BME280::STANDBY_MS_1000
        );

        LOG_INFO("BME280 initialized at 0x%02X", address);
    }

    sharedBmeRefCount++;
    return sharedBme;
}
#endif

// ============================================================
// BME280 TEMPERATURE SENSOR
// ============================================================

BME280TemperatureSensor::BME280TemperatureSensor(uint8_t address)
    : address_(address) {}

bool BME280TemperatureSensor::begin() {
#ifdef PLATFORM_ESP32
    bme_ = getSharedBme(address_);
    initialized_ = (bme_ != nullptr);
#else
    initialized_ = true;
    LOG_INFO("[SIM] BME280 Temperature sensor initialized");
#endif
    return initialized_;
}

float BME280TemperatureSensor::read() {
    if (!initialized_) return 0.0f;

#ifdef PLATFORM_ESP32
    float temp = bme_->readTemperature();
#else
    // Simulate temperature (18-25°C with daily variation)
    float hour = (hal::millis() / 3600000.0f);
    float temp = 21.5f + 3.0f * sinf(hour * 0.26f) + ((rand() % 100) - 50) / 100.0f;
#endif

    return temp + offset_;
}

// ============================================================
// BME280 HUMIDITY SENSOR
// ============================================================

BME280HumiditySensor::BME280HumiditySensor(uint8_t address)
    : address_(address) {}

bool BME280HumiditySensor::begin() {
#ifdef PLATFORM_ESP32
    bme_ = getSharedBme(address_);
    initialized_ = (bme_ != nullptr);
#else
    initialized_ = true;
    LOG_INFO("[SIM] BME280 Humidity sensor initialized");
#endif
    return initialized_;
}

float BME280HumiditySensor::read() {
    if (!initialized_) return 0.0f;

#ifdef PLATFORM_ESP32
    float hum = bme_->readHumidity();
#else
    // Simulate humidity (40-80%)
    float hum = 60.0f + 15.0f * sinf(hal::millis() / 7200000.0f) + ((rand() % 100) - 50) / 10.0f;
#endif

    return hum + offset_;
}

// ============================================================
// BME280 PRESSURE SENSOR
// ============================================================

BME280PressureSensor::BME280PressureSensor(uint8_t address)
    : address_(address) {}

bool BME280PressureSensor::begin() {
#ifdef PLATFORM_ESP32
    bme_ = getSharedBme(address_);
    initialized_ = (bme_ != nullptr);
#else
    initialized_ = true;
    LOG_INFO("[SIM] BME280 Pressure sensor initialized");
#endif
    return initialized_;
}

float BME280PressureSensor::read() {
    if (!initialized_) return 0.0f;

#ifdef PLATFORM_ESP32
    float press = bme_->readPressure() / 100.0f;  // Pa to hPa
#else
    // Simulate pressure (990-1020 hPa)
    float press = 1013.25f + 10.0f * sinf(hal::millis() / 14400000.0f) + ((rand() % 100) - 50) / 10.0f;
#endif

    return press + offset_;
}

// ============================================================
// BME280 COMBINED SENSOR
// ============================================================

BME280Sensor::BME280Sensor(uint8_t address)
    : address_(address) {}

BME280Sensor::~BME280Sensor() {
#ifdef PLATFORM_ESP32
    if (bme_ != nullptr && --sharedBmeRefCount <= 0) {
        delete sharedBme;
        sharedBme = nullptr;
        sharedBmeRefCount = 0;
    }
#endif
}

bool BME280Sensor::begin() {
#ifdef PLATFORM_ESP32
    bme_ = getSharedBme(address_);
    initialized_ = (bme_ != nullptr);
#else
    initialized_ = true;
    LOG_INFO("[SIM] BME280 combined sensor initialized");
#endif
    return initialized_;
}

float BME280Sensor::readTemperature() {
    if (!initialized_) return 0.0f;

#ifdef PLATFORM_ESP32
    return bme_->readTemperature() + tempOffset_;
#else
    float hour = (hal::millis() / 3600000.0f);
    return 21.5f + 3.0f * sinf(hour * 0.26f) + tempOffset_;
#endif
}

float BME280Sensor::readHumidity() {
    if (!initialized_) return 0.0f;

#ifdef PLATFORM_ESP32
    return bme_->readHumidity() + humOffset_;
#else
    return 60.0f + 15.0f * sinf(hal::millis() / 7200000.0f) + humOffset_;
#endif
}

float BME280Sensor::readPressure() {
    if (!initialized_) return 0.0f;

#ifdef PLATFORM_ESP32
    return (bme_->readPressure() / 100.0f) + pressOffset_;
#else
    return 1013.25f + 10.0f * sinf(hal::millis() / 14400000.0f) + pressOffset_;
#endif
}

void BME280Sensor::setCalibration(float tempOffset, float humOffset, float pressOffset) {
    tempOffset_ = tempOffset;
    humOffset_ = humOffset;
    pressOffset_ = pressOffset;
}

void BME280Sensor::takeMeasurement() {
#ifdef PLATFORM_ESP32
    if (bme_ != nullptr) {
        bme_->takeForcedMeasurement();
    }
#endif
}
