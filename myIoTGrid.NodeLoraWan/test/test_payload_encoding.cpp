/**
 * @file test_payload_encoding.cpp
 * @brief Unit Tests for LoRaWAN Payload Encoding
 *
 * Tests the binary payload encoding for sensor readings.
 *
 * @version 1.0.0
 * @date 2025-12-10
 *
 * Sprint: LoRa-02 - Grid.Sensor LoRaWAN Firmware
 */

#include <unity.h>
#include <vector>
#include <cstdint>
#include <cmath>
#include <string>

// Include the payload encoding logic
// Note: In a real test, you would include the actual header
// For this test file, we'll redefine the necessary structures and functions

// ============================================================
// SENSOR TYPE IDS (Copied from config.h for testing)
// ============================================================

namespace SensorTypeId {
    constexpr uint8_t TEMPERATURE = 0x01;
    constexpr uint8_t HUMIDITY = 0x02;
    constexpr uint8_t PRESSURE = 0x03;
    constexpr uint8_t WATER_LEVEL = 0x04;
    constexpr uint8_t BATTERY = 0x05;
    constexpr uint8_t CO2 = 0x06;
    constexpr uint8_t PM25 = 0x07;
    constexpr uint8_t PM10 = 0x08;
    constexpr uint8_t UNKNOWN = 0xFF;
}

// ============================================================
// READING STRUCTURE
// ============================================================

struct Reading {
    std::string deviceId;
    std::string type;
    float value;
    std::string unit;
    uint32_t timestamp;
};

// ============================================================
// PAYLOAD ENCODING FUNCTIONS (Under Test)
// ============================================================

uint8_t getSensorTypeId(const std::string& type) {
    if (type == "temperature")   return SensorTypeId::TEMPERATURE;
    if (type == "humidity")      return SensorTypeId::HUMIDITY;
    if (type == "pressure")      return SensorTypeId::PRESSURE;
    if (type == "water_level")   return SensorTypeId::WATER_LEVEL;
    if (type == "battery")       return SensorTypeId::BATTERY;
    if (type == "co2")           return SensorTypeId::CO2;
    if (type == "pm25")          return SensorTypeId::PM25;
    if (type == "pm10")          return SensorTypeId::PM10;
    return SensorTypeId::UNKNOWN;
}

std::vector<uint8_t> encodeReading(const Reading& reading) {
    std::vector<uint8_t> payload;

    uint8_t typeId = getSensorTypeId(reading.type);

    // Convert value to int16 with appropriate precision
    int16_t encodedValue;
    if (reading.type == "pressure") {
        encodedValue = static_cast<int16_t>(reading.value * 10);  // 1 decimal
    } else {
        encodedValue = static_cast<int16_t>(reading.value * 100); // 2 decimals
    }

    payload.push_back(typeId);
    payload.push_back((encodedValue >> 8) & 0xFF);  // High byte (MSB)
    payload.push_back(encodedValue & 0xFF);          // Low byte (LSB)

    return payload;
}

std::vector<uint8_t> encodeBatch(const std::vector<Reading>& readings) {
    std::vector<uint8_t> payload;

    for (const auto& reading : readings) {
        auto encoded = encodeReading(reading);
        payload.insert(payload.end(), encoded.begin(), encoded.end());

        if (payload.size() >= 48) break;  // Safety limit
    }

    return payload;
}

// Decoder for verification
float decodeValue(uint8_t typeId, int16_t encoded) {
    if (typeId == SensorTypeId::PRESSURE) {
        return encoded / 10.0f;   // 1 decimal
    } else {
        return encoded / 100.0f;  // 2 decimals
    }
}

// ============================================================
// TEST CASES
// ============================================================

void test_sensor_type_ids() {
    TEST_ASSERT_EQUAL(0x01, getSensorTypeId("temperature"));
    TEST_ASSERT_EQUAL(0x02, getSensorTypeId("humidity"));
    TEST_ASSERT_EQUAL(0x03, getSensorTypeId("pressure"));
    TEST_ASSERT_EQUAL(0x04, getSensorTypeId("water_level"));
    TEST_ASSERT_EQUAL(0x05, getSensorTypeId("battery"));
    TEST_ASSERT_EQUAL(0x06, getSensorTypeId("co2"));
    TEST_ASSERT_EQUAL(0xFF, getSensorTypeId("unknown_sensor"));
}

void test_single_temperature_encoding() {
    Reading r;
    r.type = "temperature";
    r.value = 18.5f;

    auto payload = encodeReading(r);

    TEST_ASSERT_EQUAL(3, payload.size());
    TEST_ASSERT_EQUAL(0x01, payload[0]);           // Type: temperature

    // Value: 18.5 * 100 = 1850 = 0x073A
    TEST_ASSERT_EQUAL(0x07, payload[1]);           // High byte
    TEST_ASSERT_EQUAL(0x3A, payload[2]);           // Low byte
}

