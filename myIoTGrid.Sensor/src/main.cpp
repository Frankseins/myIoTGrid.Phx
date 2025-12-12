/**
 * myIoTGrid.Sensor - Main Entry Point
 *
 * Self-Provisioning ESP32 Firmware with Multi-Mode Support
 *
 * Flow:
 * 1. Boot → Check NVS for stored configuration
 * 2. If no config → Start BLE pairing mode OR WPS mode (Boot button 3s)
 * 3. Receive WiFi + API config via BLE or WPS
 * 4. Connect to WiFi
 * 5. Discover Hub via UDP broadcast
 * 6. Register and enter operational mode (heartbeats + sensor readings)
 *
 * Sensor Modes:
 * - REAL: Read actual hardware sensors (DHT22, BME280, etc.)
 * - SIMULATED: Generate realistic test data with profiles
 * - AUTO: Detect hardware, fall back to simulation
 */

#include <Arduino.h>
#include <vector>
#include <map>
#include "config.h"
#include "state_machine.h"
#include "config_manager.h"
#include "wifi_manager.h"
#include "api_client.h"
#include "discovery_client.h"
#include "sensor_simulator.h"
#include "hardware_scanner.h"
#include "sensor_reader.h"
#include "led_controller.h"

// Sprint OS-01: Offline Storage Components
#include "storage/sd_manager.h"
#include "storage/storage_config.h"
#include "storage/reading_storage.h"
#include "storage/sync_manager.h"
#include "ui/sync_status_led.h"
#include "ui/sync_button.h"

// Sprint 8: Remote Debug System
#include "debug_manager.h"
#include "sd_logger.h"
#include "hardware_validator.h"
#include "debug_log_uploader.h"

#ifdef PLATFORM_ESP32
#include "ble_service.h"
#include "wps_manager.h"
#include <esp_mac.h>
#include <WiFi.h>
#include <Wire.h>
#endif

#ifdef PLATFORM_NATIVE
#include "hal/hal.h"
#endif

// ============================================================================
// Global Instances
// ============================================================================

StateMachine stateMachine;
ConfigManager configManager;
WiFiManager wifiManager;
ApiClient apiClient;
DiscoveryClient discoveryClient;
SensorSimulator sensorSimulator;
HardwareScanner hardwareScanner;
SensorReader sensorReader;
LEDController ledController;

// Sprint OS-01: Offline Storage Instances
SDManager sdManager;
StorageConfigManager storageConfigManager;
ReadingStorage readingStorage;
SyncManager syncManager;
SyncStatusLED syncStatusLED;
SyncButton syncButton;
bool offlineStorageEnabled = false;

#ifdef PLATFORM_ESP32
BLEProvisioningService bleService;
WPSManager wpsManager;

// RE_PAIRING Configuration (Story 4)
static const unsigned long RE_PAIRING_WIFI_RETRY_INTERVAL_MS = 30000;  // 30 seconds
static unsigned long lastRePairingWifiRetry = 0;
static bool rePairingActive = false;

// Boot button for WPS mode and Factory Reset
static const int BOOT_BUTTON_PIN = 0;  // GPIO0 = Boot button on most ESP32 boards
static const unsigned long WPS_BUTTON_HOLD_MS = 3000;      // 3 seconds to trigger WPS
static const unsigned long FACTORY_RESET_HOLD_MS = 10000;  // 10 seconds for factory reset
static unsigned long buttonPressStart = 0;
static bool buttonWasPressed = false;
static bool wpsTriggered = false;
static bool factoryResetTriggered = false;

// Hardware detection flags
static bool dht22Detected = false;
static bool bme280Detected = false;
static bool bme680Detected = false;
static bool sht31Detected = false;
#endif

// Sensor mode configuration
enum class SensorMode {
    AUTO,       // Auto-detect hardware, fall back to simulation
    REAL,       // Only use real hardware sensors
    SIMULATED   // Only use simulated values
};
static SensorMode sensorMode = SensorMode::AUTO;
static bool hardwareDetected = false;

// ============================================================================
// Configuration
// ============================================================================

static const unsigned long HEARTBEAT_INTERVAL_MS = 60000;   // 1 minute
static const unsigned long SENSOR_INTERVAL_MS = 60000;      // 1 minute
static const unsigned long WIFI_CHECK_INTERVAL_MS = 5000;   // 5 seconds
static const unsigned long CONFIG_CHECK_INTERVAL_MS = 60000; // 60 seconds
static const unsigned long DEBUG_CONFIG_CHECK_INTERVAL_MS = 60000; // 60 seconds for debug config sync (same as sensor config)

static unsigned long lastHeartbeat = 0;
static unsigned long lastSensorReading = 0;
static unsigned long lastWiFiCheck = 0;
static unsigned long lastConfigCheck = 0;
static unsigned long lastDebugConfigCheck = 0;

// Per-sensor timing for GCD-based polling
static std::map<int, unsigned long> sensorLastReading;  // endpointId -> last reading time
static int calculatedPollIntervalSeconds = 60;  // GCD of all sensor intervals

// Current sensor configuration from Hub
static NodeConfigurationResponse currentConfig;
static bool configLoaded = false;
static String currentSerial;
static unsigned long lastValidatedConfigTimestamp = 0;  // Track when we last validated hardware

// ============================================================================
// URL Helper Functions
// ============================================================================

/**
 * Ensure Hub URL has a port. Adds default port 5002 if missing.
 * E.g., "http://192.168.1.100" -> "http://192.168.1.100:5002"
 */
String ensureUrlHasPort(const String& url, int defaultPort = 5002) {
    if (url.length() == 0) return url;

    // Find protocol end (after "://")
    int protocolEnd = url.indexOf("://");
    if (protocolEnd < 0) protocolEnd = 0;
    else protocolEnd += 3;

    // Check if there's already a port after the host
    String afterProtocol = url.substring(protocolEnd);
    int portIndex = afterProtocol.indexOf(':');

    if (portIndex >= 0) {
        // URL already has a port
        return url;
    }

    // No port found - add default port
    // Find where host ends (before path if any)
    int pathStart = afterProtocol.indexOf('/');
    if (pathStart < 0) {
        // No path - just append port
        return url + ":" + String(defaultPort);
    } else {
        // Has path - insert port before path
        String host = url.substring(0, protocolEnd + pathStart);
        String path = url.substring(protocolEnd + pathStart);
        return host + ":" + String(defaultPort) + path;
    }
}

// ============================================================================
// Sensor Polling Interval Calculation (GCD-based)
// ============================================================================

/**
 * Calculate GCD (Greatest Common Divisor) of two numbers using Euclidean algorithm
 */
int gcd(int a, int b) {
    if (b == 0) return a;
    return gcd(b, a % b);
}

/**
 * Calculate GCD of all sensor intervals
 * This determines the poll loop interval - only sensors due at each tick are read
 *
 * Example: Sensors with 30s, 20s, 15s intervals
 * GCD(30, 20, 15) = 5
 * Poll every 5s, but only read sensors when their interval elapsed
 */
int calculatePollIntervalGCD() {
    if (!configLoaded || currentConfig.sensors.size() == 0) {
        return currentConfig.defaultIntervalSeconds > 0 ? currentConfig.defaultIntervalSeconds : 60;
    }

    int result = 0;
    for (const auto& sensor : currentConfig.sensors) {
        if (sensor.isActive && sensor.intervalSeconds > 0) {
            if (result == 0) {
                result = sensor.intervalSeconds;
            } else {
                result = gcd(result, sensor.intervalSeconds);
            }
        }
    }

    // Fallback to default if no active sensors with intervals
    if (result == 0) {
        result = currentConfig.defaultIntervalSeconds > 0 ? currentConfig.defaultIntervalSeconds : 60;
    }

    return result;
}

/**
 * Check if a specific sensor is due for reading based on its interval
 */
bool isSensorDue(const SensorAssignmentConfig& sensor, unsigned long now) {
    if (!sensor.isActive || sensor.intervalSeconds <= 0) {
        return false;
    }

    auto it = sensorLastReading.find(sensor.endpointId);
    if (it == sensorLastReading.end()) {
        // First reading - sensor is due
        return true;
    }

    unsigned long elapsed = (now - it->second) / 1000;  // Convert to seconds
    return elapsed >= (unsigned long)sensor.intervalSeconds;
}

/**
 * Mark sensor as read at current time
 */
void markSensorRead(int endpointId, unsigned long now) {
    sensorLastReading[endpointId] = now;
}

// ============================================================================
// Hardware Detection (ESP32 only)
// ============================================================================

#ifdef PLATFORM_ESP32
/**
 * Scan I2C bus for connected sensors
 * BME280: 0x76 or 0x77
 * BME680: 0x76 or 0x77 (different chip ID)
 * SHT31: 0x44 or 0x45
 */
void detectI2CSensors() {
    Wire.begin(21, 22);  // Explicit SDA=21, SCL=22
    Serial.println("[HW] Scanning I2C bus (SDA=21, SCL=22)...");

    // Full I2C scan first
    int devicesFound = 0;
    Serial.println("[HW] Full I2C scan:");
    for (uint8_t addr = 1; addr < 127; addr++) {
        Wire.beginTransmission(addr);
        byte error = Wire.endTransmission();
        if (error == 0) {
            Serial.printf("[HW]   Found device at 0x%02X\n", addr);
            devicesFound++;
        }
    }
    if (devicesFound == 0) {
        Serial.println("[HW]   No I2C devices found!");
        Serial.println("[HW]   Check wiring: SDA->GPIO21, SCL->GPIO22, VCC->3.3V, GND->GND");
    } else {
        Serial.printf("[HW]   Total: %d device(s) found\n", devicesFound);
    }

    // Check for BME280/BME680 at 0x76
    Wire.beginTransmission(0x76);
    if (Wire.endTransmission() == 0) {
        // Read chip ID to distinguish BME280 vs BME680
        Wire.beginTransmission(0x76);
        Wire.write(0xD0);  // Chip ID register
        Wire.endTransmission();
        Wire.requestFrom(0x76, 1);
        if (Wire.available()) {
            uint8_t chipId = Wire.read();
            if (chipId == 0x60) {
                bme280Detected = true;
                Serial.println("[HW] BME280 detected at 0x76");
            } else if (chipId == 0x61) {
                bme680Detected = true;
                Serial.println("[HW] BME680 detected at 0x76");
            }
        }
    }

    // Check for BME280/BME680 at 0x77
    Wire.beginTransmission(0x77);
    if (Wire.endTransmission() == 0) {
        Wire.beginTransmission(0x77);
        Wire.write(0xD0);
        Wire.endTransmission();
        Wire.requestFrom(0x77, 1);
        if (Wire.available()) {
            uint8_t chipId = Wire.read();
            if (chipId == 0x60 && !bme280Detected) {
                bme280Detected = true;
                Serial.println("[HW] BME280 detected at 0x77");
            } else if (chipId == 0x61 && !bme680Detected) {
                bme680Detected = true;
                Serial.println("[HW] BME680 detected at 0x77");
            }
        }
    }

    // Check for SHT31 at 0x44
    Wire.beginTransmission(0x44);
    if (Wire.endTransmission() == 0) {
        sht31Detected = true;
        Serial.println("[HW] SHT31 detected at 0x44");
    }

    // Check for SHT31 at 0x45
    Wire.beginTransmission(0x45);
    if (Wire.endTransmission() == 0 && !sht31Detected) {
        sht31Detected = true;
        Serial.println("[HW] SHT31 detected at 0x45");
    }
}

/**
 * Check for DHT22 on GPIO4 (default pin)
 * This is a simple presence detection, not a full read
 */
