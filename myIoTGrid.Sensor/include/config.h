#ifndef CONFIG_H
#define CONFIG_H

#include <string>

// Firmware Version
#ifndef FIRMWARE_VERSION
#define FIRMWARE_VERSION "1.0.0"
#endif

// Hardware Type
#ifndef HARDWARE_TYPE
#define HARDWARE_TYPE "UNKNOWN"
#endif

// Simulate Sensors Flag
#ifndef SIMULATE_SENSORS
#define SIMULATE_SENSORS 1
#endif

// Default Hub Configuration
namespace config {

// Hub API Configuration
constexpr const char* DEFAULT_HUB_HOST = "localhost";
constexpr int DEFAULT_HUB_PORT = 5000;
constexpr const char* DEFAULT_HUB_PROTOCOL = "http";

// WiFi Configuration (ESP32 only)
constexpr const char* DEFAULT_WIFI_SSID = "";
constexpr const char* DEFAULT_WIFI_PASSWORD = "";

// Timing Configuration
constexpr uint32_t DEFAULT_INTERVAL_SECONDS = 60;
constexpr uint32_t REGISTRATION_RETRY_DELAY_MS = 5000;
constexpr uint32_t HTTP_TIMEOUT_MS = 10000;
constexpr int HTTP_RETRY_COUNT = 3;

// Storage Keys
constexpr const char* STORAGE_KEY_SERIAL = "serial";
constexpr const char* STORAGE_KEY_CONFIG = "config";
constexpr const char* STORAGE_KEY_DEVICE_ID = "device_id";

// API Endpoints
constexpr const char* API_REGISTER = "/api/devices/register";
constexpr const char* API_READINGS = "/api/readings";

// Serial Number Prefix
constexpr const char* SERIAL_PREFIX_SIM = "SIM-";
constexpr const char* SERIAL_PREFIX_ESP32 = "ESP-";

// Data Directory (Native only)
constexpr const char* DATA_DIR = "./data";

// Environment variable names
constexpr const char* ENV_HUB_HOST = "HUB_HOST";
constexpr const char* ENV_HUB_PORT = "HUB_PORT";
constexpr const char* ENV_WIFI_SSID = "WIFI_SSID";
constexpr const char* ENV_WIFI_PASSWORD = "WIFI_PASSWORD";

} // namespace config

#endif // CONFIG_H
