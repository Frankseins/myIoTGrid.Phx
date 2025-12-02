/**
 * Unit Tests for myIoTGrid Sensor
 *
 * Run with: pio test -e native_test
 */

#include <unity.h>
#include "simulated_sensor.h"
#include "sensor_factory.h"
#include "json_serializer.h"
#include "data_types.h"
#include <cmath>

// ============================================
// SimulatedSensor Tests
// ============================================

void test_temperature_sensor_creation() {
    sensor::SimulatedSensor sensor("temperature");
    TEST_ASSERT_EQUAL_STRING("temperature", sensor.getType().c_str());
    TEST_ASSERT_EQUAL_STRING("°C", sensor.getUnit().c_str());
    TEST_ASSERT_FLOAT_WITHIN(0.01f, -40.0f, sensor.getMinValue());
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 80.0f, sensor.getMaxValue());
}

void test_humidity_sensor_creation() {
    sensor::SimulatedSensor sensor("humidity");
    TEST_ASSERT_EQUAL_STRING("humidity", sensor.getType().c_str());
    TEST_ASSERT_EQUAL_STRING("%", sensor.getUnit().c_str());
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 0.0f, sensor.getMinValue());
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 100.0f, sensor.getMaxValue());
}

void test_pressure_sensor_creation() {
    sensor::SimulatedSensor sensor("pressure");
    TEST_ASSERT_EQUAL_STRING("pressure", sensor.getType().c_str());
    TEST_ASSERT_EQUAL_STRING("hPa", sensor.getUnit().c_str());
}

void test_unknown_sensor_type_throws() {
    bool threw = false;
    try {
        sensor::SimulatedSensor sensor("unknown_type");
    } catch (const std::invalid_argument& e) {
        threw = true;
    }
    TEST_ASSERT_TRUE(threw);
}

void test_sensor_begin() {
    sensor::SimulatedSensor sensor("temperature");
    TEST_ASSERT_FALSE(sensor.isReady());
    TEST_ASSERT_TRUE(sensor.begin());
    TEST_ASSERT_TRUE(sensor.isReady());
}

void test_temperature_in_range() {
    sensor::SimulatedSensor sensor("temperature");
    sensor.begin();

    for (int i = 0; i < 100; i++) {
        float value = sensor.read();
        TEST_ASSERT_FALSE(std::isnan(value));
        TEST_ASSERT_GREATER_OR_EQUAL(-40.0f, value);
        TEST_ASSERT_LESS_OR_EQUAL(80.0f, value);
    }
}

void test_humidity_in_range() {
    sensor::SimulatedSensor sensor("humidity");
    sensor.begin();

    for (int i = 0; i < 100; i++) {
        float value = sensor.read();
        TEST_ASSERT_FALSE(std::isnan(value));
        TEST_ASSERT_GREATER_OR_EQUAL(0.0f, value);
        TEST_ASSERT_LESS_OR_EQUAL(100.0f, value);
    }
}

void test_read_without_begin_returns_nan() {
    sensor::SimulatedSensor sensor("temperature");
    // Don't call begin()
    float value = sensor.read();
    TEST_ASSERT_TRUE(std::isnan(value));
}

// ============================================
// SensorFactory Tests
// ============================================

void test_factory_creates_temperature_sensor() {
    auto sensor = sensor::SensorFactory::create("temperature");
    TEST_ASSERT_NOT_NULL(sensor.get());
    TEST_ASSERT_EQUAL_STRING("temperature", sensor->getType().c_str());
}

void test_factory_creates_humidity_sensor() {
    auto sensor = sensor::SensorFactory::create("humidity");
    TEST_ASSERT_NOT_NULL(sensor.get());
    TEST_ASSERT_EQUAL_STRING("humidity", sensor->getType().c_str());
}

void test_factory_returns_null_for_unknown_type() {
    auto sensor = sensor::SensorFactory::create("unknown_sensor");
    TEST_ASSERT_NULL(sensor.get());
}

