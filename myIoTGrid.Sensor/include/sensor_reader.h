/**
 * myIoTGrid.Sensor - Hardware Sensor Reader
 *
 * Reads real sensor values from various sensors:
 * - BME280, BME680, SHT31 (Temperature/Humidity/Pressure)
 * - DHT22 (Temperature/Humidity)
 * - DS18B20 (Temperature)
 * - BH1750/GY-302 (Light)
 * - TSL2561 (Light)
 * - SCD30, SCD40 (CO2)
 * - CCS811 (CO2/VOC)
 * - SGP30 (CO2/VOC)
 * - VL53L0X (Distance)
 * - JSN-SR04T (Ultrasonic Distance/Water Level)
 * - ADS1115 (Analog/ADC)
 * - NEO-6M (GPS: Latitude/Longitude/Altitude/Speed)
 *
 * Uses Hub-provided configuration (i2cAddress, sdaPin, sclPin, etc.)
 */

#ifndef SENSOR_READER_H
#define SENSOR_READER_H

#include <Arduino.h>
#include <vector>
#include "api_client.h"

#ifdef PLATFORM_ESP32
#include <Wire.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_BME280.h>
#include <Adafruit_BME680.h>
#include <ClosedCube_SHT31D.h>
#include <OneWire.h>
#include <DallasTemperature.h>
#include <BH1750.h>
#include <Adafruit_TSL2561_U.h>
#include <SparkFun_SCD30_Arduino_Library.h>
#include <SensirionI2CScd4x.h>
#include <Adafruit_CCS811.h>
#include <Adafruit_SGP30.h>
#include <VL53L0X.h>
#include <Adafruit_ADS1X15.h>
#include <DHT.h>
#include <TinyGPSPlus.h>
#include <HardwareSerial.h>
#include <driver/uart.h>  // ESP-IDF UART driver for SR04M-2
#endif

/**
 * Sensor reading result
 */
struct SensorReading {
    bool success;
    double value;
    String error;

    SensorReading() : success(false), value(0.0) {}
    SensorReading(double val) : success(true), value(val) {}
    SensorReading(const String& err) : success(false), value(0.0), error(err) {}
};

/**
 * Hardware sensor reader class
 * Manages initialization and reading of physical sensors based on Hub configuration
 */
class SensorReader {
public:
    SensorReader();
    ~SensorReader();

    /**
     * Initialize the sensor reader
     * Must be called before reading sensors
     */
    void init();

    /**
     * Initialize a specific sensor based on Hub configuration
     * @param config Sensor assignment configuration from Hub
     * @return true if sensor was successfully initialized
     */
    bool initializeSensor(const SensorAssignmentConfig& config);

    /**
     * Read a sensor value based on measurement type and sensor configuration
     * @param measurementType The type of measurement (temperature, humidity, pressure, etc.)
     * @param config Sensor assignment configuration from Hub
     * @return SensorReading with value or error
     */
    SensorReading readValue(const String& measurementType, const SensorAssignmentConfig& config);

    /**
     * Read temperature in Celsius
     */
    SensorReading readTemperature(const SensorAssignmentConfig& config);

    /**
     * Read humidity in percentage
     */
    SensorReading readHumidity(const SensorAssignmentConfig& config);

    /**
     * Read pressure in hPa
     */
    SensorReading readPressure(const SensorAssignmentConfig& config);

    /**
     * Read gas resistance (BME680 only) in Ohms
     */
    SensorReading readGasResistance(const SensorAssignmentConfig& config);

    /**
     * Read light intensity in Lux (BH1750, TSL2561)
     */
    SensorReading readLight(const SensorAssignmentConfig& config);

    /**
     * Read CO2 in ppm (SCD30, SCD40, CCS811, SGP30)
     */
    SensorReading readCO2(const SensorAssignmentConfig& config);

    /**
     * Read TVOC in ppb (CCS811, SGP30)
     */
    SensorReading readTVOC(const SensorAssignmentConfig& config);

    /**
     * Read distance in mm (VL53L0X)
     */
    SensorReading readDistance(const SensorAssignmentConfig& config);

    /**
     * Read water level in cm (JSN-SR04T ultrasonic)
     */
    SensorReading readWaterLevel(const SensorAssignmentConfig& config);

    /**
     * Read analog value (ADS1115)
     */
    SensorReading readAnalog(const SensorAssignmentConfig& config, int channel = 0);

    /**
     * Read GPS latitude in degrees (NEO-6M)
     */
    SensorReading readLatitude(const SensorAssignmentConfig& config);

    /**
     * Read GPS longitude in degrees (NEO-6M)
     */
    SensorReading readLongitude(const SensorAssignmentConfig& config);

    /**
     * Read GPS altitude in meters (NEO-6M)
     */
    SensorReading readAltitude(const SensorAssignmentConfig& config);

    /**
     * Read GPS speed in km/h (NEO-6M)
     */
    SensorReading readSpeed(const SensorAssignmentConfig& config);

    /**
     * Check if a sensor is available/detected
     */
    bool isSensorAvailable(const SensorAssignmentConfig& config);