void detectDHT22() {
    const int DHT_PIN = 4;  // Default DHT22 pin

    pinMode(DHT_PIN, INPUT_PULLUP);
    delay(100);

    // DHT22 should pull the line low when responding
    // Simple detection: check if pin can be controlled
    pinMode(DHT_PIN, OUTPUT);
    digitalWrite(DHT_PIN, LOW);
    delay(20);
    pinMode(DHT_PIN, INPUT_PULLUP);
    delay(40);

    // If a DHT22 is connected, the line should go low briefly
    int lowCount = 0;
    for (int i = 0; i < 100; i++) {
        if (digitalRead(DHT_PIN) == LOW) {
            lowCount++;
        }
        delayMicroseconds(10);
    }

    // If we saw some low pulses, assume DHT22 is present
    if (lowCount > 5 && lowCount < 80) {
        dht22Detected = true;
        Serial.println("[HW] DHT22 detected on GPIO4");
    }
}

/**
 * Perform hardware auto-detection (simplified - no pin scanning)
 */
void autoDetectHardware() {
    Serial.println("[HW] Auto-detecting hardware sensors...");

    // Simple I2C scan only
    detectI2CSensors();
    detectDHT22();

    hardwareDetected = dht22Detected || bme280Detected || bme680Detected || sht31Detected;

    if (hardwareDetected) {
        Serial.println("[HW] Hardware sensors detected - using REAL mode");
        sensorMode = SensorMode::REAL;
    } else {
        Serial.println("[HW] No hardware sensors detected - using SIMULATED mode");
        sensorMode = SensorMode::SIMULATED;
    }
}
#endif

// ============================================================================
// WPS Callbacks (ESP32 only)
// ============================================================================

#ifdef PLATFORM_ESP32
void onWPSSuccess(const String& ssid, const String& password) {
    Serial.println("[Main] WPS SUCCESS - WiFi credentials received!");
    Serial.printf("[Main] SSID: %s\n", ssid.c_str());

    // Wait for DHCP to assign IP address
    Serial.println("[Main] Waiting for IP address...");
    int attempts = 0;
    while (WiFi.localIP() == IPAddress(0, 0, 0, 0) && attempts < 30) {
        delay(500);
        Serial.print(".");
        attempts++;
    }
    Serial.println();

    if (WiFi.localIP() == IPAddress(0, 0, 0, 0)) {
        Serial.println("[Main] Failed to get IP address!");
        stateMachine.processEvent(StateEvent::WIFI_FAILED);
        return;
    }

    Serial.printf("[Main] Got IP address: %s\n", WiFi.localIP().toString().c_str());

    // Now start Hub Discovery via UDP broadcast
    Serial.println("[Main] Starting Hub Discovery...");
    String serial = bleService.getMacAddress();
    serial.replace(":", "");  // Remove colons from MAC

    DiscoveryResponse hubResponse = discoveryClient.discover(
        serial,
        FIRMWARE_VERSION,
        HARDWARE_TYPE
    );

    StoredConfig storedConfig;
    storedConfig.wifiSsid = ssid;
    storedConfig.wifiPassword = password;

    if (hubResponse.success) {
        Serial.printf("[Main] Hub found: %s at %s\n",
                     hubResponse.hubName.c_str(), hubResponse.apiUrl.c_str());

        storedConfig.hubApiUrl = hubResponse.apiUrl;
        storedConfig.nodeId = serial;  // Use MAC as node ID for now
        storedConfig.apiKey = "";  // Will be assigned by Hub during registration
        storedConfig.isValid = true;
    } else {
        Serial.printf("[Main] Hub Discovery failed: %s\n", hubResponse.errorMessage.c_str());
        Serial.println("[Main] Saving WiFi-only config, will retry discovery later...");

        // Save config with empty hub URL - will need manual config or retry
        storedConfig.hubApiUrl = "";
        storedConfig.nodeId = serial;
        storedConfig.apiKey = "";
        storedConfig.isValid = true;  // WiFi is valid, just no hub yet
    }

    if (configManager.saveConfig(storedConfig)) {
        Serial.println("[Main] Configuration saved to NVS");

        if (hubResponse.success) {
            // Full config - go to CONFIGURED
            stateMachine.processEvent(StateEvent::WIFI_CONNECTED);
        } else {
            // WiFi only - stay in pairing mode for BLE config of Hub URL
            Serial.println("[Main] No Hub found - waiting for BLE configuration or restart discovery");
            wpsTriggered = false;
            bleService.startAdvertising();  // Re-enable BLE for manual config
        }
    }
}

void onWPSFailed(const String& reason) {
    Serial.printf("[Main] WPS FAILED: %s\n", reason.c_str());
    wpsTriggered = false;

    // Return to BLE pairing mode
    Serial.println("[Main] Returning to BLE pairing mode...");
}

void onWPSTimeout() {
    Serial.println("[Main] WPS TIMEOUT - No response from router");
    wpsTriggered = false;

    // Return to BLE pairing mode
    Serial.println("[Main] Returning to BLE pairing mode...");
}

/**
 * Check Boot button for Factory Reset only (10 second hold)
 * Used in CONFIGURED, OPERATIONAL, and ERROR states
 */
void checkBootButtonForFactoryReset() {
    static unsigned long lastDebugPrint = 0;
    bool buttonPressed = (digitalRead(BOOT_BUTTON_PIN) == LOW);

    if (buttonPressed && !buttonWasPressed) {
        buttonPressStart = millis();
        buttonWasPressed = true;
        factoryResetTriggered = false;
        Serial.println("[Button] Boot button pressed - hold 10s for Factory Reset");
    } else if (buttonPressed && buttonWasPressed) {
        unsigned long holdTime = millis() - buttonPressStart;
        if (millis() - lastDebugPrint >= 1000) {
            lastDebugPrint = millis();
            Serial.printf("[Button] %lu ms / %d ms (Factory Reset)\n", holdTime, FACTORY_RESET_HOLD_MS);
        }

        if (!factoryResetTriggered && (holdTime >= FACTORY_RESET_HOLD_MS)) {
            factoryResetTriggered = true;

            Serial.println();
            Serial.println("========================================");
            Serial.println("  FACTORY RESET - Clearing all config!");
            Serial.println("========================================");
            Serial.println();

            configManager.factoryReset();
        }
    } else if (!buttonPressed && buttonWasPressed) {
        unsigned long holdTime = millis() - buttonPressStart;
        if (holdTime < FACTORY_RESET_HOLD_MS) {
            Serial.printf("[Button] Released after %lu ms - no action (need 10s for Factory Reset)\n", holdTime);
        }
        buttonWasPressed = false;
        factoryResetTriggered = false;
    }
}

/**
 * Check Boot button for WPS trigger (3 second hold) or Factory Reset (10 second hold)
 */
void checkBootButton() {
    static unsigned long lastDebugPrint = 0;
    bool buttonPressed = (digitalRead(BOOT_BUTTON_PIN) == LOW);

    if (buttonPressed && !buttonWasPressed) {
        // Button just pressed
        buttonPressStart = millis();
        buttonWasPressed = true;
        factoryResetTriggered = false;
        Serial.println("[Button] Boot button pressed:");
        Serial.println("[Button]   - Hold 3s  = WPS mode");
        Serial.println("[Button]   - Hold 10s = Factory Reset");
    } else if (buttonPressed && buttonWasPressed) {
        // Button still held - show progress every 500ms
        unsigned long holdTime = millis() - buttonPressStart;
        if (millis() - lastDebugPrint >= 500) {
            lastDebugPrint = millis();
            if (holdTime < WPS_BUTTON_HOLD_MS) {
                Serial.printf("[Button] %lu ms / %d ms (WPS)\n", holdTime, WPS_BUTTON_HOLD_MS);
            } else if (holdTime < FACTORY_RESET_HOLD_MS) {
                Serial.printf("[Button] %lu ms / %d ms (Factory Reset)\n", holdTime, FACTORY_RESET_HOLD_MS);
            }
        }

        // Check for Factory Reset first (10 seconds)
        if (!factoryResetTriggered && (holdTime >= FACTORY_RESET_HOLD_MS)) {
            factoryResetTriggered = true;
            wpsTriggered = false;  // Cancel WPS if it was triggered

            Serial.println();
            Serial.println("========================================");
            Serial.println("  FACTORY RESET - Clearing all config!");
            Serial.println("========================================");
            Serial.println();

            // Stop BLE if running
            if (bleService.isInitialized()) {
                bleService.stop();
            }

            // Perform factory reset (clears NVS and restarts)
            configManager.factoryReset();
            // Note: factoryReset() calls ESP.restart(), so we won't reach here
        }
        // Check for WPS (3 seconds)
        else if (!wpsTriggered && !factoryResetTriggered && (holdTime >= WPS_BUTTON_HOLD_MS)) {
            // Held for 3 seconds - trigger WPS
            wpsTriggered = true;
            Serial.println("[Main] Boot button held for 3s - Starting WPS!");
            Serial.println("[Main] (Keep holding for 10s total for Factory Reset)");

            // Stop BLE advertising for WPS (don't fully deinit to avoid crash)
            if (bleService.isInitialized()) {
                Serial.println("[Main] Pausing BLE for WPS...");
                bleService.stopForWPS();
            }

            // Ensure WiFi is in a clean state
            WiFi.disconnect(true);
            WiFi.mode(WIFI_OFF);
            delay(100);

            // Start WPS
            Serial.println("[Main] Initializing WPS...");
            wpsManager.startWPS();
        }
    } else if (!buttonPressed && buttonWasPressed) {
        // Button released
        unsigned long holdTime = millis() - buttonPressStart;
        if (holdTime < WPS_BUTTON_HOLD_MS) {
            Serial.printf("[Button] Released after %lu ms - no action (need 3s for WPS)\n", holdTime);
        } else if (holdTime < FACTORY_RESET_HOLD_MS && wpsTriggered) {
            Serial.printf("[Button] Released after %lu ms - WPS started\n", holdTime);
        }
        buttonWasPressed = false;
        wpsTriggered = false;
        factoryResetTriggered = false;
    }
}
#endif

// ============================================================================
// BLE Callbacks (ESP32 only)
// ============================================================================