void test_factory_is_type_supported() {
    TEST_ASSERT_TRUE(sensor::SensorFactory::isTypeSupported("temperature"));
    TEST_ASSERT_TRUE(sensor::SensorFactory::isTypeSupported("humidity"));
    TEST_ASSERT_TRUE(sensor::SensorFactory::isTypeSupported("pressure"));
    TEST_ASSERT_FALSE(sensor::SensorFactory::isTypeSupported("unknown"));
}

void test_factory_get_supported_types() {
    auto types = sensor::SensorFactory::getSupportedTypes();
    TEST_ASSERT_GREATER_THAN(0, types.size());

    // Check some expected types
    bool hasTemperature = false;
    bool hasHumidity = false;
    for (const auto& type : types) {
        if (type == "temperature") hasTemperature = true;
        if (type == "humidity") hasHumidity = true;
    }
    TEST_ASSERT_TRUE(hasTemperature);
    TEST_ASSERT_TRUE(hasHumidity);
}

// ============================================
// JSON Serialization Tests
// ============================================

void test_serialize_reading() {
    data::Reading reading("device-01", "temperature", 21.5f, "°C", 1234567890);

    std::string json = data::JsonSerializer::serializeReading(reading);

    TEST_ASSERT_TRUE(json.find("\"deviceId\":\"device-01\"") != std::string::npos);
    TEST_ASSERT_TRUE(json.find("\"type\":\"temperature\"") != std::string::npos);
    TEST_ASSERT_TRUE(json.find("21.5") != std::string::npos);
    TEST_ASSERT_TRUE(json.find("\"unit\":\"°C\"") != std::string::npos);
}

void test_serialize_node_info() {
    data::NodeInfo info;
    info.serialNumber = "SIM-12345678-0001";
    info.capabilities = {"temperature", "humidity"};
    info.firmwareVersion = "1.0.0";
    info.hardwareType = "SIM";

    std::string json = data::JsonSerializer::serializeNodeInfo(info);

    TEST_ASSERT_TRUE(json.find("\"serialNumber\":\"SIM-12345678-0001\"") != std::string::npos);
    TEST_ASSERT_TRUE(json.find("\"firmwareVersion\":\"1.0.0\"") != std::string::npos);
    TEST_ASSERT_TRUE(json.find("\"hardwareType\":\"SIM\"") != std::string::npos);
    TEST_ASSERT_TRUE(json.find("\"capabilities\"") != std::string::npos);
}

void test_deserialize_node_config() {
    std::string json = R"({
        "deviceId": "wetterstation-sim-01",
        "name": "Test Sensor",
        "location": "Office",
        "intervalSeconds": 30,
        "sensors": [
            {"type": "temperature", "enabled": true, "pin": -1},
            {"type": "humidity", "enabled": false, "pin": 5}
        ],
        "connection": {
            "mode": "http",
            "endpoint": "http://localhost:5000"
        }
    })";

    data::NodeConfig config;
    bool success = data::JsonSerializer::deserializeNodeConfig(json, config);

    TEST_ASSERT_TRUE(success);
    TEST_ASSERT_EQUAL_STRING("wetterstation-sim-01", config.deviceId.c_str());
    TEST_ASSERT_EQUAL_STRING("Test Sensor", config.name.c_str());
    TEST_ASSERT_EQUAL_STRING("Office", config.location.c_str());
    TEST_ASSERT_EQUAL_UINT32(30, config.intervalSeconds);
    TEST_ASSERT_EQUAL(2, config.sensors.size());
    TEST_ASSERT_EQUAL_STRING("temperature", config.sensors[0].type.c_str());
    TEST_ASSERT_TRUE(config.sensors[0].enabled);
    TEST_ASSERT_EQUAL(-1, config.sensors[0].pin);
    TEST_ASSERT_EQUAL_STRING("humidity", config.sensors[1].type.c_str());
    TEST_ASSERT_FALSE(config.sensors[1].enabled);
    TEST_ASSERT_EQUAL(5, config.sensors[1].pin);
    TEST_ASSERT_EQUAL_STRING("http", config.connection.mode.c_str());
    TEST_ASSERT_EQUAL_STRING("http://localhost:5000", config.connection.endpoint.c_str());
}

