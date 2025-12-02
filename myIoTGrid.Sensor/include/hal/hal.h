#ifndef HAL_H
#define HAL_H

#include <string>
#include <cstdint>

namespace hal {

/**
 * HTTP Response structure
 */
struct HttpResponse {
    int statusCode;
    std::string body;
    bool success;
    std::string errorMessage;
};

/**
 * Initialize the HAL layer
 * Must be called once at startup
 */
void init();

// ============================================
// Timing Functions
// ============================================

/**
 * Delay execution for specified milliseconds
 * @param ms Milliseconds to delay
 */
void delay_ms(uint32_t ms);

/**
 * Get milliseconds since system start
 * @return Milliseconds elapsed
 */
uint32_t millis();

/**
 * Get current Unix timestamp in seconds
 * @return Unix timestamp
 */
uint64_t timestamp();

// ============================================
// Device Identification
// ============================================

/**
 * Get or generate a unique device serial number
 * Format: PREFIX-XXXXXXXX-XXXX (e.g., SIM-A1B2C3D4-0001)
 * The serial is generated once and persisted
 * @return Device serial number
 */
std::string get_device_serial();

// ============================================
// Persistent Storage
// ============================================

/**
 * Save a string value to persistent storage
 * @param key Storage key
 * @param value Value to store
 * @return true if successful
 */
bool storage_save(const std::string& key, const std::string& value);

/**
 * Load a string value from persistent storage
 * @param key Storage key
 * @return Stored value, or empty string if not found
 */
std::string storage_load(const std::string& key);

/**
 * Check if a key exists in persistent storage
 * @param key Storage key
 * @return true if key exists
 */
bool storage_exists(const std::string& key);

/**
 * Delete a key from persistent storage
 * @param key Storage key
 * @return true if deleted (or didn't exist)
 */
bool storage_delete(const std::string& key);

// ============================================
// Network (WiFi for ESP32, always connected for Native)
// ============================================

/**
 * Connect to WiFi network
 * For Native: Always returns true (assumes network available)
 * @param ssid WiFi SSID
 * @param password WiFi password
 * @return true if connected
 */
bool network_connect(const std::string& ssid, const std::string& password);

/**
 * Check if network is connected
 * @return true if connected
 */
bool network_is_connected();

/**
 * Get the current IP address
 * @return IP address string, or empty if not connected
 */
std::string network_get_ip();

// ============================================
// HTTP Client
// ============================================

/**
 * Send HTTP POST request with JSON body
 * @param url Full URL including protocol
 * @param json JSON body string
 * @param timeoutMs Request timeout in milliseconds (default: 10000)
 * @return HttpResponse with status code and body
 */
HttpResponse http_post(const std::string& url, const std::string& json, uint32_t timeoutMs = 10000);

/**
 * Send HTTP GET request
 * @param url Full URL including protocol
 * @param timeoutMs Request timeout in milliseconds (default: 10000)
 * @return HttpResponse with status code and body
 */
HttpResponse http_get(const std::string& url, uint32_t timeoutMs = 10000);

// ============================================
// Logging
// ============================================

/**
 * Log informational message
 * @param message Message to log
 */
void log_info(const std::string& message);

/**
 * Log warning message
 * @param message Message to log
 */
void log_warn(const std::string& message);

/**
 * Log error message
 * @param message Message to log
 */
void log_error(const std::string& message);

/**
 * Log debug message (only in debug builds)
 * @param message Message to log
 */
void log_debug(const std::string& message);

// ============================================
// System
// ============================================

/**
 * Get free heap memory in bytes
 * @return Free heap memory
 */
uint32_t get_free_heap();

/**
 * Restart the device
 */
void restart();

/**
 * Get environment variable value
 * @param name Environment variable name
 * @param defaultValue Default value if not set
 * @return Environment variable value or default
 */
std::string get_env(const std::string& name, const std::string& defaultValue = "");

} // namespace hal

#endif // HAL_H