#ifdef PLATFORM_ESP32
void onBLEConfigReceived(const BLEConfig& config) {
    Serial.println("[Main] ========================================");
    Serial.println("[Main] BLE CONFIGURATION RECEIVED");
    Serial.println("[Main] ========================================");
    Serial.printf("[Main] NodeID: %s\n", config.nodeId.c_str());
    Serial.printf("[Main] WiFi SSID: %s\n", config.wifiSsid.c_str());
    Serial.printf("[Main] Target Mode: %s\n", config.targetMode.c_str());

    // Determine the effective Hub URL based on target mode
    String effectiveHubUrl = config.hubApiUrl;

    if (config.isCloudMode()) {
        // Cloud mode - use fixed cloud endpoint, skip UDP discovery
        effectiveHubUrl = String(config::CLOUD_API_URL);
        Serial.println("[Main] Mode: CLOUD CONNECTION (fixed endpoint, no UDP discovery)");
        Serial.printf("[Main] Cloud URL: %s\n", effectiveHubUrl.c_str());
        Serial.printf("[Main] TenantID: %s\n", config.tenantId.c_str());
    } else if (config.hubApiUrl.length() > 0) {
        Serial.println("[Main] Mode: DIRECT HUB CONNECTION (no UDP discovery needed)");
        Serial.printf("[Main] Hub URL: %s\n", config.hubApiUrl.c_str());
    } else {
        Serial.println("[Main] Mode: WIFI-ONLY (will discover Hub via UDP broadcast)");
    }

    // Save configuration to NVS
    StoredConfig storedConfig;
    storedConfig.nodeId = config.nodeId;
    storedConfig.apiKey = config.apiKey;
    storedConfig.wifiSsid = config.wifiSsid;
    storedConfig.wifiPassword = config.wifiPassword;
    storedConfig.hubApiUrl = effectiveHubUrl;  // Use effective URL (cloud URL for cloud mode)
    storedConfig.targetMode = config.targetMode;
    storedConfig.tenantId = config.tenantId;
    storedConfig.isValid = true;

    if (configManager.saveConfig(storedConfig)) {
        Serial.println("[Main] Configuration saved to NVS");

        // Stop BLE service
        bleService.stop();

        // Configure API client if Hub/Cloud URL is available
        // For cloud mode: always have the cloud URL
        // For local mode with hub_url: use provided URL
        // For local mode without hub_url: handleConfiguredState will discover it via UDP
        if (effectiveHubUrl.length() > 0) {
            Serial.println("[Main] Configuring API client with target URL...");
            apiClient.configure(ensureUrlHasPort(effectiveHubUrl), config.nodeId, config.apiKey);
        }

        // Transition to CONFIGURED state (will attempt WiFi connection)
        stateMachine.processEvent(StateEvent::BLE_CONFIG_RECEIVED);
    } else {
        Serial.println("[Main] Failed to save configuration!");
        stateMachine.processEvent(StateEvent::ERROR_OCCURRED);
    }
}

/**
 * Callback for RE_PAIRING mode - new WiFi config received via BLE (Story 4)
 */
void onReProvisioningConfigReceived(const BLEConfig& config) {
    Serial.println("[Main] ========================================");
    Serial.println("[Main] RE_PAIRING: NEW WIFI CONFIG RECEIVED");
    Serial.println("[Main] ========================================");
    Serial.printf("[Main] NodeID: %s\n", config.nodeId.c_str());
    Serial.printf("[Main] WiFi SSID: %s\n", config.wifiSsid.c_str());
    Serial.printf("[Main] Target Mode: %s\n", config.targetMode.c_str());

    // Determine the effective Hub URL based on target mode
    String effectiveHubUrl = config.hubApiUrl;

    if (config.isCloudMode()) {
        // Cloud mode - use fixed cloud endpoint
        effectiveHubUrl = String(config::CLOUD_API_URL);
        Serial.println("[Main] RE_PAIRING: Cloud mode - using fixed cloud endpoint");
        Serial.printf("[Main] TenantID: %s\n", config.tenantId.c_str());
    }

    // Save new configuration to NVS
    StoredConfig storedConfig;
    storedConfig.nodeId = config.nodeId;
    storedConfig.apiKey = config.apiKey;
    storedConfig.wifiSsid = config.wifiSsid;
    storedConfig.wifiPassword = config.wifiPassword;
    storedConfig.hubApiUrl = effectiveHubUrl;
    storedConfig.targetMode = config.targetMode;
    storedConfig.tenantId = config.tenantId;
    storedConfig.isValid = true;

    if (configManager.saveConfig(storedConfig)) {
        Serial.println("[Main] RE_PAIRING: New configuration saved to NVS");

        // Stop BLE service
        bleService.stop();
        rePairingActive = false;

        // Configure API client if Hub/Cloud URL is available
        if (effectiveHubUrl.length() > 0) {
            Serial.println("[Main] Configuring API client with target URL...");
            apiClient.configure(ensureUrlHasPort(effectiveHubUrl), config.nodeId, config.apiKey);
        }

        // Signal NEW_WIFI_RECEIVED event
        stateMachine.processEvent(StateEvent::NEW_WIFI_RECEIVED);
    } else {
        Serial.println("[Main] RE_PAIRING: Failed to save configuration!");
    }
}
#endif

// ============================================================================
// WiFi Callbacks
// ============================================================================

void onWiFiConnected(const String& ip) {
    Serial.printf("[Main] WiFi connected! IP: %s\n", ip.c_str());
    stateMachine.processEvent(StateEvent::WIFI_CONNECTED);
}

void onWiFiDisconnected() {
    Serial.println("[Main] WiFi disconnected!");
    stateMachine.processEvent(StateEvent::WIFI_FAILED);
}

void onWiFiFailed(const String& reason) {
    Serial.printf("[Main] WiFi connection failed: %s\n", reason.c_str());
    stateMachine.processEvent(StateEvent::WIFI_FAILED);
}

// ============================================================================
// Operational Functions
// ============================================================================

void sendHeartbeat() {
    if (!apiClient.isConfigured()) {
        return;
    }

#ifndef PLATFORM_NATIVE
    // ESP32: Check WiFi connection
    if (!wifiManager.isConnected()) {
        return;
    }
#endif

    HeartbeatResponse response = apiClient.sendHeartbeat(FIRMWARE_VERSION);
    if (response.success) {
        Serial.printf("[Main] Heartbeat OK, next in %d seconds\n",
                      response.nextHeartbeatSeconds);
    } else {
        Serial.println("[Main] Heartbeat failed!");
    }
}

// Flag to track if simulation mode has been logged (reset on config change)
static bool simulationModeLogged = false;

/**
 * Fetch or refresh sensor configuration from Hub
 */
void fetchSensorConfiguration() {
    if (!apiClient.isConfigured()) {
        return;
    }

    if (currentSerial.length() == 0) {
        Serial.println("[Main] Serial not set, cannot fetch configuration");
        return;
    }

    Serial.println("[Main] Fetching sensor configuration from Hub...");

    // Remember previous simulation state to detect changes
    bool wasSimulation = configLoaded ? currentConfig.isSimulation : false;

    NodeConfigurationResponse response = apiClient.fetchConfiguration(currentSerial);

    if (response.success) {
        currentConfig = response;
        configLoaded = true;

        // Calculate GCD-based poll interval for all active sensors
        calculatedPollIntervalSeconds = calculatePollIntervalGCD();

        // Log sensor intervals for debugging
        Serial.printf("[Main] Configuration updated: %d sensors\n", (int)currentConfig.sensors.size());
        Serial.printf("[Main] Poll interval: %ds (GCD of sensor intervals)\n", calculatedPollIntervalSeconds);
        for (const auto& sensor : currentConfig.sensors) {
            if (sensor.isActive) {
                Serial.printf("[Main]   - %s (Endpoint %d): every %ds\n",
                              sensor.sensorName.c_str(), sensor.endpointId, sensor.intervalSeconds);
            }
        }

        // Log simulation mode change
        if (currentConfig.isSimulation != wasSimulation || !simulationModeLogged) {
            simulationModeLogged = false;  // Reset to log new mode in readAndSendSensors()
            if (currentConfig.isSimulation) {
                Serial.println("[Main] Node is in SIMULATION mode (isSimulation=true)");
            } else {
                Serial.println("[Main] Node is in REAL HARDWARE mode (isSimulation=false)");
            }
        }

        // Validate hardware configuration only when:
        // 1. Not in simulation mode
        // 2. Sensors are configured
        // 3. Configuration has changed (different timestamp) or first time validation
        if (!currentConfig.isSimulation && currentConfig.sensors.size() > 0) {
            if (currentConfig.configurationTimestamp != lastValidatedConfigTimestamp) {
                Serial.println("[Main] Configuration changed - validating hardware...");
                ValidationSummary validationResult = hardwareScanner.validateConfiguration(currentConfig.sensors);

                if (!validationResult.allFound()) {
                    Serial.println("\n[Main] WARNING: Hardware validation found missing sensors!");
                    Serial.println("[Main] Some configured sensors may not work correctly.");
                    Serial.println("[Main] Please check your hardware connections.\n");
                } else {
                    Serial.println("[Main] Hardware validation successful - all sensors detected!");
                }

                // Remember this timestamp so we don't re-validate until config changes
                lastValidatedConfigTimestamp = currentConfig.configurationTimestamp;
            }
        }

        // Sprint OS-01: Apply storageMode from API to storageConfigManager
#ifdef PLATFORM_ESP32
        if (offlineStorageEnabled) {
            StorageMode apiMode = static_cast<StorageMode>(response.storageMode);
            StorageMode currentMode = storageConfigManager.getMode();

            if (apiMode != currentMode) {
                Serial.printf("[Main] Storage Mode changed: %s -> %s (from Hub API)\n",
                              StorageConfig::getModeString(currentMode),
                              StorageConfig::getModeString(apiMode));
                storageConfigManager.setMode(apiMode);
                storageConfigManager.save(sdManager);
            }
        }
#endif
    } else {
        Serial.printf("[Main] Config fetch: %s\n", response.error.c_str());
        // Don't clear configLoaded - keep using last known config
    }
}

/**
 * Periodically check and apply debug configuration from Hub (Sprint 8)
 * Called in handleOperationalState to sync debug level and remote logging settings
 */
void checkDebugConfiguration() {
    if (!apiClient.isConfigured()) {
        return;
    }

    String nodeId = apiClient.getNodeId();
    if (nodeId.length() == 0) {
        return;
    }

    DebugConfigurationResponse debugConfig = apiClient.fetchDebugConfiguration(nodeId);
    if (debugConfig.success) {
        // Apply debug level to DebugManager
        DebugLevel newLevel = static_cast<DebugLevel>(debugConfig.debugLevel);
        DebugLevel currentLevel = DebugManager::getInstance().getLevel();

        if (newLevel != currentLevel) {
            DebugManager::getInstance().setLevel(newLevel);
            Serial.printf("[Main] Debug level changed: %d -> %d (from Hub sync)\n",
                          static_cast<int>(currentLevel), static_cast<int>(newLevel));
        }

        // Apply remote logging setting
        DebugManager::getInstance().setRemoteLogging(debugConfig.enableRemoteLogging);

        // Enable/disable debug log uploader
        bool uploaderWasEnabled = DebugLogUploader::getInstance().isEnabled();
        if (debugConfig.enableRemoteLogging) {
            if (!uploaderWasEnabled) {
                // Initialize SerialCapture and uploader on first enable
                DebugLogUploader::getInstance().begin(apiClient.getBaseUrl(), nodeId);
                Serial.println("[Main] Remote logging ENABLED (from Hub sync)");
            }
            DebugLogUploader::getInstance().setEnabled(true);
        } else {
            DebugLogUploader::getInstance().setEnabled(false);
            if (uploaderWasEnabled) {
                Serial.println("[Main] Remote logging DISABLED (from Hub sync)");
            }
        }
    }
}

/**
 * Generate simulated value using the advanced SensorSimulator
 * Supports 5 profiles: NORMAL, WINTER, SUMMER, STORM, STRESS
 */
