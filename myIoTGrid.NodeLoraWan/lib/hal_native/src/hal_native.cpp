/**
 * @file hal_native.cpp
 * @brief HAL Implementation for Native/Linux simulation
 *
 * Provides simulation of hardware functions for testing on Linux.
 *
 * @version 1.0.0
 * @date 2025-12-10
 */

#include "hal/hal.h"

#include <iostream>
#include <fstream>
#include <cstring>
#include <cstdarg>
#include <chrono>
#include <thread>
#include <map>
#include <random>
#include <cstdlib>

// Simulated storage
static std::map<std::string, std::vector<uint8_t>> storage;

// Timing start point
static auto startTime = std::chrono::steady_clock::now();

namespace hal {

// ============================================================
// TIMING FUNCTIONS
// ============================================================

void delay_ms(uint32_t ms) {
    std::this_thread::sleep_for(std::chrono::milliseconds(ms));
}

void delay_us(uint32_t us) {
    std::this_thread::sleep_for(std::chrono::microseconds(us));
}

uint32_t millis() {
    auto now = std::chrono::steady_clock::now();
    return std::chrono::duration_cast<std::chrono::milliseconds>(now - startTime).count();
}

uint32_t micros() {
    auto now = std::chrono::steady_clock::now();
    return std::chrono::duration_cast<std::chrono::microseconds>(now - startTime).count();
}

uint32_t timestamp() {
    return std::chrono::duration_cast<std::chrono::seconds>(
        std::chrono::system_clock::now().time_since_epoch()
    ).count();
}

// ============================================================
// NON-VOLATILE STORAGE (Simulated)
// ============================================================

bool storage_save(const char* key, const void* data, size_t size) {
    std::vector<uint8_t> vec((uint8_t*)data, (uint8_t*)data + size);
    storage[key] = vec;
    return true;
}

size_t storage_load(const char* key, void* data, size_t size) {
    auto it = storage.find(key);
    if (it == storage.end()) return 0;

    size_t copySize = std::min(size, it->second.size());
    memcpy(data, it->second.data(), copySize);
    return copySize;
}

bool storage_exists(const char* key) {
    return storage.find(key) != storage.end();
}

bool storage_delete(const char* key) {
    return storage.erase(key) > 0;
}

bool storage_clear() {
    storage.clear();
    return true;
}

// ============================================================
// GPIO FUNCTIONS (Simulated)
// ============================================================

static std::map<uint8_t, bool> gpioState;
static std::map<uint8_t, PinMode> gpioPinMode;

void pin_mode(uint8_t pin, PinMode mode) {
    gpioPinMode[pin] = mode;
}

void digital_write(uint8_t pin, bool value) {
    gpioState[pin] = value;
}

bool digital_read(uint8_t pin) {
    auto it = gpioState.find(pin);
    return it != gpioState.end() ? it->second : false;
}

uint16_t analog_read(uint8_t pin) {
    // Return simulated ADC value (mid-range)
    return 2048;
}

uint32_t pulse_in(uint8_t pin, bool state, uint32_t timeout_us) {
    // Simulate a pulse (~1000us)
    return 1000;
}

// ============================================================
// I2C FUNCTIONS (Simulated)
// ============================================================

bool i2c_init(uint8_t sda, uint8_t scl, uint32_t frequency) {
    std::cout << "[HAL] I2C initialized (SDA=" << (int)sda
              << ", SCL=" << (int)scl << ")" << std::endl;
    return true;
}

uint8_t i2c_scan(uint8_t* addresses, uint8_t max_count) {
    // Simulate finding BME280 at 0x76
    if (max_count > 0) {
        addresses[0] = 0x76;
        return 1;
    }
    return 0;
}

bool i2c_device_present(uint8_t address) {
    // Simulate BME280
    return address == 0x76;
}

// ============================================================
// SPI FUNCTIONS (Simulated)
// ============================================================

bool spi_init(uint8_t sck, uint8_t miso, uint8_t mosi, uint32_t frequency) {
    std::cout << "[HAL] SPI initialized" << std::endl;
    return true;
}

void spi_transfer(const uint8_t* tx_data, uint8_t* rx_data, size_t length) {
    if (rx_data) {
        memset(rx_data, 0, length);
    }
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

    std::cout << "[" << levelStr << "] [" << tag << "] " << buffer << std::endl;
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
    return 200000;  // Simulated free heap
}

uint32_t get_min_free_heap() {
    return 180000;  // Simulated min free heap
}

void get_device_id(uint8_t* id) {
    // Simulated device ID
    id[0] = 0xAA;
    id[1] = 0xBB;
    id[2] = 0xCC;
    id[3] = 0xDD;
    id[4] = 0xEE;
    id[5] = 0xFF;
}

std::string get_device_id_string() {
    return "AABBCCDDEEFF";
}

void restart() {
    std::cout << "[HAL] Restart requested" << std::endl;
    exit(0);
}

void deep_sleep(uint32_t seconds) {
    std::cout << "[HAL] Deep sleep for " << seconds << " seconds" << std::endl;
    delay_ms(seconds * 1000);
}

uint8_t get_reset_reason() {
    return 1;  // Power-on reset
}

std::string get_env(const char* name) {
    const char* value = std::getenv(name);
    return value ? std::string(value) : "";
}

// ============================================================
// RANDOM NUMBER GENERATION
// ============================================================

static std::random_device rd;
static std::mt19937 gen(rd());

uint32_t get_random() {
    return gen();
}

uint32_t get_random_range(uint32_t min, uint32_t max) {
    if (min >= max) return min;
    std::uniform_int_distribution<uint32_t> dist(min, max - 1);
    return dist(gen);
}

// ============================================================
// SERIAL FUNCTIONS (Simulated via stdout/stdin)
// ============================================================

void serial_init(uint32_t baudrate) {
    std::cout << "[HAL] Serial initialized at " << baudrate << " baud" << std::endl;
}

void serial_print(const char* str) {
    std::cout << str;
}

void serial_println(const char* str) {
    std::cout << str << std::endl;
}

int serial_available() {
    return 0;  // No input in simulation
}

int serial_read() {
    return -1;
}

size_t serial_read_line(char* buffer, size_t max_length, uint32_t timeout_ms) {
    buffer[0] = '\0';
    return 0;
}

} // namespace hal
