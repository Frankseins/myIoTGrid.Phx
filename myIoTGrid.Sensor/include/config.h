#ifndef CONFIG_H
#define CONFIG_H

#include <string>

// Firmware Version
#ifndef FIRMWARE_VERSION
#define FIRMWARE_VERSION "1.10.6"  // Cloud mode: always use firmware constant URL, not stored URL
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

// Hub API Configuration (Local Mode)
constexpr const char* DEFAULT_HUB_HOST = "localhost";
constexpr int DEFAULT_HUB_PORT = 5001;           // HTTPS port
constexpr const char* DEFAULT_HUB_PROTOCOL = "https";

// Cloud API Configuration (Cloud Mode)
constexpr const char* CLOUD_API_URL = "http://api.myiotgrid.cloud:5002";
constexpr int CLOUD_API_PORT = 5002;
constexpr const char* CLOUD_API_PROTOCOL = "http";

// Target Modes
constexpr const char* TARGET_MODE_LOCAL = "local";
constexpr const char* TARGET_MODE_CLOUD = "cloud";

// WiFi Configuration (ESP32 only)
constexpr const char* DEFAULT_WIFI_SSID = "";
constexpr const char* DEFAULT_WIFI_PASSWORD = "";

// Timing Configuration
constexpr uint32_t DEFAULT_INTERVAL_SECONDS = 60;
constexpr uint32_t REGISTRATION_RETRY_DELAY_MS = 5000;
constexpr uint32_t HTTP_TIMEOUT_MS = 60000;  // 60s for Azure cold starts
constexpr int HTTP_RETRY_COUNT = 3;

// Discovery Configuration
constexpr int DISCOVERY_PORT = 5001;
constexpr int DISCOVERY_TIMEOUT_MS = 5000;
constexpr int DISCOVERY_RETRY_COUNT = 3;
constexpr int DISCOVERY_RETRY_DELAY_MS = 2000;

// Discovery Protocol Message Types
constexpr const char* DISCOVERY_MESSAGE_TYPE = "MYIOTGRID_DISCOVER";
constexpr const char* DISCOVERY_RESPONSE_TYPE = "MYIOTGRID_HUB";

// Storage Keys
constexpr const char* STORAGE_KEY_SERIAL = "serial";
constexpr const char* STORAGE_KEY_CONFIG = "config";
constexpr const char* STORAGE_KEY_DEVICE_ID = "device_id";

// API Endpoints
constexpr const char* API_REGISTER = "/api/Nodes/register";
constexpr const char* API_READINGS = "/api/readings";

// Serial Number Prefix
constexpr const char* SERIAL_PREFIX_SIM = "SIM-";
constexpr const char* SERIAL_PREFIX_ESP32 = "ESP-";

// Data Directory (Native only)
constexpr const char* DATA_DIR = "./data";

// ============================================================================
// SD Card Configuration (Sprint OS-01)
// ============================================================================
constexpr int SD_MISO_PIN = 19;           // SD Card MISO (GPIO19)
constexpr int SD_MOSI_PIN = 23;           // SD Card MOSI (GPIO23)
constexpr int SD_SCK_PIN = 18;            // SD Card SCK (GPIO18)
constexpr int SD_CS_PIN = 5;              // SD Card CS (GPIO5)

// Sync Button and Status LED (Sprint OS-01)
constexpr int SYNC_BUTTON_GPIO = 4;       // Sync button (GPIO4)
constexpr int SYNC_LED_GPIO = 2;          // Sync status LED (GPIO2 = onboard LED)

// Environment variable names
constexpr const char* ENV_HUB_HOST = "HUB_HOST";
constexpr const char* ENV_HUB_PORT = "HUB_PORT";
constexpr const char* ENV_HUB_PROTOCOL = "HUB_PROTOCOL";
constexpr const char* ENV_WIFI_SSID = "WIFI_SSID";
constexpr const char* ENV_WIFI_PASSWORD = "WIFI_PASSWORD";
constexpr const char* ENV_DISCOVERY_ENABLED = "DISCOVERY_ENABLED";
constexpr const char* ENV_DISCOVERY_PORT = "DISCOVERY_PORT";

} // namespace config

#endif // CONFIG_H