double generateSimulatedValue(const String& sensorCode, const String& unit) {
    // Use the new SensorSimulator for realistic values
    String code = sensorCode;
    code.toLowerCase();

    // Temperature sensors
    if (code.indexOf("temp") >= 0 || unit == "°C" || unit == "C") {
        return sensorSimulator.getTemperature();
    }

    // Humidity sensors
    if (code.indexOf("humid") >= 0 || code.indexOf("hum") >= 0 || unit == "%" || unit == "% RH") {
        // Check if it's specifically humidity (not soil moisture)
        if (code.indexOf("soil") < 0 && code.indexOf("moisture") < 0) {
            return sensorSimulator.getHumidity();
        }
    }

    // Pressure sensors
    if (code.indexOf("pressure") >= 0 || code.indexOf("bmp") >= 0 ||
        unit == "hPa" || unit == "mbar") {
        return sensorSimulator.getPressure();
    }

    // CO2 sensors
    if (code.indexOf("co2") >= 0 || unit == "ppm") {
        return sensorSimulator.getCO2();
    }

    // Light sensors
    if (code.indexOf("light") >= 0 || unit == "lux" || unit == "lx") {
        return sensorSimulator.getLight();
    }

    // Soil moisture sensors
    if (code.indexOf("soil") >= 0 || code.indexOf("moisture") >= 0) {
        return sensorSimulator.getSoilMoisture();
    }

    // GPS satellites (simulate 0-12, mostly 6-10)
    if (code.indexOf("gps_satellite") >= 0 || code.indexOf("satellite") >= 0) {
        // Simulate satellite count with occasional cold start (0 satellites)
        static int lastSatellites = 0;
        static unsigned long lastSatUpdate = 0;
        unsigned long now = millis();

        if (now - lastSatUpdate > 5000) {  // Update every 5 seconds
            lastSatUpdate = now;
            // 10% chance of cold start (0 satellites)
            if (random(100) < 10) {
                lastSatellites = 0;
            } else {
                // Normal operation: 4-12 satellites
                lastSatellites = random(4, 13);
            }
        }
        return (double)lastSatellites;
    }

    // GPS fix type (0=none, 2=2D, 3=3D)
    if (code.indexOf("gps_fix") >= 0 || code.indexOf("fix_type") >= 0) {
        // Simulate fix type based on "satellite" count (correlate with satellites)
        static int lastFixType = 0;
        static unsigned long lastFixUpdate = 0;
        unsigned long now = millis();

        if (now - lastFixUpdate > 5000) {
            lastFixUpdate = now;
            // 10% chance no fix, 20% chance 2D, 70% chance 3D
            int r = random(100);
            if (r < 10) lastFixType = 0;
            else if (r < 30) lastFixType = 2;
            else lastFixType = 3;
        }
        return (double)lastFixType;
    }

    // GPS HDOP (lower is better: <1=ideal, 1-2=excellent, 2-5=good, 5-10=moderate, >10=poor)
    if (code.indexOf("gps_hdop") >= 0 || code.indexOf("hdop") >= 0) {
        // Simulate HDOP with mostly good values
        static double lastHdop = 1.5;
        static unsigned long lastHdopUpdate = 0;
        unsigned long now = millis();

        if (now - lastHdopUpdate > 5000) {
            lastHdopUpdate = now;
            // Random HDOP between 0.5 and 5.0 (mostly good)
            lastHdop = 0.5 + (random(450) / 100.0);  // 0.5 to 5.0
        }
        return lastHdop;
    }

    // GPS latitude/longitude/altitude/speed (simulate with fixed location + drift)
    if (code.indexOf("latitude") >= 0 || code.indexOf("lat") >= 0) {
        // Simulate latitude (e.g., Berlin area: ~52.52)
        return 52.52 + (random(-100, 100) / 10000.0);  // Small drift
    }
    if (code.indexOf("longitude") >= 0 || code.indexOf("lng") >= 0 || code.indexOf("lon") >= 0) {
        // Simulate longitude (e.g., Berlin area: ~13.40)
        return 13.40 + (random(-100, 100) / 10000.0);
    }
    if (code.indexOf("altitude") >= 0 || code.indexOf("alt") >= 0) {
        // Simulate altitude (e.g., ~34m for Berlin)
        return 34.0 + (random(-50, 50) / 10.0);
    }
    if (code.indexOf("speed") >= 0) {
        // Simulate speed (0-5 km/h for stationary with drift)
        return random(0, 50) / 10.0;
    }

    // Default: Use temperature as fallback
    return sensorSimulator.getTemperature();
}

/**
 * Set simulation profile from string
 */
void setSimulationProfile(const String& profileName) {
    String name = profileName;
    name.toLowerCase();

    if (name == "winter") {
        sensorSimulator.setProfile(SimulationProfile::WINTER);
    } else if (name == "summer") {
        sensorSimulator.setProfile(SimulationProfile::SUMMER);
    } else if (name == "storm") {
        sensorSimulator.setProfile(SimulationProfile::STORM);
    } else if (name == "stress") {
        sensorSimulator.setProfile(SimulationProfile::STRESS);
    } else {
        sensorSimulator.setProfile(SimulationProfile::NORMAL);
    }
}

/**
 * Get current simulation profile name
 */
const char* getCurrentProfileName() {
    return SensorSimulator::getProfileName(sensorSimulator.getProfile());
}

/**
 * Read sensor value - either from hardware or simulation based on Hub config
 * @param sensorCode Sensor code/measurement type
 * @param unit Unit of measurement
 * @param sensorConfig Sensor configuration from Hub (optional, for hardware reading)
 * @return Sensor reading value
 */
double readSensorValueWithConfig(const String& sensorCode, const String& unit, const SensorAssignmentConfig* sensorConfig) {
    // Use isSimulation flag from Hub configuration (not local auto-detect!)
    if (currentConfig.isSimulation) {
        // Hub says to simulate - use simulated values
        return generateSimulatedValue(sensorCode, unit);
    }

#ifdef PLATFORM_ESP32
    // Hub says real hardware - try to read from actual sensors using SensorReader
    if (sensorConfig != nullptr) {
        // Use the sensor configuration to read hardware
        SensorReading reading = sensorReader.readValue(sensorCode, *sensorConfig);

        if (reading.success) {
            return reading.value;
        }

        // Hardware reading failed - log error and fall back to simulation with warning
        Serial.printf("[HW] Hardware read failed for %s: %s\n",
                      sensorCode.c_str(), reading.error.c_str());
        Serial.println("[HW] CRITICAL: isSimulation=false but hardware unavailable!");
        Serial.println("[HW] Check sensor wiring and configuration in Hub");

        // Return NaN or 0 to indicate error (don't silently simulate)
        // Actually, return a distinctive error value that Hub can recognize
        return -999.99;  // Error indicator value
    }

    // No sensor config provided - can't read hardware
    Serial.printf("[HW] No sensor config for %s - cannot read hardware\n", sensorCode.c_str());
    return -999.99;  // Error indicator value
#else
    // Native platform - no hardware available
    Serial.println("[HW] Native platform has no hardware sensors");
    return generateSimulatedValue(sensorCode, unit);
#endif
}

/**
 * Read sensor value - either from hardware or simulation based on Hub config
 * Legacy wrapper for backward compatibility
 * @param sensorCode Sensor code/measurement type
 * @param unit Unit of measurement
 * @return Sensor reading value
 */
double readSensorValue(const String& sensorCode, const String& unit) {
    // Use isSimulation flag from Hub configuration
    if (currentConfig.isSimulation) {
        return generateSimulatedValue(sensorCode, unit);
    }

    // When not simulating but no config, we can't read hardware
    // This should not happen in normal operation
    Serial.printf("[HW] readSensorValue called without config for %s\n", sensorCode.c_str());
    return -999.99;  // Error indicator
}

/**
 * Read and send only sensors that are DUE based on their individual intervals.
 * Uses GCD-based polling: loop runs at GCD interval, only reads sensors whose time has come.
 */