void test_negative_temperature_encoding() {
    Reading r;
    r.type = "temperature";
    r.value = -5.5f;

    auto payload = encodeReading(r);

    TEST_ASSERT_EQUAL(3, payload.size());
    TEST_ASSERT_EQUAL(0x01, payload[0]);           // Type: temperature

    // Value: -5.5 * 100 = -550 = 0xFDDA (two's complement)
    int16_t expected = -550;
    TEST_ASSERT_EQUAL((expected >> 8) & 0xFF, payload[1]);
    TEST_ASSERT_EQUAL(expected & 0xFF, payload[2]);
}

void test_humidity_encoding() {
    Reading r;
    r.type = "humidity";
    r.value = 67.0f;

    auto payload = encodeReading(r);

    TEST_ASSERT_EQUAL(3, payload.size());
    TEST_ASSERT_EQUAL(0x02, payload[0]);           // Type: humidity

    // Value: 67.0 * 100 = 6700 = 0x1A2C
    TEST_ASSERT_EQUAL(0x1A, payload[1]);
    TEST_ASSERT_EQUAL(0x2C, payload[2]);
}

void test_pressure_encoding() {
    // Pressure uses 1 decimal place due to larger values
    Reading r;
    r.type = "pressure";
    r.value = 1005.4f;

    auto payload = encodeReading(r);

    TEST_ASSERT_EQUAL(3, payload.size());
    TEST_ASSERT_EQUAL(0x03, payload[0]);           // Type: pressure

    // Value: 1005.4 * 10 = 10054 = 0x2746
    TEST_ASSERT_EQUAL(0x27, payload[1]);
    TEST_ASSERT_EQUAL(0x46, payload[2]);
}

void test_water_level_encoding() {
    Reading r;
    r.type = "water_level";
    r.value = 85.5f;

    auto payload = encodeReading(r);

    TEST_ASSERT_EQUAL(3, payload.size());
    TEST_ASSERT_EQUAL(0x04, payload[0]);           // Type: water_level

    // Value: 85.5 * 100 = 8550 = 0x2166
    TEST_ASSERT_EQUAL(0x21, payload[1]);
    TEST_ASSERT_EQUAL(0x66, payload[2]);
}

void test_battery_encoding() {
    Reading r;
    r.type = "battery";
    r.value = 85.0f;

    auto payload = encodeReading(r);

    TEST_ASSERT_EQUAL(3, payload.size());
    TEST_ASSERT_EQUAL(0x05, payload[0]);           // Type: battery

    // Value: 85.0 * 100 = 8500 = 0x2134
    TEST_ASSERT_EQUAL(0x21, payload[1]);
    TEST_ASSERT_EQUAL(0x34, payload[2]);
}

void test_batch_encoding_4_sensors() {
    std::vector<Reading> batch = {
        {"", "temperature", 18.5f, "°C", 0},
        {"", "humidity", 67.0f, "%", 0},
        {"", "pressure", 1005.4f, "hPa", 0},
        {"", "battery", 85.0f, "%", 0}
    };

    auto payload = encodeBatch(batch);

    // 4 sensors × 3 bytes = 12 bytes
    TEST_ASSERT_EQUAL(12, payload.size());

    // Verify each sensor
    // Temperature: 0x01 0x07 0x3A
    TEST_ASSERT_EQUAL(0x01, payload[0]);
    TEST_ASSERT_EQUAL(0x07, payload[1]);
    TEST_ASSERT_EQUAL(0x3A, payload[2]);

    // Humidity: 0x02 0x1A 0x2C
    TEST_ASSERT_EQUAL(0x02, payload[3]);
    TEST_ASSERT_EQUAL(0x1A, payload[4]);
    TEST_ASSERT_EQUAL(0x2C, payload[5]);

    // Pressure: 0x03 0x27 0x46
    TEST_ASSERT_EQUAL(0x03, payload[6]);
    TEST_ASSERT_EQUAL(0x27, payload[7]);
    TEST_ASSERT_EQUAL(0x46, payload[8]);

    // Battery: 0x05 0x21 0x34
    TEST_ASSERT_EQUAL(0x05, payload[9]);
    TEST_ASSERT_EQUAL(0x21, payload[10]);
    TEST_ASSERT_EQUAL(0x34, payload[11]);
}

void test_batch_encoding_with_water_level() {
    std::vector<Reading> batch = {
        {"", "temperature", 21.5f, "°C", 0},
        {"", "humidity", 55.0f, "%", 0},
        {"", "pressure", 1013.25f, "hPa", 0},
        {"", "water_level", 150.0f, "cm", 0},
        {"", "battery", 75.0f, "%", 0}
    };

    auto payload = encodeBatch(batch);

    // 5 sensors × 3 bytes = 15 bytes
    TEST_ASSERT_EQUAL(15, payload.size());

    // Verify water level (4th reading)
    TEST_ASSERT_EQUAL(0x04, payload[9]);  // Type: water_level
    // 150.0 * 100 = 15000 = 0x3A98
    TEST_ASSERT_EQUAL(0x3A, payload[10]);
    TEST_ASSERT_EQUAL(0x98, payload[11]);
}

