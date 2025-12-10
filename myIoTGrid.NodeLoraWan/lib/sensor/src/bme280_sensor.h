/**
 * @file bme280_sensor.h
 * @brief BME280 Environmental Sensor
 *
 * Provides temperature, humidity, and pressure readings
 * from Bosch BME280 sensor.
 *
 * @version 1.0.0
 * @date 2025-12-10
 *
 * Sprint: LoRa-02 - Grid.Sensor LoRaWAN Firmware
 */

#pragma once

#include "sensor_interface.h"
#include "config.h"

#ifdef PLATFORM_ESP32
#include <Adafruit_BME280.h>
#endif

/**
 * @brief BME280 Temperature Sensor
 */
class BME280TemperatureSensor : public ISensor {
public:
    BME280TemperatureSensor(uint8_t address = BME280_ADDRESS_PRIMARY);
    ~BME280TemperatureSensor() override = default;

    std::string getType() const override { return "temperature"; }
    std::string getUnit() const override { return "°C"; }
    float getMinValue() const override { return -40.0f; }
    float getMaxValue() const override { return 85.0f; }

    bool begin() override;
    bool isReady() const override { return initialized_; }
    float read() override;

    /**
     * @brief Set temperature offset for calibration
     * @param offset Offset in °C
     */
    void setOffset(float offset) { offset_ = offset; }

private:
    uint8_t address_;
    bool initialized_ = false;
    float offset_ = 0.0f;

#ifdef PLATFORM_ESP32
    Adafruit_BME280* bme_ = nullptr;
#endif
};

/**
 * @brief BME280 Humidity Sensor
 */
class BME280HumiditySensor : public ISensor {
public:
    BME280HumiditySensor(uint8_t address = BME280_ADDRESS_PRIMARY);
    ~BME280HumiditySensor() override = default;

    std::string getType() const override { return "humidity"; }
    std::string getUnit() const override { return "%"; }
    float getMinValue() const override { return 0.0f; }
    float getMaxValue() const override { return 100.0f; }

    bool begin() override;
    bool isReady() const override { return initialized_; }
    float read() override;

    void setOffset(float offset) { offset_ = offset; }

private:
    uint8_t address_;
    bool initialized_ = false;
    float offset_ = 0.0f;

#ifdef PLATFORM_ESP32
    Adafruit_BME280* bme_ = nullptr;
#endif
};

/**
 * @brief BME280 Pressure Sensor
 */
class BME280PressureSensor : public ISensor {
public:
    BME280PressureSensor(uint8_t address = BME280_ADDRESS_PRIMARY);
    ~BME280PressureSensor() override = default;

    std::string getType() const override { return "pressure"; }
    std::string getUnit() const override { return "hPa"; }
    float getMinValue() const override { return 300.0f; }
    float getMaxValue() const override { return 1100.0f; }

    bool begin() override;
    bool isReady() const override { return initialized_; }
    float read() override;

    void setOffset(float offset) { offset_ = offset; }

private:
    uint8_t address_;
    bool initialized_ = false;
    float offset_ = 0.0f;

#ifdef PLATFORM_ESP32
    Adafruit_BME280* bme_ = nullptr;
#endif
};

/**
 * @brief BME280 Combined Sensor Controller
 *
 * Manages a single BME280 sensor and provides all three readings.
 */
class BME280Sensor {
public:
    BME280Sensor(uint8_t address = BME280_ADDRESS_PRIMARY);
    ~BME280Sensor();

    /**
     * @brief Initialize BME280 sensor
     * @return true if successful
     */
    bool begin();

    /**
     * @brief Check if sensor is ready
     * @return true if initialized
     */
    bool isReady() const { return initialized_; }

    /**
     * @brief Read temperature
     * @return Temperature in °C
     */
    float readTemperature();

    /**
     * @brief Read humidity
     * @return Humidity in %
     */
    float readHumidity();

    /**
     * @brief Read pressure
     * @return Pressure in hPa
     */
    float readPressure();

    /**
     * @brief Set calibration offsets
     * @param tempOffset Temperature offset in °C
     * @param humOffset Humidity offset in %
     * @param pressOffset Pressure offset in hPa
     */
    void setCalibration(float tempOffset, float humOffset, float pressOffset);

    /**
     * @brief Force a new reading
     *
     * Triggers a measurement cycle (for forced mode).
     */
    void takeMeasurement();

private:
    uint8_t address_;
    bool initialized_ = false;
    float tempOffset_ = 0.0f;
    float humOffset_ = 0.0f;
    float pressOffset_ = 0.0f;

#ifdef PLATFORM_ESP32
    Adafruit_BME280* bme_ = nullptr;
#endif
};