void readAndSendDueSensors(unsigned long now) {
    if (!apiClient.isConfigured()) {
        Serial.println("[Main] API client not configured - skipping sensor readings");
        return;
    }

    // IMPORTANT: Only send readings when we have valid configuration with sensors
    // The Hub assigns sensors to nodes - we don't send data without knowing what sensors we have
    if (!configLoaded) {
        Serial.println("[Main] No configuration loaded - skipping sensor readings");
        Serial.println("[Main] Waiting for Hub to assign sensors to this node...");
        return;
    }

    if (currentConfig.sensors.size() == 0) {
        Serial.println("[Main] No sensors assigned to this node - skipping readings");
        Serial.println("[Main] Please assign sensors to this node in the Hub UI");
        return;
    }

#ifndef PLATFORM_NATIVE
    // ESP32: Check WiFi connection
    if (!wifiManager.isConnected()) {
        Serial.println("[Main] WiFi not connected - skipping sensor readings");
        return;
    }
#endif

    // Log simulation mode from Hub (uses global simulationModeLogged flag)
    if (!simulationModeLogged) {
        if (currentConfig.isSimulation) {
            Serial.println("[Main] ========================================");
            Serial.println("[Main] SIMULATION MODE (from Hub configuration)");
            Serial.println("[Main] Node.isSimulation = true");
            Serial.println("[Main] ========================================");
        } else {
            Serial.println("[Main] ========================================");
            Serial.println("[Main] REAL HARDWARE MODE (from Hub configuration)");
            Serial.println("[Main] Node.isSimulation = false");
            Serial.println("[Main] ========================================");
        }
        simulationModeLogged = true;
    }

    // Count how many sensors are due this tick
    int dueCount = 0;
    for (const auto& sensor : currentConfig.sensors) {
        if (isSensorDue(sensor, now)) {
            dueCount++;
        }
    }

    if (dueCount == 0) {
        // No sensors due this tick - silently skip
        return;
    }

    Serial.printf("[Main] Polling tick: %d of %d sensors due\n",
                  dueCount, (int)currentConfig.sensors.size());

    // Read only sensors that are due
    for (const auto& sensor : currentConfig.sensors) {
        if (!sensor.isActive) {
            continue;  // Skip inactive sensors silently
        }

        if (!isSensorDue(sensor, now)) {
            continue;  // Not due yet - skip
        }

        // Mark sensor as read at current time
        markSensorRead(sensor.endpointId, now);

        // Initialize sensor if not simulating (first reading will init)
#ifdef PLATFORM_ESP32
        if (!currentConfig.isSimulation) {
            // Ensure sensor is initialized with Hub configuration
            sensorReader.initializeSensor(sensor);
        }
#endif

        // If sensor has capabilities, send one reading per capability
        if (sensor.capabilities.size() > 0) {
            for (const auto& cap : sensor.capabilities) {
                // Read value based on Hub's isSimulation flag, with sensor config
                double value = readSensorValueWithConfig(cap.measurementType, cap.unit, &sensor);

                // Check for error indicator
                if (value <= -999.0) {
                    Serial.printf("[Main] Skipping %s/%s - hardware read error\n",
                                  sensor.sensorName.c_str(), cap.measurementType.c_str());
                    continue;
                }

                // Apply calibration corrections
                value = (value + sensor.offsetCorrection) * sensor.gainCorrection;

                // Include endpointId to identify which sensor assignment this reading belongs to
                // Sprint OS-01: Check storage mode and WiFi availability
                bool sentToHub = false;
                bool storedLocally = false;

#ifdef PLATFORM_ESP32
                if (offlineStorageEnabled) {
                    StorageMode mode = storageConfigManager.getMode();
                    bool wifiAvailable = wifiManager.isConnected();

                    // Decide where to store/send based on mode
                    if (mode == StorageMode::LOCAL_ONLY) {
                        // Only store locally
                        storedLocally = readingStorage.storeReading(
                            cap.measurementType, value, cap.unit, sensor.endpointId);
                    } else if (mode == StorageMode::REMOTE_ONLY) {
                        // Only send to Hub (original behavior)
                        sentToHub = apiClient.sendReading(cap.measurementType, value, cap.unit, sensor.endpointId);
                    } else {
                        // LOCAL_AND_REMOTE or LOCAL_AUTOSYNC
                        // Store locally first
                        storedLocally = readingStorage.storeReading(
                            cap.measurementType, value, cap.unit, sensor.endpointId);

                        // Also send to Hub if WiFi available (for LOCAL_AND_REMOTE)
                        // For LOCAL_AUTOSYNC, sync manager handles the upload
                        if (mode == StorageMode::LOCAL_AND_REMOTE && wifiAvailable) {
                            sentToHub = apiClient.sendReading(cap.measurementType, value, cap.unit, sensor.endpointId);
                        }
                    }
                } else {
                    // No offline storage - send directly
                    sentToHub = apiClient.sendReading(cap.measurementType, value, cap.unit, sensor.endpointId);
                }
#else
                sentToHub = apiClient.sendReading(cap.measurementType, value, cap.unit, sensor.endpointId);
#endif

                // Log result with mode info
                if (sentToHub && storedLocally) {
                    Serial.printf("[Main] Sent+Stored %s/%s: %.2f %s (Endpoint %d) [LOCAL_AND_REMOTE]%s\n",
                                  sensor.sensorName.c_str(), cap.displayName.c_str(),
                                  value, cap.unit.c_str(), sensor.endpointId,
                                  currentConfig.isSimulation ? " [SIM]" : " [HW]");
                } else if (sentToHub) {
                    Serial.printf("[Main] Sent %s/%s: %.2f %s (Endpoint %d) [REMOTE]%s\n",
                                  sensor.sensorName.c_str(), cap.displayName.c_str(),
                                  value, cap.unit.c_str(), sensor.endpointId,
                                  currentConfig.isSimulation ? " [SIM]" : " [HW]");
                } else if (storedLocally) {
                    Serial.printf("[Main] Stored %s/%s: %.2f %s (Endpoint %d) [LOCAL]%s\n",
                                  sensor.sensorName.c_str(), cap.displayName.c_str(),
                                  value, cap.unit.c_str(), sensor.endpointId,
                                  currentConfig.isSimulation ? " [SIM]" : " [HW]");
                } else {
                    Serial.printf("[Main] Failed to send/store %s/%s reading\n",
                                  sensor.sensorName.c_str(), cap.measurementType.c_str());
                }
            }
        } else {
            // Fallback: Send single reading with sensor code as measurement type
            double value = readSensorValueWithConfig(sensor.sensorCode, "", &sensor);

            // Check for error indicator
            if (value <= -999.0) {
                Serial.printf("[Main] Skipping %s - hardware read error\n",
                              sensor.sensorName.c_str());
                continue;
            }

            // Apply calibration corrections
            value = (value + sensor.offsetCorrection) * sensor.gainCorrection;

            // Include endpointId to identify which sensor assignment this reading belongs to
            // Sprint OS-01: Check storage mode and WiFi availability
            bool sentToHub = false;
            bool storedLocally = false;

#ifdef PLATFORM_ESP32
            if (offlineStorageEnabled) {
                StorageMode mode = storageConfigManager.getMode();
                bool wifiAvailable = wifiManager.isConnected();

                if (mode == StorageMode::LOCAL_ONLY) {
                    storedLocally = readingStorage.storeReading(
                        sensor.sensorCode, value, "", sensor.endpointId);
                } else if (mode == StorageMode::REMOTE_ONLY) {
                    sentToHub = apiClient.sendReading(sensor.sensorCode, value, "", sensor.endpointId);
                } else {
                    storedLocally = readingStorage.storeReading(
                        sensor.sensorCode, value, "", sensor.endpointId);
                    if (mode == StorageMode::LOCAL_AND_REMOTE && wifiAvailable) {
                        sentToHub = apiClient.sendReading(sensor.sensorCode, value, "", sensor.endpointId);
                    }
                }
            } else {
                sentToHub = apiClient.sendReading(sensor.sensorCode, value, "", sensor.endpointId);
            }
#else
            sentToHub = apiClient.sendReading(sensor.sensorCode, value, "", sensor.endpointId);
#endif

            if (sentToHub && storedLocally) {
                Serial.printf("[Main] Sent+Stored %s: %.2f (Endpoint %d) [LOCAL_AND_REMOTE]%s\n",
                              sensor.sensorName.c_str(), value, sensor.endpointId,
                              currentConfig.isSimulation ? " [SIM]" : " [HW]");
            } else if (sentToHub) {
                Serial.printf("[Main] Sent %s: %.2f (Endpoint %d) [REMOTE]%s\n",
                              sensor.sensorName.c_str(), value, sensor.endpointId,
                              currentConfig.isSimulation ? " [SIM]" : " [HW]");
            } else if (storedLocally) {
                Serial.printf("[Main] Stored %s: %.2f (Endpoint %d) [LOCAL]%s\n",
                              sensor.sensorName.c_str(), value, sensor.endpointId,
                              currentConfig.isSimulation ? " [SIM]" : " [HW]");
            } else {
                Serial.printf("[Main] Failed to send/store %s reading\n", sensor.sensorName.c_str());
            }
        }
    }
    // Note: No fallback - we only send readings when we have proper configuration
    // The Hub assigns sensors to nodes, so we wait for that configuration
}

/**
 * Send hardware status report to Hub (Sprint 8)
 * Collects detected devices, SD card status, and bus status
 */
void sendHardwareStatusReport() {
    Serial.println("[Main] Preparing hardware status report...");

    // Build detected devices JSON array
    String devicesJson = "[";
    bool firstDevice = true;

#ifdef PLATFORM_ESP32
    // Scan hardware to get detected devices
    auto devices = hardwareScanner.getLastScanResults();

    for (const auto& device : devices) {
        if (!firstDevice) devicesJson += ",";
        firstDevice = false;

        devicesJson += "{";
        devicesJson += "\"deviceType\":\"" + device.deviceName + "\",";
        devicesJson += "\"bus\":\"" + device.bus + "\",";

        // Format address based on bus type
        if (device.bus == "I2C") {
            char addrStr[8];
            snprintf(addrStr, sizeof(addrStr), "0x%02X", device.address);
            devicesJson += "\"address\":\"" + String(addrStr) + "\",";
        } else if (device.bus == "1-Wire" || device.bus == "Analog") {
            devicesJson += "\"address\":\"Pin " + String(device.pin) + "\",";
        } else if (device.bus == "UART") {
            devicesJson += "\"address\":\"RX:" + String(device.rxPin) + "/TX:" + String(device.txPin) + "\",";
        } else {
            devicesJson += "\"address\":\"unknown\",";
        }

        devicesJson += "\"status\":\"OK\",";
        devicesJson += "\"sensorCode\":\"" + device.sensorType + "\"";
        devicesJson += "}";
    }
#endif
    devicesJson += "]";

    // Build storage status JSON
    String storageJson = "{";
#ifdef PLATFORM_ESP32
    if (offlineStorageEnabled) {
        storageJson += "\"available\":true,";
        storageJson += "\"mode\":\"" + String(StorageConfig::getModeString(storageConfigManager.getMode())) + "\",";
        storageJson += "\"totalBytes\":" + String(sdManager.getTotalBytes()) + ",";
        storageJson += "\"usedBytes\":" + String(sdManager.getUsedBytes()) + ",";
        storageJson += "\"freeBytes\":" + String(sdManager.getFreeBytes()) + ",";
        storageJson += "\"pendingSyncCount\":" + String(readingStorage.getPendingCount()) + ",";
        storageJson += "\"lastSyncAt\":null,";
        storageJson += "\"lastSyncError\":null";
    } else {
        storageJson += "\"available\":false,";
        storageJson += "\"mode\":\"REMOTE_ONLY\",";
        storageJson += "\"totalBytes\":0,";
        storageJson += "\"usedBytes\":0,";
        storageJson += "\"freeBytes\":0,";
        storageJson += "\"pendingSyncCount\":0,";
        storageJson += "\"lastSyncAt\":null,";
        storageJson += "\"lastSyncError\":null";
    }
#else
    storageJson += "\"available\":false,";
    storageJson += "\"mode\":\"REMOTE_ONLY\",";
    storageJson += "\"totalBytes\":0,";
    storageJson += "\"usedBytes\":0,";
    storageJson += "\"freeBytes\":0,";
    storageJson += "\"pendingSyncCount\":0,";
    storageJson += "\"lastSyncAt\":null,";
    storageJson += "\"lastSyncError\":null";
#endif
    storageJson += "}";

    // Build bus status JSON
    String busStatusJson = "{";
#ifdef PLATFORM_ESP32
    // Check I2C availability
    bool i2cAvailable = true;  // We initialized I2C in setup
    auto devices2 = hardwareScanner.getLastScanResults();

    int i2cCount = 0;
    String i2cAddresses = "[";
    bool firstAddr = true;
    for (const auto& device : devices2) {
        if (device.bus == "I2C") {
            i2cCount++;
            if (!firstAddr) i2cAddresses += ",";
            firstAddr = false;
            char addrStr[8];
            snprintf(addrStr, sizeof(addrStr), "\"0x%02X\"", device.address);
            i2cAddresses += addrStr;
        }
    }
    i2cAddresses += "]";

    int oneWireCount = 0;
    bool uartAvailable = false;
    bool gpsDetected = false;
    for (const auto& device : devices2) {
        if (device.bus == "1-Wire") oneWireCount++;
        if (device.bus == "UART") {
            uartAvailable = true;
            if (device.sensorType.indexOf("gps") >= 0 || device.sensorType.indexOf("neo") >= 0) {
                gpsDetected = true;
            }
        }
    }

    busStatusJson += "\"i2cAvailable\":" + String(i2cAvailable ? "true" : "false") + ",";
    busStatusJson += "\"i2cDeviceCount\":" + String(i2cCount) + ",";
    busStatusJson += "\"i2cAddresses\":" + i2cAddresses + ",";
    busStatusJson += "\"oneWireAvailable\":" + String(oneWireCount > 0 ? "true" : "false") + ",";
    busStatusJson += "\"oneWireDeviceCount\":" + String(oneWireCount) + ",";
    busStatusJson += "\"uartAvailable\":" + String(uartAvailable ? "true" : "false") + ",";
    busStatusJson += "\"gpsDetected\":" + String(gpsDetected ? "true" : "false");
#else
    busStatusJson += "\"i2cAvailable\":false,";
    busStatusJson += "\"i2cDeviceCount\":0,";
    busStatusJson += "\"i2cAddresses\":[],";
    busStatusJson += "\"oneWireAvailable\":false,";
    busStatusJson += "\"oneWireDeviceCount\":0,";
    busStatusJson += "\"uartAvailable\":false,";
    busStatusJson += "\"gpsDetected\":false";
#endif
    busStatusJson += "}";

    // Send the hardware status report
    bool success = apiClient.sendHardwareStatus(
        currentSerial,
        FIRMWARE_VERSION,
        HARDWARE_TYPE,
        devicesJson,
        storageJson,
        busStatusJson
    );

    if (success) {
        Serial.println("[Main] Hardware status report sent successfully");
    } else {
        Serial.println("[Main] Failed to send hardware status report");
    }
}

