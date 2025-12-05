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
#include "config.h"
#include "state_machine.h"
#include "config_manager.h"
#include "wifi_manager.h"
#include "api_client.h"
#include "discovery_client.h"
#include "sensor_simulator.h"

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

#ifdef PLATFORM_ESP32
BLEProvisioningService bleService;
WPSManager wpsManager;

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

static unsigned long lastHeartbeat = 0;
static unsigned long lastSensorReading = 0;
static unsigned long lastWiFiCheck = 0;
static unsigned long lastConfigCheck = 0;

// Current sensor configuration from Hub
static NodeConfigurationResponse currentConfig;
static bool configLoaded = false;
static String currentSerial;

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
    Wire.begin();
    Serial.println("[HW] Scanning I2C bus for sensors...");

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
 * Perform hardware auto-detection
 */
void autoDetectHardware() {
    Serial.println("[HW] Auto-detecting hardware sensors...");

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
    Serial.println("[Main] BLE configuration received!");
    Serial.printf("[Main] NodeID: %s\n", config.nodeId.c_str());
    Serial.printf("[Main] WiFi SSID: %s\n", config.wifiSsid.c_str());
    Serial.printf("[Main] Hub URL: %s\n", config.hubApiUrl.c_str());

    // Save configuration to NVS
    StoredConfig storedConfig;
    storedConfig.nodeId = config.nodeId;
    storedConfig.apiKey = config.apiKey;
    storedConfig.wifiSsid = config.wifiSsid;
    storedConfig.wifiPassword = config.wifiPassword;
    storedConfig.hubApiUrl = config.hubApiUrl;
    storedConfig.isValid = true;

    if (configManager.saveConfig(storedConfig)) {
        Serial.println("[Main] Configuration saved to NVS");

        // Stop BLE service
        bleService.stop();

        // Configure API client
        apiClient.configure(config.hubApiUrl, config.nodeId, config.apiKey);

        // Transition to CONFIGURED state (will attempt WiFi connection)
        stateMachine.processEvent(StateEvent::BLE_CONFIG_RECEIVED);
    } else {
        Serial.println("[Main] Failed to save configuration!");
        stateMachine.processEvent(StateEvent::ERROR_OCCURRED);
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

    NodeConfigurationResponse response = apiClient.fetchConfiguration(currentSerial);

    if (response.success) {
        currentConfig = response;
        configLoaded = true;

        Serial.printf("[Main] Configuration updated: %d sensors\n",
                      (int)currentConfig.sensors.size());

        if (currentConfig.isSimulation) {
            Serial.println("[Main] Node is in SIMULATION mode");
        }
    } else {
        Serial.printf("[Main] Config fetch: %s\n", response.error.c_str());
        // Don't clear configLoaded - keep using last known config
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

void readAndSendSensors() {
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

    // We have configuration with sensors, send readings
    if (configLoaded && currentConfig.sensors.size() > 0) {
        Serial.printf("[Main] Reading %d configured sensors...\n", (int)currentConfig.sensors.size());

        for (const auto& sensor : currentConfig.sensors) {
            if (!sensor.isActive) {
                Serial.printf("[Main] Skipping inactive sensor: %s\n", sensor.sensorName.c_str());
                continue;
            }

            // If sensor has capabilities, send one reading per capability
            if (sensor.capabilities.size() > 0) {
                for (const auto& cap : sensor.capabilities) {
                    // Generate simulated value based on measurement type and unit
                    double value = generateSimulatedValue(cap.measurementType, cap.unit);

                    // Apply calibration corrections
                    value = (value + sensor.offsetCorrection) * sensor.gainCorrection;

                    // Include endpointId to identify which sensor assignment this reading belongs to
                    if (apiClient.sendReading(cap.measurementType, value, cap.unit, sensor.endpointId)) {
                        Serial.printf("[Main] Sent %s/%s: %.2f %s (Endpoint %d)\n",
                                      sensor.sensorName.c_str(), cap.displayName.c_str(),
                                      value, cap.unit.c_str(), sensor.endpointId);
                    } else {
                        Serial.printf("[Main] Failed to send %s/%s reading\n",
                                      sensor.sensorName.c_str(), cap.measurementType.c_str());
                    }
                }
            } else {
                // Fallback: Send single reading with sensor code as measurement type
                double value = generateSimulatedValue(sensor.sensorCode, "");

                // Apply calibration corrections
                value = (value + sensor.offsetCorrection) * sensor.gainCorrection;

                // Include endpointId to identify which sensor assignment this reading belongs to
                if (apiClient.sendReading(sensor.sensorCode, value, "", sensor.endpointId)) {
                    Serial.printf("[Main] Sent %s: %.2f (Endpoint %d)\n",
                                  sensor.sensorName.c_str(), value, sensor.endpointId);
                } else {
                    Serial.printf("[Main] Failed to send %s reading\n", sensor.sensorName.c_str());
                }
            }
        }
    }
    // Note: No fallback - we only send readings when we have proper configuration
    // The Hub assigns sensors to nodes, so we wait for that configuration
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
    // ESP32: Generate serial from MAC address
    String serial = "ESP-UNKNOWN";
#ifdef PLATFORM_ESP32
    uint8_t mac[6];
    WiFi.macAddress(mac);
    char serialBuf[20];
    snprintf(serialBuf, sizeof(serialBuf), "ESP-%02X%02X%02X%02X",
             mac[2], mac[3], mac[4], mac[5]);
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
#endif
}

void handleConfiguredState() {
    static bool nodeRegistered = false;

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

    // If WiFi already connected (e.g., after WPS) but not yet configured API
    if (wifiManager.isConnected() && !apiConfigured) {
        StoredConfig config = configManager.loadConfig();
        if (config.isValid) {
            Serial.println("[Main] WiFi connected, configuring API client...");
            apiClient.configure(config.hubApiUrl, config.nodeId, config.apiKey);
            apiConfigured = true;
        }
    }

    // If WiFi not connected, try to connect
    if (!wifiManager.isConnected() && !wifiConnecting) {
        StoredConfig config = configManager.loadConfig();
        if (config.isValid) {
            Serial.printf("[Main] Connecting to WiFi: %s\n", config.wifiSsid.c_str());
            apiClient.configure(config.hubApiUrl, config.nodeId, config.apiKey);
            apiConfigured = true;
            wifiManager.connect(config.wifiSsid, config.wifiPassword);
            wifiConnecting = true;
        } else {
            Serial.println("[Main] Invalid stored configuration!");
            stateMachine.processEvent(StateEvent::ERROR_OCCURRED);
        }
    }

    // Run WiFi manager loop
    wifiManager.loop();

    // If WiFi connected and API configured, register with Hub
    if (wifiManager.isConnected() && apiConfigured && !nodeRegistered) {
        wifiConnecting = false;
        Serial.println("[Main] WiFi connected, registering with Hub...");

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

    // Send heartbeat periodically
    if (now - lastHeartbeat >= HEARTBEAT_INTERVAL_MS) {
        lastHeartbeat = now;
        sendHeartbeat();
    }

    // Read and send sensor data periodically
    // Use configured interval if available
    unsigned long sensorInterval = SENSOR_INTERVAL_MS;
    if (configLoaded && currentConfig.defaultIntervalSeconds > 0) {
        sensorInterval = currentConfig.defaultIntervalSeconds * 1000UL;
    }

    if (now - lastSensorReading >= sensorInterval) {
        lastSensorReading = now;
        readAndSendSensors();
    }
}

void handleErrorState() {
    Serial.println("[Main] In error state - checking for recovery...");

    // Check if we have valid config
    if (configManager.hasConfig()) {
        Serial.println("[Main] Config exists, attempting recovery...");
        delay(stateMachine.getRetryDelay());
        stateMachine.processEvent(StateEvent::RETRY_TIMEOUT);
    } else {
        Serial.println("[Main] No config, need BLE pairing...");
        delay(5000);
        // Clear any partial config and restart pairing
        configManager.clearConfig();
        stateMachine.processEvent(StateEvent::RESET_REQUESTED);
    }
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
    }

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