    /**
     * Get sensor type from code
     */
    String getSensorType(const String& sensorCode);

private:
#ifdef PLATFORM_ESP32
    // BME280 sensor instances (indexed by I2C address for multi-sensor support)
    Adafruit_BME280* _bme280_0x76;
    Adafruit_BME280* _bme280_0x77;
    bool _bme280_0x76_ready;
    bool _bme280_0x77_ready;

    // BME680 sensor instances
    Adafruit_BME680* _bme680_0x76;
    Adafruit_BME680* _bme680_0x77;
    bool _bme680_0x76_ready;
    bool _bme680_0x77_ready;

    // SHT31 sensor instances
    ClosedCube_SHT31D* _sht31_0x44;
    ClosedCube_SHT31D* _sht31_0x45;
    bool _sht31_0x44_ready;
    bool _sht31_0x45_ready;

    // DS18B20 (OneWire) support
    OneWire* _oneWire;
    DallasTemperature* _ds18b20;
    bool _ds18b20_ready;
    int _ds18b20_pin;

    // BH1750 (GY-302) Light sensor
    BH1750* _bh1750_0x23;
    BH1750* _bh1750_0x5C;
    bool _bh1750_0x23_ready;
    bool _bh1750_0x5C_ready;

    // TSL2561 Light sensor
    Adafruit_TSL2561_Unified* _tsl2561_0x29;
    Adafruit_TSL2561_Unified* _tsl2561_0x39;
    Adafruit_TSL2561_Unified* _tsl2561_0x49;
    bool _tsl2561_0x29_ready;
    bool _tsl2561_0x39_ready;
    bool _tsl2561_0x49_ready;

    // SCD30 CO2 sensor
    SCD30* _scd30;
    bool _scd30_ready;

    // SCD40/SCD41 CO2 sensor
    SensirionI2CScd4x* _scd4x;
    bool _scd4x_ready;

    // CCS811 CO2/VOC sensor
    Adafruit_CCS811* _ccs811_0x5A;
    Adafruit_CCS811* _ccs811_0x5B;
    bool _ccs811_0x5A_ready;
    bool _ccs811_0x5B_ready;

    // SGP30 CO2/VOC sensor
    Adafruit_SGP30* _sgp30;
    bool _sgp30_ready;

    // VL53L0X Distance sensor
    VL53L0X* _vl53l0x;
    bool _vl53l0x_ready;

    // ADS1115 ADC
    Adafruit_ADS1115* _ads1115_0x48;
    Adafruit_ADS1115* _ads1115_0x49;
    bool _ads1115_0x48_ready;
    bool _ads1115_0x49_ready;

    // DHT22 sensor
    DHT* _dht22;
    bool _dht22_ready;
    int _dht22_pin;

    // JSN-SR04T Ultrasonic sensor (no library needed, direct GPIO)
    int _ultrasonic_trigger_pin;
    int _ultrasonic_echo_pin;
    bool _ultrasonic_ready;

    // NEO-6M GPS module
    TinyGPSPlus* _gps;
    HardwareSerial* _gpsSerial;
    bool _gps_ready;
    int _gps_rx_pin;
    int _gps_tx_pin;

    // SR04M-2 Ultrasonic UART mode
    HardwareSerial* _sr04m2Serial;
    bool _sr04m2_ready;
    int _sr04m2_rx_pin;
    int _sr04m2_tx_pin;

    // Current Wire/I2C instance pins
    int _currentSdaPin;
    int _currentSclPin;

    /**
     * Initialize I2C bus with specific pins
     */
    void initI2C(int sdaPin = -1, int sclPin = -1);

    /**
     * Parse I2C address from string (e.g., "0x76" -> 0x76)
     */
    uint8_t parseI2CAddress(const String& addressStr);

    // Sensor initialization functions
    bool initBME280(uint8_t address);
    bool initBME680(uint8_t address);
    bool initSHT31(uint8_t address);
    bool initDS18B20(int pin);
    bool initDHT22(int pin);
    bool initBH1750(uint8_t address);
    bool initTSL2561(uint8_t address);
    bool initSCD30();
    bool initSCD4x();
    bool initCCS811(uint8_t address);
    bool initSGP30();
    bool initVL53L0X();
    bool initADS1115(uint8_t address);
    bool initUltrasonic(int triggerPin, int echoPin);
    bool initGPS(int rxPin, int txPin);
    bool initSR04M2(int rxPin, int txPin);

    // Sensor getter functions
    Adafruit_BME280* getBME280(uint8_t address);
    Adafruit_BME680* getBME680(uint8_t address);
    ClosedCube_SHT31D* getSHT31(uint8_t address);
    BH1750* getBH1750(uint8_t address);
    Adafruit_TSL2561_Unified* getTSL2561(uint8_t address);
    Adafruit_CCS811* getCCS811(uint8_t address);
    Adafruit_ADS1115* getADS1115(uint8_t address);
#endif

    bool _initialized;
};

#endif // SENSOR_READER_H