bool registerWithHub() {
    if (apiClient.getBaseUrl().length() == 0) {
        Serial.println("[Main] Base URL not set for registration");
        return false;
    }

    Serial.println("[Main] Registering with Hub...");

    // Get serial number
#ifdef PLATFORM_NATIVE
    String serial = hal::get_device_serial().c_str();
#else
    String serial = configManager.getSerial();
#endif

    // Store serial for later configuration fetches
    currentSerial = serial;

    // No capabilities sent - Hub assigns sensors to nodes
    std::vector<String> emptyCapabilities;

    RegistrationResponse response = apiClient.registerNode(
        serial,
        FIRMWARE_VERSION,
        HARDWARE_TYPE,
        emptyCapabilities
    );

    // If HTTPS fails, try HTTP fallback on port 5000
    if (!response.success) {
        String currentUrl = apiClient.getBaseUrl();
        if (currentUrl.startsWith("https://")) {
            // Extract host from URL and try HTTP on port 5000
            int hostStart = 8; // after "https://"
            int hostEnd = currentUrl.indexOf(':', hostStart);
            if (hostEnd < 0) hostEnd = currentUrl.indexOf('/', hostStart);
            if (hostEnd < 0) hostEnd = currentUrl.length();
            String host = currentUrl.substring(hostStart, hostEnd);
            String httpUrl = "http://" + host + ":5002";

            Serial.printf("[Main] HTTPS failed, trying HTTP fallback: %s\n", httpUrl.c_str());
            apiClient.configure(httpUrl, "", "");

            response = apiClient.registerNode(
                serial,
                FIRMWARE_VERSION,
                HARDWARE_TYPE,
                emptyCapabilities
            );
        }
    }

    if (response.success) {
        Serial.printf("[Main] Registered as: %s\n", response.name.c_str());
        Serial.printf("[Main]   Node ID: %s\n", response.nodeId.c_str());
        Serial.printf("[Main]   Interval: %d seconds\n", response.intervalSeconds);
        Serial.printf("[Main]   New Node: %s\n", response.isNewNode ? "yes" : "no");

        // IMPORTANT: Update apiClient with the correct nodeId from registration response
        // This is especially important after HTTP fallback where we used empty nodeId
        String currentUrl = apiClient.getBaseUrl();
        apiClient.configure(currentUrl, response.nodeId, "");
        Serial.printf("[Main] API client configured with nodeId: %s\n", response.nodeId.c_str());

        // Update currentSerial to match the registered nodeId
        currentSerial = serial;

        // Immediately fetch configuration after successful registration
        Serial.println("[Main] Fetching initial sensor configuration...");
        fetchSensorConfiguration();

        // Sprint 8: Send hardware status report after successful registration
        Serial.println("[Main] Sending hardware status report...");
        sendHardwareStatusReport();

        // Sprint 8: Fetch and apply debug configuration from Hub
        // Note: Use nodeId (GUID) not serial number for the debug config endpoint
        Serial.println("[Main] Fetching debug configuration from Hub...");
        DebugConfigurationResponse debugConfig = apiClient.fetchDebugConfiguration(response.nodeId);
        if (debugConfig.success) {
            // Apply debug level to DebugManager
            DebugLevel newLevel = static_cast<DebugLevel>(debugConfig.debugLevel);
            DebugManager::getInstance().setLevel(newLevel);
            DebugManager::getInstance().setRemoteLogging(debugConfig.enableRemoteLogging);

            // Configure remote serial monitor (captures ALL Serial output)
            // Note: Backend looks up by NodeId (GUID) or MacAddress, so we must send the nodeId GUID
            if (debugConfig.enableRemoteLogging) {
                DebugLogUploader::getInstance().begin(apiClient.getBaseUrl(), response.nodeId);
                DebugLogUploader::getInstance().setEnabled(true);
            }

            Serial.printf("[Main] Debug config applied - Level: %s, Remote: %s\n",
                          DebugManager::getInstance().getLevelString(),
                          debugConfig.enableRemoteLogging ? "enabled" : "disabled");
        } else {
            Serial.println("[Main] Debug config fetch failed - using default settings");
        }

        return true;
    } else {
        Serial.printf("[Main] Registration failed: %s\n", response.error.c_str());
        return false;
    }
}

bool validateApiKeyWithHub() {
    if (!apiClient.isConfigured()) {
        return false;
    }

    Serial.println("[Main] Validating API key with Hub...");
    if (apiClient.validateApiKey()) {
        Serial.println("[Main] API key valid!");
        return true;
    } else {
        Serial.println("[Main] API key invalid or Hub unreachable!");
        return false;
    }
}

// ============================================================================
// State Handlers
// ============================================================================

/**
 * Attempt Hub Discovery via UDP broadcast.
 * Returns true if Hub was discovered and configuration is ready.
 */
bool attemptHubDiscovery() {
    Serial.println("[Main] Attempting Hub Discovery via UDP broadcast...");

    // Configure discovery client
    int discoveryPort = config::DISCOVERY_PORT;

#ifdef PLATFORM_NATIVE
    const char* portEnv = std::getenv(config::ENV_DISCOVERY_PORT);
    if (portEnv) {
        discoveryPort = atoi(portEnv);
    }
    // Get sensor serial from HAL (native only)
    String serial = hal::get_device_serial().c_str();
#else
    // ESP32: Generate serial from full 6-byte WiFi MAC address
    String serial = "ESP32-UNKNOWN";
#ifdef PLATFORM_ESP32
    uint8_t mac[6];
    WiFi.macAddress(mac);
    char serialBuf[24];
    // Use full 6-byte WiFi MAC address for unique sensor ID
    snprintf(serialBuf, sizeof(serialBuf), "ESP32-%02X%02X%02X%02X%02X%02X",
             mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);
    serial = String(serialBuf);
#endif
#endif

    discoveryClient.configure(discoveryPort, config::DISCOVERY_TIMEOUT_MS);

    // Attempt discovery with retries
    for (int attempt = 1; attempt <= config::DISCOVERY_RETRY_COUNT; attempt++) {
        Serial.printf("[Main] Discovery attempt %d/%d...\n", attempt, config::DISCOVERY_RETRY_COUNT);

        DiscoveryResponse response = discoveryClient.discover(
            serial,
            FIRMWARE_VERSION,
            HARDWARE_TYPE
        );

        if (response.success) {
            Serial.println("[Main] Hub discovered!");
            Serial.printf("[Main]   Hub ID: %s\n", response.hubId.c_str());
            Serial.printf("[Main]   Hub Name: %s\n", response.hubName.c_str());
            Serial.printf("[Main]   API URL: %s\n", response.apiUrl.c_str());

            // Save discovered Hub URL to stored config
            StoredConfig storedConfig = configManager.loadConfig();
            storedConfig.hubApiUrl = response.apiUrl;
            storedConfig.nodeId = serial;
            if (configManager.saveConfig(storedConfig)) {
                Serial.println("[Main] Hub URL saved to NVS");
            } else {
                Serial.println("[Main] Warning: Failed to save Hub URL to NVS");
            }

            // Configure API client with discovered URL
            // Use serial as nodeId and empty apiKey for now (will register)
            apiClient.configure(response.apiUrl, serial, "");

            return true;
        }

        Serial.printf("[Main] Discovery failed: %s\n", response.errorMessage.c_str());

        if (attempt < config::DISCOVERY_RETRY_COUNT) {
            Serial.printf("[Main] Retrying in %d ms...\n", config::DISCOVERY_RETRY_DELAY_MS);
            delay(config::DISCOVERY_RETRY_DELAY_MS);
        }
    }

    Serial.println("[Main] Hub Discovery failed after all attempts");
    return false;
}

void handleUnconfiguredState() {
#ifdef PLATFORM_ESP32
    // Start BLE pairing service
    if (!bleService.isAdvertising()) {
        Serial.println("[Main] Starting BLE pairing service...");
        bleService.setConfigCallback(onBLEConfigReceived);

        // Generate unique device name from MAC address
        uint8_t mac[6];
        esp_efuse_mac_get_default(mac);
        char deviceName[32];
        snprintf(deviceName, sizeof(deviceName), "myIoTGrid-%02X%02X",
                 mac[4], mac[5]);

        bleService.init(deviceName);
        bleService.setFirmwareVersion(FIRMWARE_VERSION);  // Set registration data with node_id
        bleService.startAdvertising();
        stateMachine.processEvent(StateEvent::BLE_PAIR_START);
    }
#else
    // Native/simulation mode - try Hub Discovery first
    static bool discoveryAttempted = false;

    // Check discovery configuration
    const char* discoveryEnabled = std::getenv(config::ENV_DISCOVERY_ENABLED);
    bool tryDiscovery = true;

    // Explicitly disabled?
    if (discoveryEnabled && strcmp(discoveryEnabled, "false") == 0) {
        tryDiscovery = false;
    }

    // Try Discovery first if enabled
    if (tryDiscovery && !discoveryAttempted) {
        discoveryAttempted = true;
        Serial.println("[Main] Attempting Hub Discovery via UDP broadcast...");

        if (attemptHubDiscovery()) {
            // Discovery succeeded - transition to configured state
            Serial.println("[Main] Hub discovered successfully!");
            stateMachine.processEvent(StateEvent::CONFIG_FOUND);
            return;
        }

        Serial.println("[Main] Discovery failed, checking for fallback configuration...");
    }

    // Fallback: Check if hub host is set via environment variable
    const char* hubHost = std::getenv(config::ENV_HUB_HOST);
    if (hubHost && strlen(hubHost) > 0) {
        // Manual configuration via environment variables
        Serial.println("[Main] Using fallback configuration from environment variables");

        const char* hubPort = std::getenv(config::ENV_HUB_PORT);
        const char* hubProtocol = std::getenv(config::ENV_HUB_PROTOCOL);

        String protocol = hubProtocol ? String(hubProtocol) : "https";
        int port = hubPort ? atoi(hubPort) : config::DEFAULT_HUB_PORT;

        String apiUrl = protocol + "://" + String(hubHost) + ":" + String(port);
        String serial = hal::get_device_serial().c_str();

        Serial.printf("[Main] API URL: %s\n", apiUrl.c_str());
        Serial.printf("[Main] Serial: %s\n", serial.c_str());

        apiClient.configure(apiUrl, serial, "");

        // Transition to configured state
        stateMachine.processEvent(StateEvent::CONFIG_FOUND);
        return;
    }

    // No discovery and no fallback - wait and retry
    if (!tryDiscovery) {
        Serial.println("[Main] Discovery disabled and no HUB_HOST set - please configure");
        delay(5000);
        return;
    }

    // Discovery failed and no fallback - wait and retry discovery
    Serial.println("[Main] Waiting before next discovery attempt...");
    delay(10000);
    discoveryAttempted = false;  // Allow retry
#endif
}

void handlePairingState() {
#ifdef PLATFORM_ESP32
    // Just wait for BLE callback - bleService handles everything
    bleService.loop();

    // Check for WiFi-only timeout (if WiFi received but no API config after 5 seconds)
    static unsigned long wifiReceivedTime = 0;
    static const unsigned long WIFI_ONLY_TIMEOUT_MS = 5000;  // 5 seconds to wait for API config

    if (bleService.hasWifiPending()) {
        if (wifiReceivedTime == 0) {
            wifiReceivedTime = millis();
            Serial.println("[Main] WiFi credentials received, waiting for API config...");
        } else if (millis() - wifiReceivedTime > WIFI_ONLY_TIMEOUT_MS) {
            Serial.println("[Main] Timeout waiting for API config - proceeding with WiFi-only mode");
            bleService.finalizeWifiOnlyConfig();
            wifiReceivedTime = 0;
        }
    } else {
        wifiReceivedTime = 0;  // Reset when no longer pending
    }
#endif
}

