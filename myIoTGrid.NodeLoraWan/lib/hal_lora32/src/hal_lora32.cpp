/**
 * @file hal_lora32.cpp
 * @brief HAL Implementation for Heltec LoRa32 V3
 *
 * ESP32-specific implementation of the Hardware Abstraction Layer.
 *
 * @version 1.0.0
 * @date 2025-12-10
 */

#include "hal/hal.h"
#include "config.h"

#include <Arduino.h>
#include <Wire.h>
#include <SPI.h>
#include <Preferences.h>
#include <esp_system.h>
#include <esp_sleep.h>
#include <WiFi.h>

// Preferences instance for NVS
static Preferences preferences;
static const char* NVS_NAMESPACE = "myiotgrid";

namespace hal {

// ============================================================
// TIMING FUNCTIONS
// ============================================================

void delay_ms(uint32_t ms) {
    delay(ms);
}

void delay_us(uint32_t us) {
    delayMicroseconds(us);
}

uint32_t millis() {
    return ::millis();
}

uint32_t micros() {
    return ::micros();
}

uint32_t timestamp() {
    // Return seconds since boot (no RTC available without WiFi/NTP)
    return ::millis() / 1000;
}

// ============================================================
// NON-VOLATILE STORAGE
// ============================================================

bool storage_save(const char* key, const void* data, size_t size) {
    preferences.begin(NVS_NAMESPACE, false);
    size_t written = preferences.putBytes(key, data, size);
    preferences.end();
    return written == size;
}

size_t storage_load(const char* key, void* data, size_t size) {
    preferences.begin(NVS_NAMESPACE, true);
    size_t read = preferences.getBytes(key, data, size);
    preferences.end();
    return read;
}

bool storage_exists(const char* key) {
    preferences.begin(NVS_NAMESPACE, true);
    bool exists = preferences.isKey(key);
    preferences.end();
    return exists;
}

bool storage_delete(const char* key) {
    preferences.begin(NVS_NAMESPACE, false);
    bool removed = preferences.remove(key);
    preferences.end();
    return removed;
}

bool storage_clear() {
    preferences.begin(NVS_NAMESPACE, false);
    bool cleared = preferences.clear();
    preferences.end();
    return cleared;
}

// ============================================================
// GPIO FUNCTIONS
// ============================================================

void pin_mode(uint8_t pin, PinMode mode) {
    switch (mode) {
        case PinMode::PIN_INPUT:
            pinMode(pin, INPUT);
            break;
        case PinMode::PIN_OUTPUT:
            pinMode(pin, OUTPUT);
            break;
        case PinMode::PIN_INPUT_PULLUP:
            pinMode(pin, INPUT_PULLUP);
            break;
        case PinMode::PIN_INPUT_PULLDOWN:
            pinMode(pin, INPUT_PULLDOWN);
            break;
    }
}

void digital_write(uint8_t pin, bool value) {
    digitalWrite(pin, value ? HIGH : LOW);
}

bool digital_read(uint8_t pin) {
    return digitalRead(pin) == HIGH;
}

uint16_t analog_read(uint8_t pin) {
    return analogRead(pin);
}

uint32_t pulse_in(uint8_t pin, bool state, uint32_t timeout_us) {
    return pulseIn(pin, state ? HIGH : LOW, timeout_us);
}

// ============================================================
// I2C FUNCTIONS
// ============================================================

static TwoWire* i2cBus = nullptr;

bool i2c_init(uint8_t sda, uint8_t scl, uint32_t frequency) {
    if (i2cBus == nullptr) {
        i2cBus = &Wire;
    }
    return i2cBus->begin(sda, scl, frequency);
}

uint8_t i2c_scan(uint8_t* addresses, uint8_t max_count) {
    if (i2cBus == nullptr) return 0;

    uint8_t count = 0;
    for (uint8_t addr = 1; addr < 127 && count < max_count; addr++) {
        i2cBus->beginTransmission(addr);
        if (i2cBus->endTransmission() == 0) {
            addresses[count++] = addr;
        }
    }
    return count;
}

bool i2c_device_present(uint8_t address) {
    if (i2cBus == nullptr) return false;

    i2cBus->beginTransmission(address);
    return i2cBus->endTransmission() == 0;
}

// ============================================================
// SPI FUNCTIONS
// ============================================================

static SPIClass* spiBus = nullptr;

bool spi_init(uint8_t sck, uint8_t miso, uint8_t mosi, uint32_t frequency) {
    if (spiBus == nullptr) {
        spiBus = new SPIClass(HSPI);
    }
    spiBus->begin(sck, miso, mosi);
    return true;
}

void spi_transfer(const uint8_t* tx_data, uint8_t* rx_data, size_t length) {
    if (spiBus == nullptr) return;

    spiBus->beginTransaction(SPISettings(1000000, MSBFIRST, SPI_MODE0));
    for (size_t i = 0; i < length; i++) {
        uint8_t rx = spiBus->transfer(tx_data ? tx_data[i] : 0);
        if (rx_data) rx_data[i] = rx;
    }
    spiBus->endTransaction();
}

// ============================================================
// LOGGING FUNCTIONS
// ============================================================

void log(LogLevel level, const char* tag, const char* format, ...) {
    const char* levelStr;
    switch (level) {
        case LogLevel::ERROR: levelStr = "ERROR"; break;
        case LogLevel::WARN:  levelStr = "WARN";  break;
        case LogLevel::INFO:  levelStr = "INFO";  break;
        case LogLevel::DEBUG: levelStr = "DEBUG"; break;
        default: levelStr = "???"; break;
    }

    char buffer[256];
    va_list args;
    va_start(args, format);
    vsnprintf(buffer, sizeof(buffer), format, args);
    va_end(args);

    Serial.printf("[%s] [%s] %s\n", levelStr, tag, buffer);
}

void log_error(const char* tag, const char* format, ...) {
    char buffer[256];
    va_list args;
    va_start(args, format);
    vsnprintf(buffer, sizeof(buffer), format, args);
    va_end(args);
    log(LogLevel::ERROR, tag, "%s", buffer);
}

void log_warn(const char* tag, const char* format, ...) {
    char buffer[256];
    va_list args;
    va_start(args, format);
    vsnprintf(buffer, sizeof(buffer), format, args);
    va_end(args);
    log(LogLevel::WARN, tag, "%s", buffer);
}

void log_info(const char* tag, const char* format, ...) {
    char buffer[256];
    va_list args;
    va_start(args, format);
    vsnprintf(buffer, sizeof(buffer), format, args);
    va_end(args);
    log(LogLevel::INFO, tag, "%s", buffer);
}

void log_debug(const char* tag, const char* format, ...) {
    char buffer[256];
    va_list args;
    va_start(args, format);
    vsnprintf(buffer, sizeof(buffer), format, args);
    va_end(args);
    log(LogLevel::DEBUG, tag, "%s", buffer);
}

// ============================================================
// SYSTEM FUNCTIONS
// ============================================================

uint32_t get_free_heap() {
    return ESP.getFreeHeap();
}

uint32_t get_min_free_heap() {
    return ESP.getMinFreeHeap();
}

void get_device_id(uint8_t* id) {
    uint64_t mac = ESP.getEfuseMac();
    for (int i = 0; i < 6; i++) {
        id[i] = (mac >> (i * 8)) & 0xFF;
    }
}

std::string get_device_id_string() {
    uint8_t mac[6];
    get_device_id(mac);
    char buffer[13];
    snprintf(buffer, sizeof(buffer), "%02X%02X%02X%02X%02X%02X",
             mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);
    return std::string(buffer);
}

void restart() {
    ESP.restart();
}

void deep_sleep(uint32_t seconds) {
    LOG_INFO("Entering deep sleep for %u seconds", seconds);
    Serial.flush();
    esp_sleep_enable_timer_wakeup((uint64_t)seconds * 1000000ULL);
    esp_deep_sleep_start();
}

uint8_t get_reset_reason() {
    return (uint8_t)esp_reset_reason();
}

std::string get_env(const char* name) {
    // ESP32 doesn't have environment variables
    return "";
}

// ============================================================
// RANDOM NUMBER GENERATION
// ============================================================

uint32_t get_random() {
    return esp_random();
}

uint32_t get_random_range(uint32_t min, uint32_t max) {
    if (min >= max) return min;
    return min + (esp_random() % (max - min));
}

// ============================================================
// SERIAL FUNCTIONS
// ============================================================

void serial_init(uint32_t baudrate) {
    Serial.begin(baudrate);
    while (!Serial && ::millis() < 3000) {
        // Wait for serial (max 3 seconds)
    }
}

void serial_print(const char* str) {
    Serial.print(str);
}

void serial_println(const char* str) {
    Serial.println(str);
}

int serial_available() {
    return Serial.available();
}

int serial_read() {
    return Serial.read();
}

size_t serial_read_line(char* buffer, size_t max_length, uint32_t timeout_ms) {
    size_t index = 0;
    uint32_t start = ::millis();

    while (index < max_length - 1 && (::millis() - start) < timeout_ms) {
        if (Serial.available()) {
            char c = Serial.read();
            if (c == '\n' || c == '\r') {
                if (index > 0) break;  // End of line
            } else {
                buffer[index++] = c;
            }
        }
    }

    buffer[index] = '\0';
    return index;
}

} // namespace hal