void test_decode_roundtrip_temperature() {
    Reading r;
    r.type = "temperature";
    r.value = 23.45f;

    auto payload = encodeReading(r);

    // Decode
    uint8_t typeId = payload[0];
    int16_t encoded = (payload[1] << 8) | payload[2];
    float decoded = decodeValue(typeId, encoded);

    // Should round-trip within 0.01°C
    TEST_ASSERT_FLOAT_WITHIN(0.01f, r.value, decoded);
}

void test_decode_roundtrip_pressure() {
    Reading r;
    r.type = "pressure";
    r.value = 1013.25f;

    auto payload = encodeReading(r);

    // Decode
    uint8_t typeId = payload[0];
    int16_t encoded = (payload[1] << 8) | payload[2];
    float decoded = decodeValue(typeId, encoded);

    // Pressure only has 1 decimal, so allow 0.1 tolerance
    TEST_ASSERT_FLOAT_WITHIN(0.1f, r.value, decoded);
}

void test_payload_size_limit() {
    // Create a batch larger than the limit
    std::vector<Reading> batch;
    for (int i = 0; i < 20; i++) {
        Reading r;
        r.type = "temperature";
        r.value = 20.0f + i;
        batch.push_back(r);
    }

    auto payload = encodeBatch(batch);

    // Should be capped at 48 bytes (16 sensors × 3 bytes)
    TEST_ASSERT_LESS_OR_EQUAL(48, payload.size());
}

void test_empty_batch() {
    std::vector<Reading> batch;

    auto payload = encodeBatch(batch);

    TEST_ASSERT_EQUAL(0, payload.size());
}

void test_single_reading_batch() {
    std::vector<Reading> batch = {
        {"", "temperature", 25.0f, "°C", 0}
    };

    auto payload = encodeBatch(batch);

    TEST_ASSERT_EQUAL(3, payload.size());
}

void test_unknown_sensor_type() {
    Reading r;
    r.type = "unknown_type";
    r.value = 42.0f;

    auto payload = encodeReading(r);

    TEST_ASSERT_EQUAL(3, payload.size());
    TEST_ASSERT_EQUAL(0xFF, payload[0]);  // Unknown type ID
}

void test_zero_values() {
    Reading r;
    r.type = "temperature";
    r.value = 0.0f;

    auto payload = encodeReading(r);

    TEST_ASSERT_EQUAL(3, payload.size());
    TEST_ASSERT_EQUAL(0x01, payload[0]);
    TEST_ASSERT_EQUAL(0x00, payload[1]);
    TEST_ASSERT_EQUAL(0x00, payload[2]);
}

void test_max_temperature_value() {
    Reading r;
    r.type = "temperature";
    r.value = 85.0f;  // BME280 max

    auto payload = encodeReading(r);

    // 85.0 * 100 = 8500 = 0x2134
    TEST_ASSERT_EQUAL(0x21, payload[1]);
    TEST_ASSERT_EQUAL(0x34, payload[2]);
}

void test_min_temperature_value() {
    Reading r;
    r.type = "temperature";
    r.value = -40.0f;  // BME280 min

    auto payload = encodeReading(r);

    // -40.0 * 100 = -4000 = 0xF060 (two's complement)
    int16_t expected = -4000;
    TEST_ASSERT_EQUAL((expected >> 8) & 0xFF, payload[1]);
    TEST_ASSERT_EQUAL(expected & 0xFF, payload[2]);
}

// ============================================================
// TEST RUNNER
// ============================================================

#ifdef UNIT_TEST

int main(int argc, char **argv) {
    UNITY_BEGIN();

    // Sensor Type ID tests
    RUN_TEST(test_sensor_type_ids);

    // Single reading encoding tests
    RUN_TEST(test_single_temperature_encoding);
    RUN_TEST(test_negative_temperature_encoding);
    RUN_TEST(test_humidity_encoding);
    RUN_TEST(test_pressure_encoding);
    RUN_TEST(test_water_level_encoding);
    RUN_TEST(test_battery_encoding);

    // Batch encoding tests
    RUN_TEST(test_batch_encoding_4_sensors);
    RUN_TEST(test_batch_encoding_with_water_level);

    // Roundtrip tests
    RUN_TEST(test_decode_roundtrip_temperature);
    RUN_TEST(test_decode_roundtrip_pressure);

    // Edge case tests
    RUN_TEST(test_payload_size_limit);
    RUN_TEST(test_empty_batch);
    RUN_TEST(test_single_reading_batch);
    RUN_TEST(test_unknown_sensor_type);
    RUN_TEST(test_zero_values);
    RUN_TEST(test_max_temperature_value);
    RUN_TEST(test_min_temperature_value);

    return UNITY_END();
}

#endif // UNIT_TEST
