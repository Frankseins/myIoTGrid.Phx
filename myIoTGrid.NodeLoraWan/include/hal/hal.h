/**
 * @file hal.h
 * @brief Hardware Abstraction Layer Interface
 *
 * Defines platform-independent interfaces for hardware operations.
 * Implementations are provided in hal_lora32 (ESP32) and hal_native (Linux).
 *
 * @version 1.0.0
 * @date 2025-12-10
 */

#pragma once

#include <cstdint>
#include <cstddef>
#include <string>

namespace hal {

// ============================================================
// TIMING FUNCTIONS
// ============================================================

/**
 * @brief Delay execution for specified milliseconds
 * @param ms Milliseconds to delay
 */
void delay_ms(uint32_t ms);

/**
 * @brief Delay execution for specified microseconds
 * @param us Microseconds to delay
 */
void delay_us(uint32_t us);

/**
 * @brief Get milliseconds since boot
 * @return Milliseconds since boot
 */
uint32_t millis();

/**
 * @brief Get microseconds since boot
 * @return Microseconds since boot
 */
uint32_t micros();

/**
 * @brief Get current timestamp (Unix epoch)
 * @return Unix timestamp in seconds
 */
uint32_t timestamp();

// ============================================================
// NON-VOLATILE STORAGE
// ============================================================

/**
 * @brief Save data to non-volatile storage
 * @param key Storage key
 * @param data Pointer to data
 * @param size Size of data in bytes
 * @return true if successful
 */
bool storage_save(const char* key, const void* data, size_t size);

/**
 * @brief Load data from non-volatile storage
 * @param key Storage key
 * @param data Pointer to destination buffer
 * @param size Size of buffer
 * @return Number of bytes read, 0 if not found
 */
size_t storage_load(const char* key, void* data, size_t size);

/**
 * @brief Check if key exists in storage
 * @param key Storage key
 * @return true if key exists
 */
bool storage_exists(const char* key);

/**
 * @brief Delete key from storage
 * @param key Storage key
 * @return true if deleted successfully
 */
bool storage_delete(const char* key);

/**
 * @brief Clear all storage
 * @return true if successful
 */
bool storage_clear();

// ============================================================
// GPIO FUNCTIONS
// ============================================================

enum class PinMode {
    PIN_INPUT,
    PIN_OUTPUT,
    PIN_INPUT_PULLUP,
    PIN_INPUT_PULLDOWN
};

/**
 * @brief Configure GPIO pin mode
 * @param pin GPIO pin number
 * @param mode Pin mode
 */
void pin_mode(uint8_t pin, PinMode mode);

/**
 * @brief Write digital value to GPIO pin
 * @param pin GPIO pin number
 * @param value true for HIGH, false for LOW
 */
void digital_write(uint8_t pin, bool value);

/**
 * @brief Read digital value from GPIO pin
 * @param pin GPIO pin number
 * @return true for HIGH, false for LOW
 */
bool digital_read(uint8_t pin);

/**
 * @brief Read analog value from ADC pin
 * @param pin ADC pin number
 * @return Raw ADC value (0-4095 for ESP32)
 */
uint16_t analog_read(uint8_t pin);

/**
 * @brief Measure pulse duration on GPIO pin
 * @param pin GPIO pin number
 * @param state State to measure (HIGH or LOW)
 * @param timeout_us Timeout in microseconds
 * @return Pulse duration in microseconds, 0 on timeout
 */
uint32_t pulse_in(uint8_t pin, bool state, uint32_t timeout_us);

// ============================================================
// I2C FUNCTIONS
// ============================================================

/**
 * @brief Initialize I2C bus
 * @param sda SDA pin number
 * @param scl SCL pin number
 * @param frequency I2C frequency in Hz
 * @return true if successful
 */
bool i2c_init(uint8_t sda, uint8_t scl, uint32_t frequency = 400000);

/**
 * @brief Scan I2C bus for devices
 * @param addresses Output array for found addresses
 * @param max_count Maximum number of addresses to return
 * @return Number of devices found
 */
uint8_t i2c_scan(uint8_t* addresses, uint8_t max_count);

/**
 * @brief Check if device is present on I2C bus
 * @param address I2C address (7-bit)
 * @return true if device responds
 */
bool i2c_device_present(uint8_t address);

// ============================================================
// SPI FUNCTIONS
// ============================================================

/**
 * @brief Initialize SPI bus
 * @param sck Clock pin
 * @param miso MISO pin
 * @param mosi MOSI pin
 * @param frequency SPI frequency in Hz
 * @return true if successful
 */
bool spi_init(uint8_t sck, uint8_t miso, uint8_t mosi, uint32_t frequency = 1000000);

/**
 * @brief Transfer data over SPI
 * @param tx_data Data to transmit
 * @param rx_data Buffer for received data (can be nullptr)
 * @param length Number of bytes to transfer
 */
void spi_transfer(const uint8_t* tx_data, uint8_t* rx_data, size_t length);

// ============================================================
// LOGGING FUNCTIONS
// ============================================================

enum class LogLevel {
    ERROR = 1,
    WARN = 2,
    INFO = 3,
    DEBUG = 4
};

/**
 * @brief Log a message
 * @param level Log level
 * @param tag Tag/category for the message
 * @param format Printf-style format string
 * @param ... Format arguments
 */
void log(LogLevel level, const char* tag, const char* format, ...);

/**
 * @brief Log error message
 */
void log_error(const char* tag, const char* format, ...);

/**
 * @brief Log warning message
 */
void log_warn(const char* tag, const char* format, ...);

/**
 * @brief Log info message
 */
void log_info(const char* tag, const char* format, ...);

/**
 * @brief Log debug message
 */
void log_debug(const char* tag, const char* format, ...);

// ============================================================
// SYSTEM FUNCTIONS
// ============================================================

/**
 * @brief Get free heap memory in bytes
 * @return Free heap size
 */
uint32_t get_free_heap();

/**
 * @brief Get minimum free heap since boot
 * @return Minimum free heap size
 */
uint32_t get_min_free_heap();

/**
 * @brief Get unique device ID (MAC address)
 * @param id Output buffer (minimum 6 bytes)
 */
void get_device_id(uint8_t* id);

/**
 * @brief Get device ID as string (12 hex characters)
 * @return Device ID string
 */
std::string get_device_id_string();

/**
 * @brief Restart the device
 */
void restart();

/**
 * @brief Enter deep sleep mode
 * @param seconds Sleep duration in seconds
 */
void deep_sleep(uint32_t seconds);

/**
 * @brief Get reset reason
 * @return Reset reason code
 */
uint8_t get_reset_reason();

/**
 * @brief Get environment variable (native only)
 * @param name Variable name
 * @return Variable value or empty string
 */
std::string get_env(const char* name);

// ============================================================
// RANDOM NUMBER GENERATION
// ============================================================

/**
 * @brief Get random 32-bit value
 * @return Random value
 */
uint32_t get_random();

/**
 * @brief Get random value in range
 * @param min Minimum value (inclusive)
 * @param max Maximum value (exclusive)
 * @return Random value in range
 */
uint32_t get_random_range(uint32_t min, uint32_t max);

// ============================================================
// SERIAL FUNCTIONS
// ============================================================

/**
 * @brief Initialize serial port
 * @param baudrate Baud rate
 */
void serial_init(uint32_t baudrate);

/**
 * @brief Print string to serial
 * @param str String to print
 */
void serial_print(const char* str);

/**
 * @brief Print string with newline to serial
 * @param str String to print
 */
void serial_println(const char* str);

/**
 * @brief Check if serial data is available
 * @return Number of bytes available
 */
int serial_available();

/**
 * @brief Read byte from serial
 * @return Byte read, -1 if no data
 */
int serial_read();

/**
 * @brief Read line from serial
 * @param buffer Output buffer
 * @param max_length Maximum length
 * @param timeout_ms Timeout in milliseconds
 * @return Number of bytes read
 */
size_t serial_read_line(char* buffer, size_t max_length, uint32_t timeout_ms);

} // namespace hal