void handleConfiguredState() {
    static bool nodeRegistered = false;
    static unsigned long lastWiFiAttempt = 0;
    static bool waitingForRetry = false;

#ifdef PLATFORM_NATIVE
    // Native mode: Already "connected" via network, register with Hub
    if (!nodeRegistered) {
        if (apiClient.getBaseUrl().length() > 0) {
            if (registerWithHub()) {
                nodeRegistered = true;
                stateMachine.processEvent(StateEvent::API_VALIDATED);
            } else {
                // Registration failed - go to error state
                stateMachine.processEvent(StateEvent::API_FAILED);
            }
        } else {
            Serial.println("[Main] API base URL not set!");
            stateMachine.processEvent(StateEvent::ERROR_OCCURRED);
        }
    }
#else
    // ESP32 mode: Need WiFi connection
    static bool wifiConnecting = false;
    static bool apiConfigured = false;
    static bool discoveryAttempted = false;

    // Check if we're in RE_PAIRING state (max retries reached)
    if (stateMachine.getState() != NodeState::CONFIGURED) {
        return;
    }

    // If waiting for retry after failed attempt, check timer
    if (waitingForRetry) {
        int retryDelay = stateMachine.getRetryDelay();
        if (millis() - lastWiFiAttempt < (unsigned long)retryDelay) {
            // Still waiting
            return;
        }
        // Retry time reached
        waitingForRetry = false;
        wifiConnecting = false;
        Serial.println("[Main] Retry timer expired, attempting WiFi reconnect...");
    }

    // If WiFi not connected, try to connect
    if (!wifiManager.isConnected() && !wifiConnecting) {
        StoredConfig config = configManager.loadConfig();
        if (config.isValid) {
            int retryCount = stateMachine.getRetryCount();
            int maxRetries = stateMachine.getMaxRetries();
            Serial.printf("[Main] WiFi connecting (attempt %d/%d): %s\n",
                         retryCount + 1, maxRetries, config.wifiSsid.c_str());
            wifiManager.connect(config.wifiSsid, config.wifiPassword);
            wifiConnecting = true;
            lastWiFiAttempt = millis();
        } else {
            Serial.println("[Main] Invalid stored configuration!");
            stateMachine.processEvent(StateEvent::ERROR_OCCURRED);
        }
    }

    // Run WiFi manager loop
    wifiManager.loop();

    // Check if WiFi connection failed (callback already sent WIFI_FAILED event)
    // If still in CONFIGURED state after WIFI_FAILED, start retry timer
    if (wifiConnecting && !wifiManager.isConnected() &&
        wifiManager.getStatus() == WiFiStatus::FAILED) {
        wifiConnecting = false;

        // Check if we should retry or if max retries reached (would be in RE_PAIRING)
        if (stateMachine.getState() == NodeState::CONFIGURED) {
            int retryDelay = stateMachine.getRetryDelay();
            Serial.printf("[Main] WiFi failed, waiting %d ms before retry...\n", retryDelay);
            waitingForRetry = true;
            lastWiFiAttempt = millis();
            return;
        }
    }

    // If WiFi connected but API not configured yet
    if (wifiManager.isConnected() && !apiConfigured) {
        wifiConnecting = false;
        StoredConfig config = configManager.loadConfig();

        // Check if we have a Hub/Cloud URL from BLE config (direct connection mode)
        if (config.hubApiUrl.length() > 0) {
            // Hub/Cloud URL is available - SKIP UDP discovery
            Serial.println("[Main] ========================================");
            if (config.isCloudMode()) {
                Serial.println("[Main] CLOUD CONNECTION MODE");
                Serial.println("[Main] Using fixed cloud endpoint - no UDP discovery needed");
                Serial.printf("[Main] Cloud URL: %s\n", config.hubApiUrl.c_str());
                Serial.printf("[Main] TenantID: %s\n", config.tenantId.c_str());
            } else {
                Serial.println("[Main] DIRECT HUB CONNECTION MODE");
                Serial.println("[Main] Hub URL received from BLE config - skipping UDP discovery");
                Serial.printf("[Main] Hub URL: %s\n", config.hubApiUrl.c_str());
            }
            Serial.println("[Main] ========================================");
            apiClient.configure(ensureUrlHasPort(config.hubApiUrl), config.nodeId, config.apiKey);
            apiConfigured = true;
        } else if (!discoveryAttempted) {
            // No Hub URL - need to discover Hub via UDP broadcast (fallback mode)
            Serial.println("[Main] No Hub URL in config - starting UDP Hub Discovery...");
            discoveryAttempted = true;

            if (attemptHubDiscovery()) {
                // Discovery succeeded - reload config with new Hub URL
                config = configManager.loadConfig();
                Serial.printf("[Main] Hub discovered via UDP! URL: %s\n", config.hubApiUrl.c_str());
                apiClient.configure(ensureUrlHasPort(config.hubApiUrl), config.nodeId, config.apiKey);
                apiConfigured = true;
            } else {
                Serial.println("[Main] Hub Discovery failed - will retry later");
                // Reset to allow retry after delay
                static unsigned long lastDiscoveryAttempt = 0;
                if (millis() - lastDiscoveryAttempt > 10000) {
                    lastDiscoveryAttempt = millis();
                    discoveryAttempted = false;
                }
                return;
            }
        } else {
            Serial.println("[Main] Still no Hub URL - waiting for discovery retry...");
            return;
        }
    }

    // If WiFi connected and API configured, register with Hub
    if (wifiManager.isConnected() && apiConfigured && !nodeRegistered) {
        Serial.println("[Main] Registering with Hub...");

        if (registerWithHub()) {
            nodeRegistered = true;
            stateMachine.processEvent(StateEvent::API_VALIDATED);
        } else {
            // Registration failed - go to error state
            stateMachine.processEvent(StateEvent::API_FAILED);
        }
    }
#endif
}

void handleOperationalState() {
    unsigned long now = millis();

#ifndef PLATFORM_NATIVE
    // ESP32: Check WiFi connection periodically
    if (now - lastWiFiCheck >= WIFI_CHECK_INTERVAL_MS) {
        lastWiFiCheck = now;
        wifiManager.loop();

        if (!wifiManager.isConnected()) {
            Serial.println("[Main] WiFi lost in operational mode");
            stateMachine.processEvent(StateEvent::WIFI_FAILED);
            return;
        }
    }
#endif

    // Check for configuration updates periodically
    if (now - lastConfigCheck >= CONFIG_CHECK_INTERVAL_MS) {
        lastConfigCheck = now;
        fetchSensorConfiguration();
    }

    // Check for debug configuration updates periodically (Sprint 8)
    if (now - lastDebugConfigCheck >= DEBUG_CONFIG_CHECK_INTERVAL_MS) {
        lastDebugConfigCheck = now;
        checkDebugConfiguration();
    }

    // Send heartbeat periodically
    if (now - lastHeartbeat >= HEARTBEAT_INTERVAL_MS) {
        lastHeartbeat = now;
        sendHeartbeat();
    }

    // Read and send sensor data using GCD-based polling
    // Poll loop runs at GCD interval, but only reads sensors that are due
    unsigned long sensorInterval = calculatedPollIntervalSeconds * 1000UL;
    if (sensorInterval == 0) {
        sensorInterval = SENSOR_INTERVAL_MS;
    }

    if (now - lastSensorReading >= sensorInterval) {
        lastSensorReading = now;
        readAndSendDueSensors(now);
    }
}

void handleErrorState() {
    int retryCount = stateMachine.getRetryCount();
    int maxRetries = stateMachine.getMaxRetries();
    int retryDelay = stateMachine.getRetryDelay();

    Serial.println("[Main] ========================================");
    Serial.printf("[Main] ERROR STATE - Retry %d/%d\n", retryCount + 1, maxRetries);
    Serial.printf("[Main] Next retry delay: %d ms\n", retryDelay);
    Serial.println("[Main] ========================================");

    // Check if we have valid config
    if (configManager.hasConfig()) {
        Serial.printf("[Main] Config exists, waiting %d ms before retry...\n", retryDelay);
        delay(retryDelay);
        stateMachine.processEvent(StateEvent::RETRY_TIMEOUT);
    } else {
        Serial.println("[Main] No config, need BLE pairing...");
        delay(5000);
        // Clear any partial config and restart pairing
        configManager.clearConfig();
        stateMachine.processEvent(StateEvent::RESET_REQUESTED);
    }
}

/**
 * Handle RE_PAIRING state (Story 4: Paralleler WiFi-Retry)
 *
 * In this state:
 * - BLE is advertising with "-SETUP" suffix (so frontend can detect RE_PAIRING)
 * - WiFi retry happens every 30 seconds with old credentials
 * - LED shows RE_PAIRING pattern (2x blink every 2s)
 *
 * Exit conditions:
 * - NEW_WIFI_RECEIVED: New WiFi credentials received via BLE
 * - OLD_WIFI_FOUND: Old WiFi came back online
 * - RESET_REQUESTED: Factory reset
 */
void handleRePairingState() {
#ifdef PLATFORM_ESP32
    // Initialize RE_PAIRING mode if not already active
    if (!rePairingActive) {
        Serial.println("[Main] ========================================");
        Serial.println("[Main] ENTERING RE_PAIRING STATE");
        Serial.println("[Main] ========================================");
        Serial.println("[Main] - BLE advertising with '-SETUP' suffix");
        Serial.println("[Main] - WiFi retry every 30 seconds");
        Serial.println("[Main] - LED: 2x blink pattern");

        // Start BLE in RE_PAIRING mode (adds -SETUP suffix)
        bleService.setConfigCallback(onBLEConfigReceived);
        bleService.setReProvisioningCallback(onReProvisioningConfigReceived);

        if (bleService.startForReProvisioning()) {
            Serial.println("[Main] BLE RE_PAIRING mode started");
        } else {
            Serial.println("[Main] Failed to start BLE RE_PAIRING mode!");
        }

        // Set LED pattern for RE_PAIRING
        ledController.setPattern(LEDPattern::RE_PAIRING_BLINK);

        // Reset retry timer
        lastRePairingWifiRetry = millis();
        rePairingActive = true;
    }

    // Process BLE events
    bleService.loop();

    // Check for WiFi retry timer (Story 4: Parallel WiFi Retry)
    unsigned long now = millis();
    if (now - lastRePairingWifiRetry >= RE_PAIRING_WIFI_RETRY_INTERVAL_MS) {
        lastRePairingWifiRetry = now;

        // Signal timer event to state machine
        stateMachine.processEvent(StateEvent::WIFI_RETRY_TIMER);

        // Attempt to connect with stored (old) WiFi credentials
        StoredConfig config = configManager.loadConfig();
        if (config.isValid && config.wifiSsid.length() > 0) {
            Serial.println("[Main] RE_PAIRING: Attempting WiFi reconnect with old credentials...");
            Serial.printf("[Main] SSID: %s\n", config.wifiSsid.c_str());

            // Temporarily stop BLE advertising to avoid conflicts
            // (WiFi and BLE can interfere on some ESP32 boards)
            // Note: We don't stop BLE here to allow parallel operation

            // Try non-blocking WiFi connection
            WiFi.disconnect(true);
            WiFi.mode(WIFI_STA);
            WiFi.begin(config.wifiSsid.c_str(), config.wifiPassword.c_str());

            // Wait briefly for connection (non-blocking approach)
            // Full connection is handled in the next iteration via WiFi callbacks
            unsigned long wifiAttemptStart = millis();
            while (WiFi.status() != WL_CONNECTED &&
                   (millis() - wifiAttemptStart) < 5000) {
                delay(100);
            }

            if (WiFi.status() == WL_CONNECTED) {
                Serial.println("[Main] RE_PAIRING: Old WiFi RECONNECTED!");
                Serial.printf("[Main] IP: %s\n", WiFi.localIP().toString().c_str());

                // Stop BLE
                bleService.stop();
                rePairingActive = false;

                // Signal OLD_WIFI_FOUND event
                stateMachine.processEvent(StateEvent::OLD_WIFI_FOUND);
                return;
            } else {
                Serial.println("[Main] RE_PAIRING: WiFi retry failed, continuing BLE advertising...");
                // Disconnect to clean up
                WiFi.disconnect(true);
            }
        } else {
            Serial.println("[Main] RE_PAIRING: No valid WiFi config for retry");
        }
    }

    // Update LED pattern
    ledController.update();
#else
    // Native platform - just wait
    Serial.println("[Main] RE_PAIRING not fully supported on native platform");
    delay(5000);
#endif
}

