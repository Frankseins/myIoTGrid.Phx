#ifndef SENSOR_INTERFACE_H
#define SENSOR_INTERFACE_H

#include <string>
#include <memory>

namespace sensor {

/**
 * Interface for all sensor types
 * Provides a unified API for reading sensor values regardless of hardware
 */
class ISensor {
public:
    virtual ~ISensor() = default;

    /**
     * Get the sensor type code (e.g., "temperature", "humidity")
     * Must match SensorType.Code in the Hub backend
     * @return Sensor type code string
     */
    virtual std::string getType() const = 0;

    /**
     * Get the unit of measurement (e.g., "°C", "%", "hPa")
     * @return Unit string
     */
    virtual std::string getUnit() const = 0;

    /**
     * Get the minimum valid value for this sensor
     * Used for validation and clamping
     * @return Minimum value
     */
    virtual float getMinValue() const = 0;

    /**
     * Get the maximum valid value for this sensor
     * Used for validation and clamping
     * @return Maximum value
     */
    virtual float getMaxValue() const = 0;

    /**
     * Initialize the sensor hardware
     * Must be called before read()
     * @return true if initialization successful
     */
    virtual bool begin() = 0;

    /**
     * Read current sensor value
     * Returns the current measurement in the sensor's native unit
     * @return Sensor value, or NAN if read failed
     */
    virtual float read() = 0;

    /**
     * Check if the sensor is ready to read
     * @return true if sensor is initialized and operational
     */
    virtual bool isReady() const = 0;

    /**
     * Get a human-readable name for this sensor instance
     * @return Sensor name
     */
    virtual std::string getName() const = 0;
};

/**
 * Sensor type definitions with their properties
 */
struct SensorTypeInfo {
    const char* type;
    const char* name;
    const char* unit;
    float minValue;
    float maxValue;
    float baseValue;      // For simulation: center value
    float amplitude;      // For simulation: variation range
    float noise;          // For simulation: random noise range
};

// Supported sensor types
namespace SensorTypes {
    constexpr SensorTypeInfo TEMPERATURE = {
        "temperature", "Temperatur", "°C",
        -40.0f, 80.0f,    // Valid range
        18.0f, 8.0f, 0.5f // Simulation: base=18, amplitude=±8, noise=±0.5
    };

    constexpr SensorTypeInfo HUMIDITY = {
        "humidity", "Luftfeuchtigkeit", "%",
        0.0f, 100.0f,
        55.0f, 15.0f, 2.0f
    };

    constexpr SensorTypeInfo PRESSURE = {
        "pressure", "Luftdruck", "hPa",
        870.0f, 1085.0f,
        1013.0f, 10.0f, 1.0f
    };

    constexpr SensorTypeInfo WATER_LEVEL = {
        "water_level", "Wasserstand", "cm",
        0.0f, 500.0f,
        50.0f, 20.0f, 2.0f
    };

    constexpr SensorTypeInfo CO2 = {
        "co2", "CO2", "ppm",
        400.0f, 5000.0f,
        600.0f, 200.0f, 20.0f
    };

    constexpr SensorTypeInfo PM25 = {
        "pm25", "Feinstaub PM2.5", "µg/m³",
        0.0f, 500.0f,
        15.0f, 10.0f, 2.0f
    };

    constexpr SensorTypeInfo PM10 = {
        "pm10", "Feinstaub PM10", "µg/m³",
        0.0f, 600.0f,
        25.0f, 15.0f, 3.0f
    };

    constexpr SensorTypeInfo SOIL_MOISTURE = {
        "soil_moisture", "Bodenfeuchtigkeit", "%",
        0.0f, 100.0f,
        45.0f, 20.0f, 3.0f
    };

    constexpr SensorTypeInfo LIGHT = {
        "light", "Helligkeit", "lux",
        0.0f, 100000.0f,
        500.0f, 400.0f, 50.0f
    };

    constexpr SensorTypeInfo UV = {
        "uv", "UV-Index", "index",
        0.0f, 11.0f,
        3.0f, 2.0f, 0.3f
    };

    constexpr SensorTypeInfo WIND_SPEED = {
        "wind_speed", "Windgeschwindigkeit", "m/s",
        0.0f, 60.0f,
        5.0f, 4.0f, 1.0f
    };

    constexpr SensorTypeInfo RAINFALL = {
        "rainfall", "Niederschlag", "mm",
        0.0f, 500.0f,
        0.0f, 2.0f, 0.5f
    };

    constexpr SensorTypeInfo BATTERY = {
        "battery", "Batterie", "%",
        0.0f, 100.0f,
        85.0f, 10.0f, 1.0f
    };

    constexpr SensorTypeInfo RSSI = {
        "rssi", "Signalstärke", "dBm",
        -120.0f, 0.0f,
        -60.0f, 15.0f, 3.0f
    };

    /**
     * Get sensor type info by type code
     * @param type Type code (e.g., "temperature")
     * @return Pointer to SensorTypeInfo or nullptr if not found
     */
    const SensorTypeInfo* getInfo(const std::string& type);
}

} // namespace sensor

#endif // SENSOR_INTERFACE_H
