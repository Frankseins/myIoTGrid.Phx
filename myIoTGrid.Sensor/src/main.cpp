/**
 * myIoTGrid.Sensor - Main Entry Point
 *
 * Self-Provisioning ESP32 Firmware
 *
 * Flow:
 * 1. Boot → Check NVS for stored configuration
 * 2. If no config → Start BLE pairing mode
 * 3. Receive WiFi + API config via BLE
 * 4. Connect to WiFi
 * 5. Validate API key with Hub
 * 6. Enter operational mode (heartbeats + sensor readings)
 */

#include <Arduino.h>
#include "config.h"
#include "state_machine.h"
#include "config_manager.h"
#include "wifi_manager.h"
#include "api_client.h"

#ifdef PLATFORM_ESP32
#include "ble_service.h"
#include <esp_mac.h>
#endif

// ============================================================================
// Global Instances
// ============================================================================

StateMachine stateMachine;
ConfigManager configManager;
WiFiManager wifiManager;
ApiClient apiClient;

#ifdef PLATFORM_ESP32
BLEProvisioningService bleService;
#endif

// ============================================================================
// Configuration
// ============================================================================

static const unsigned long HEARTBEAT_INTERVAL_MS = 60000;   // 1 minute
static const unsigned long SENSOR_INTERVAL_MS = 60000;      // 1 minute
static const unsigned long WIFI_CHECK_INTERVAL_MS = 5000;   // 5 seconds

static unsigned long lastHeartbeat = 0;
static unsigned long lastSensorReading = 0;
static unsigned long lastWiFiCheck = 0;

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
    if (!apiClient.isConfigured() || !wifiManager.isConnected()) {
        return;
    }

    HeartbeatResponse response = apiClient.sendHeartbeat(FIRMWARE_VERSION);
    if (response.success) {
        Serial.printf("[Main] Heartbeat OK, next in %d seconds\n",
                      response.nextHeartbeatSeconds);
    } else {
        Serial.println("[Main] Heartbeat failed!");
    }
}

void readAndSendSensors() {
    if (!apiClient.isConfigured() || !wifiManager.isConnected()) {
        return;
    }

    // TODO: Read actual sensor values
    // For now, send simulated temperature reading
    float temperature = 20.0 + (random(100) / 10.0);  // 20.0 - 30.0°C
    float humidity = 40.0 + (random(400) / 10.0);     // 40.0 - 80.0%

    if (apiClient.sendReading("temperature", temperature, "°C")) {
        Serial.printf("[Main] Sent temperature: %.1f°C\n", temperature);
    }

    if (apiClient.sendReading("humidity", humidity, "%")) {
        Serial.printf("[Main] Sent humidity: %.1f%%\n", humidity);
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
    // Native/simulation mode - check for config file or environment variables
    Serial.println("[Main] No configuration - please set via environment variables");
    delay(5000);
#endif
}

void handlePairingState() {
#ifdef PLATFORM_ESP32
    // Just wait for BLE callback - bleService handles everything
    bleService.loop();
#endif
}

void handleConfiguredState() {
    static bool wifiConnecting = false;
    static bool apiKeyValidated = false;

    if (!wifiManager.isConnected() && !wifiConnecting) {
        // Load config and connect to WiFi
        StoredConfig config = configManager.loadConfig();
        if (config.isValid) {
            Serial.printf("[Main] Connecting to WiFi: %s\n", config.wifiSsid.c_str());

            // Configure API client
            apiClient.configure(config.hubApiUrl, config.nodeId, config.apiKey);

            // Start WiFi connection
            wifiManager.connect(config.wifiSsid, config.wifiPassword);
            wifiConnecting = true;
        } else {
            Serial.println("[Main] Invalid stored configuration!");
            stateMachine.processEvent(StateEvent::ERROR_OCCURRED);
        }
    }

    // Run WiFi manager loop
    wifiManager.loop();

    // If WiFi connected, validate API key and start operation
    if (wifiManager.isConnected() && wifiConnecting) {
        wifiConnecting = false;

        // Validate API key once after connection
        if (!apiKeyValidated) {
            if (validateApiKeyWithHub()) {
                apiKeyValidated = true;
                stateMachine.processEvent(StateEvent::API_VALIDATED);
            } else {
                // API key invalid - go to error state
                stateMachine.processEvent(StateEvent::API_FAILED);
            }
        }
    }
}

void handleOperationalState() {
    unsigned long now = millis();

    // Check WiFi connection periodically
    if (now - lastWiFiCheck >= WIFI_CHECK_INTERVAL_MS) {
        lastWiFiCheck = now;
        wifiManager.loop();

        if (!wifiManager.isConnected()) {
            Serial.println("[Main] WiFi lost in operational mode");
            stateMachine.processEvent(StateEvent::WIFI_FAILED);
            return;
        }
    }

    // Send heartbeat periodically
    if (now - lastHeartbeat >= HEARTBEAT_INTERVAL_MS) {
        lastHeartbeat = now;
        sendHeartbeat();
    }

    // Read and send sensor data periodically
    if (now - lastSensorReading >= SENSOR_INTERVAL_MS) {
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
    Serial.println("  myIoTGrid Sensor - Self-Provisioning");
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