// ============================================================================
// Arduino Setup & Loop
// ============================================================================

void setup() {
    Serial.begin(115200);
    delay(1000);

    Serial.println();
    Serial.println("========================================");
    Serial.println("  myIoTGrid Sensor - Multi-Mode Support");
    Serial.printf("  Firmware: %s\n", FIRMWARE_VERSION);
    Serial.println("========================================");
    Serial.println();

    // ============================================================================
    // Sprint 8: Initialize Remote Debug System (FIRST - so all subsequent logs are captured)
    // ============================================================================
    DebugManager::getInstance().begin();
    DBG_SYSTEM("Debug system initialized - Level: %s",
               DebugManager::getInstance().getLevelString());

    // Initialize HardwareValidator
    HardwareValidator::getInstance().begin();

    // Initialize LED controller (GPIO 2 = built-in LED on most ESP32)
    ledController.init(2, false);
    ledController.setPattern(LEDPattern::SLOW_BLINK);  // Initial pattern
    DBG_SYSTEM("LED controller initialized");

    // ============================================================================
    // Sprint OS-01: Initialize Offline Storage
    // ============================================================================
#ifdef PLATFORM_ESP32
    Serial.println("[Main] Initializing Offline Storage (Sprint OS-01)...");

    // Initialize SD Card
    if (sdManager.init(config::SD_MISO_PIN, config::SD_MOSI_PIN,
                       config::SD_SCK_PIN, config::SD_CS_PIN)) {
        Serial.println("[Main] SD Card initialized successfully");

        // Initialize Storage Configuration
        if (storageConfigManager.load(sdManager)) {
            Serial.println("[Main] Storage Configuration loaded");
            Serial.printf("[Main]   Mode: %s\n",
                          StorageConfig::getModeString(storageConfigManager.getMode()));

            // Initialize Reading Storage
            if (readingStorage.init(sdManager, storageConfigManager)) {
                Serial.println("[Main] Reading Storage initialized");

                // Initialize Sync Manager
                if (syncManager.init(readingStorage, storageConfigManager, apiClient, wifiManager)) {
                    Serial.println("[Main] Sync Manager initialized");

                    // Set up sync callbacks
                    syncManager.onSyncStart([]() {
                        Serial.println("[Sync] Sync started...");
                        syncStatusLED.setSyncing();
                    });

                    syncManager.onSyncComplete([](const SyncResult& result) {
                        if (result.success) {
                            Serial.printf("[Sync] Sync complete: %d synced\n", result.syncedCount);
                            if (syncManager.hasPendingReadings()) {
                                syncStatusLED.setPendingData();
                            } else {
                                syncStatusLED.setAllSynced();
                            }
                        }
                    });

                    syncManager.onSyncError([](const String& error) {
                        Serial.printf("[Sync] Error: %s\n", error.c_str());
                        syncStatusLED.setSyncError();
                    });

                    offlineStorageEnabled = true;
                }
            }
        }

        // Initialize Sync Status LED
        syncStatusLED.init(config::SYNC_LED_GPIO, true);
        syncStatusLED.setAllSynced();
        Serial.println("[Main] Sync Status LED initialized");

        // Initialize Sync Button
        syncButton.init(config::SYNC_BUTTON_GPIO, true);
        syncButton.onPress([](ButtonEvent event) {
            if (event == ButtonEvent::SHORT_PRESS) {
                Serial.println("[Main] Sync button: SHORT press - triggering sync");
                syncManager.triggerSync(false);
            } else if (event == ButtonEvent::LONG_PRESS) {
                Serial.println("[Main] Sync button: LONG press - force sync ALL");
                syncManager.triggerSync(true);
            }
        });
        syncButton.onHeld([](unsigned long heldMs) {
            // Visual feedback while button is held
            uint8_t progress = syncButton.getLongPressProgress();
            if (progress > 0 && progress < 100) {
                syncStatusLED.forceOn();
            }
        });
        Serial.println("[Main] Sync Button initialized");

    } else {
        Serial.println("[Main] SD Card initialization failed - offline storage disabled");
        offlineStorageEnabled = false;
    }

    // Always print storage status (visible regardless of debug level)
    Serial.println("[Main] ========================================");
    if (offlineStorageEnabled) {
        Serial.println("[Main] OFFLINE STORAGE: ENABLED (Sprint OS-01)");
        Serial.printf("[Main]   Storage Mode: %s\n", StorageConfig::getModeString(storageConfigManager.getMode()));
        Serial.printf("[Main]   Pending readings: %lu\n", readingStorage.getPendingCount());

        // Sprint 8: Initialize SD Logger for debug logs
        if (SDLogger::getInstance().begin(config::SD_CS_PIN)) {
            Serial.println("[Main]   SD Logger: initialized");
        } else {
            Serial.println("[Main]   SD Logger: FAILED");
        }
    } else {
        Serial.println("[Main] OFFLINE STORAGE: DISABLED");
        Serial.println("[Main]   Reason: SD Card init failed or not present");
    }
    Serial.println("[Main] ========================================");
#else
    Serial.println("[Main] ========================================");
    Serial.println("[Main] OFFLINE STORAGE: NOT AVAILABLE");
    Serial.println("[Main]   Platform: Native (no SD card support)");
    Serial.println("[Main] ========================================");
    offlineStorageEnabled = false;
#endif

    // Initialize configuration manager (NVS)
    if (!configManager.init()) {
        Serial.println("[Main] Failed to initialize NVS!");
    }

    // Setup WiFi callbacks
    wifiManager.onConnected(onWiFiConnected);
    wifiManager.onDisconnected(onWiFiDisconnected);
    wifiManager.onFailed(onWiFiFailed);

#ifdef PLATFORM_ESP32
    // Initialize Boot button for WPS
    pinMode(BOOT_BUTTON_PIN, INPUT_PULLUP);

    // Initialize WPS Manager
    wpsManager.init();
    wpsManager.onSuccess(onWPSSuccess);
    wpsManager.onFailed(onWPSFailed);
    wpsManager.onTimeout(onWPSTimeout);
    Serial.println("[Main] WPS Manager initialized (hold Boot button 3s to start)");

    // Initialize SensorReader for hardware sensor access
    sensorReader.init();
    Serial.println("[Main] SensorReader initialized");

    // Auto-detect hardware sensors (Story 6)
    autoDetectHardware();
#else
    // Native mode: Always use simulation
    sensorMode = SensorMode::SIMULATED;
    Serial.println("[Main] Native platform - using SIMULATED mode");
#endif

    // Initialize Sensor Simulator with default profile
    // Check environment variable for profile (native) or use NORMAL
#ifdef PLATFORM_NATIVE
    const char* profileEnv = std::getenv("SIMULATION_PROFILE");
    if (profileEnv) {
        setSimulationProfile(String(profileEnv));
        Serial.printf("[Simulator] Profile from env: %s\n", getCurrentProfileName());
    } else {
        sensorSimulator.init(SimulationProfile::NORMAL);
    }
#else
    sensorSimulator.init(SimulationProfile::NORMAL);
#endif
    Serial.printf("[Simulator] Active profile: %s\n", getCurrentProfileName());
    Serial.printf("[Simulator] Daily cycle: %s\n",
                  sensorSimulator.isDailyCycleEnabled() ? "enabled" : "disabled");

    // Print sensor mode
    const char* modeNames[] = {"AUTO", "REAL", "SIMULATED"};
    Serial.printf("[Main] Sensor mode: %s\n", modeNames[(int)sensorMode]);

    // Check for existing configuration
    if (configManager.hasConfig()) {
        Serial.println("[Main] Found stored configuration");
        StoredConfig config = configManager.loadConfig();
        if (config.isValid) {
            Serial.printf("[Main] NodeID: %s\n", config.nodeId.c_str());
            Serial.printf("[Main] Hub URL: %s\n", config.hubApiUrl.c_str());

            // We have config - go directly to CONFIGURED state
            stateMachine.processEvent(StateEvent::CONFIG_FOUND);
        } else {
            Serial.println("[Main] Stored config invalid - need pairing");
            stateMachine.processEvent(StateEvent::NO_CONFIG);
        }
    } else {
        Serial.println("[Main] No stored configuration - need pairing");
        stateMachine.processEvent(StateEvent::NO_CONFIG);
    }

    Serial.printf("[Main] Initial state: %s\n",
                  StateMachine::getStateName(stateMachine.getState()));
}

void loop() {
    NodeState currentState = stateMachine.getState();

#ifdef PLATFORM_ESP32
    // Check Boot button for WPS (in UNCONFIGURED/PAIRING) or Factory Reset (any state)
    // Factory Reset works in ALL states, WPS only in pairing states
    if (currentState == NodeState::UNCONFIGURED || currentState == NodeState::PAIRING) {
        checkBootButton();  // WPS + Factory Reset
    } else {
        // In other states, only check for Factory Reset (10 second hold)
        checkBootButtonForFactoryReset();
    }

    // Process WPS events if active
    if (wpsManager.isActive()) {
        wpsManager.loop();
    }
#endif

    // Update sensor simulator (generates smooth value transitions)
    static unsigned long lastSimulatorUpdate = 0;
    if (millis() - lastSimulatorUpdate >= 1000) {  // Update every second
        lastSimulatorUpdate = millis();
        sensorSimulator.update();
    }

    switch (currentState) {
        case NodeState::UNCONFIGURED:
            handleUnconfiguredState();
            break;

        case NodeState::PAIRING:
            handlePairingState();
            break;

        case NodeState::CONFIGURED:
            handleConfiguredState();
            break;

        case NodeState::OPERATIONAL:
            handleOperationalState();
            break;

        case NodeState::ERROR:
            handleErrorState();
            break;

        case NodeState::RE_PAIRING:
            handleRePairingState();
            break;
    }

    // Update LED controller (for all states)
    ledController.update();

    // ============================================================================
    // Sprint 8: Update Remote Debug System Components
    // ============================================================================
#ifdef PLATFORM_ESP32
    // Process SD logger queue
    if (offlineStorageEnabled) {
        SDLogger::getInstance().loop();
    }

    // Process debug log uploader
    DebugLogUploader::getInstance().loop();
#endif

    // ============================================================================
    // Sprint OS-01: Update Offline Storage Components
    // ============================================================================
#ifdef PLATFORM_ESP32
    if (offlineStorageEnabled) {
        // Update sync button (check for presses)
        syncButton.update();

        // Update sync status LED (blink patterns)
        syncStatusLED.update();

        // Run sync manager loop (handles auto-sync, retries)
        syncManager.loop();

        // Update LED based on sync state (if not syncing)
        if (syncManager.getState() == SyncState::IDLE) {
            if (!wifiManager.isConnected()) {
                syncStatusLED.setNoWifi();
            } else if (syncManager.hasPendingReadings()) {
                syncStatusLED.setPendingData();
            } else {
                syncStatusLED.setAllSynced();
            }
        }
    }
#endif

    // Small delay to prevent busy-looping
    delay(10);
}

// ============================================================================
// Native Platform Entry Point
// ============================================================================

#ifdef PLATFORM_NATIVE
int main() {
    setup();
    while (true) {
        loop();
    }
    return 0;
}
#endif