void test_deserialize_invalid_json() {
    std::string json = "{ invalid json }";
    data::NodeConfig config;
    bool success = data::JsonSerializer::deserializeNodeConfig(json, config);
    TEST_ASSERT_FALSE(success);
}

void test_deserialize_missing_device_id() {
    std::string json = R"({
        "name": "Test Sensor",
        "intervalSeconds": 30
    })";

    data::NodeConfig config;
    bool success = data::JsonSerializer::deserializeNodeConfig(json, config);
    TEST_ASSERT_FALSE(success);
}

// ============================================
// Data Types Tests
// ============================================

void test_node_config_is_valid() {
    data::NodeConfig config;
    TEST_ASSERT_FALSE(config.isValid()); // Empty deviceId

    config.deviceId = "test-device";
    TEST_ASSERT_TRUE(config.isValid());

    config.intervalSeconds = 0;
    TEST_ASSERT_FALSE(config.isValid());
}

void test_node_config_get_enabled_sensor_types() {
    data::NodeConfig config;
    config.sensors.push_back(data::SensorConfig("temperature", true, -1));
    config.sensors.push_back(data::SensorConfig("humidity", false, -1));
    config.sensors.push_back(data::SensorConfig("pressure", true, -1));

    auto enabled = config.getEnabledSensorTypes();

    TEST_ASSERT_EQUAL(2, enabled.size());
    TEST_ASSERT_EQUAL_STRING("temperature", enabled[0].c_str());
    TEST_ASSERT_EQUAL_STRING("pressure", enabled[1].c_str());
}

void test_reading_constructor() {
    data::Reading reading("dev-01", "temp", 25.5f, "C", 123456);

    TEST_ASSERT_EQUAL_STRING("dev-01", reading.deviceId.c_str());
    TEST_ASSERT_EQUAL_STRING("temp", reading.type.c_str());
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 25.5f, reading.value);
    TEST_ASSERT_EQUAL_STRING("C", reading.unit.c_str());
    TEST_ASSERT_EQUAL_UINT64(123456, reading.timestamp);
}

// ============================================
// Test Runner
// ============================================

void setUp(void) {
    // Called before each test
}

void tearDown(void) {
    // Called after each test
}

int main(int argc, char **argv) {
    UNITY_BEGIN();

    // SimulatedSensor Tests
    RUN_TEST(test_temperature_sensor_creation);
    RUN_TEST(test_humidity_sensor_creation);
    RUN_TEST(test_pressure_sensor_creation);
    RUN_TEST(test_unknown_sensor_type_throws);
    RUN_TEST(test_sensor_begin);
    RUN_TEST(test_temperature_in_range);
    RUN_TEST(test_humidity_in_range);
    RUN_TEST(test_read_without_begin_returns_nan);

    // SensorFactory Tests
    RUN_TEST(test_factory_creates_temperature_sensor);
    RUN_TEST(test_factory_creates_humidity_sensor);
    RUN_TEST(test_factory_returns_null_for_unknown_type);
    RUN_TEST(test_factory_is_type_supported);
    RUN_TEST(test_factory_get_supported_types);

    // JSON Serialization Tests
    RUN_TEST(test_serialize_reading);
    RUN_TEST(test_serialize_node_info);
    RUN_TEST(test_deserialize_node_config);
    RUN_TEST(test_deserialize_invalid_json);
    RUN_TEST(test_deserialize_missing_device_id);

    // Data Types Tests
    RUN_TEST(test_node_config_is_valid);
    RUN_TEST(test_node_config_get_enabled_sensor_types);
    RUN_TEST(test_reading_constructor);

    return UNITY_END();
}
