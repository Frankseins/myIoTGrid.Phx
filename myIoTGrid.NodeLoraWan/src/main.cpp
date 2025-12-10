/**
 * @file main.cpp
 * @brief myIoTGrid NodeLoraWan - Main Application
 *
 * LoRaWAN sensor node firmware for Heltec LoRa32 V3.
 * Collects sensor data and transmits via LoRaWAN to Grid.Hub.
 *
 * @version 1.0.0
 * @date 2025-12-10
 *
 * Sprint: LoRa-02 - Grid.Sensor LoRaWAN Firmware
 * Hackathon: 12./13. Dezember 2025
 *
 * Hardware: Heltec WiFi LoRa32 V3 (ESP32-S3 + SX1262)
 */

#ifdef PLATFORM_ESP32
#include <Arduino.h>
#endif

#include "config.h"
#include "hal/hal.h"
#include "hal/hal_lora.h"
#include "lora_credentials.h"
#include "oled_display.h"
#include "power_manager.h"
#include "lora_connection.h"
#include "bme280_sensor.h"
#include "water_level_sensor.h"

// ============================================================
// GLOBAL INSTANCES
// ============================================================

static LoRaConnection* loraConnection = nullptr;
static OledDisplay* display = nullptr;
static BME280Sensor* bmeSensor = nullptr;
static WaterLevelSensor* waterSensor = nullptr;

// Configuration
static uint32_t txIntervalSeconds = DEFAULT_TX_INTERVAL_SECONDS;
static uint32_t lastTxTime = 0;
static uint32_t joinAttempts = 0;
static bool hasWaterSensor = false;

// State
enum class NodeState {
    BOOTING,
    JOINING,
    OPERATIONAL,
    SLEEPING,
    ERROR
};

static NodeState currentState = NodeState::BOOTING;

// ============================================================
// FORWARD DECLARATIONS
// ============================================================

void initHardware();
void initSensors();
void initLoRa();
void runStateMachine();
void collectAndSendReadings();
void updateDisplay();
void handleButton();
void handleSerialCommands();
void enterDeepSleep();

// ============================================================
// ARDUINO ENTRY POINTS
// ============================================================

#ifdef PLATFORM_ESP32

void setup() {
    // Initialize serial first for debugging
    Serial.begin(SERIAL_BAUD);
    delay(100);

    LOG_INFO("=======================================");
    LOG_INFO("  myIoTGrid NodeLoraWan v%s", FIRMWARE_VERSION);
    LOG_INFO("  Heltec LoRa32 V3 - LoRaWAN Sensor");
    LOG_INFO("=======================================");

    // Check wake-up reason
    PowerManager::init();
    WakeReason wakeReason = PowerManager::getWakeReason();

    if (wakeReason == WakeReason::TIMER) {
        LOG_INFO("Woke from deep sleep (timer)");
        // Skip boot screen, go directly to operational
        currentState = NodeState::OPERATIONAL;
    } else if (wakeReason == WakeReason::BUTTON) {
        LOG_INFO("Woke from deep sleep (button)");
        currentState = NodeState::BOOTING;
    } else {
        LOG_INFO("Cold boot / reset");
        currentState = NodeState::BOOTING;
    }

    // Initialize hardware
    initHardware();

    // Initialize sensors
    initSensors();

    // Initialize LoRa
    initLoRa();
}

void loop() {
    // Handle serial configuration
    handleSerialCommands();

    // Handle user button
    handleButton();

    // Run state machine
    runStateMachine();

    // Process LoRa events
    if (loraConnection != nullptr) {
        loraConnection->process();
    }

    // Process display auto-off
    if (display != nullptr) {
        display->process();
    }

    // Small delay to prevent watchdog issues
    delay(10);
}

#else // PLATFORM_NATIVE

int main() {
    LOG_INFO("=======================================");
    LOG_INFO("  myIoTGrid NodeLoraWan v%s (Native)", FIRMWARE_VERSION);
    LOG_INFO("  Simulation Mode");
    LOG_INFO("=======================================");

    PowerManager::init();

    initHardware();
    initSensors();
    initLoRa();

    // Simple main loop for simulation
    for (int i = 0; i < 100; i++) {
        handleSerialCommands();
        runStateMachine();

        if (loraConnection != nullptr) {
            loraConnection->process();
        }

        hal::delay_ms(100);
    }

    LOG_INFO("Simulation complete");
    return 0;
}

#endif // PLATFORM_ESP32

// ============================================================
// INITIALIZATION
// ============================================================

void initHardware() {
    LOG_INFO("Initializing hardware...");

    // Initialize LED
    hal::pin_mode(LED_PIN, hal::PinMode::PIN_OUTPUT);
    hal::digital_write(LED_PIN, true);  // LED on during init

    // Initialize button
    hal::pin_mode(USER_BUTTON_PIN, hal::PinMode::PIN_INPUT_PULLUP);

    // Initialize display
    display = new OledDisplay();
    if (display->init()) {
        display->showBootScreen(FIRMWARE_VERSION);
        LOG_INFO("Display initialized");
    } else {
        LOG_WARN("Display initialization failed");
        delete display;
        display = nullptr;
    }

    // Initialize I2C for sensors
    hal::i2c_init(I2C_SDA, I2C_SCL, I2C_FREQUENCY);

    // Brief delay to show boot screen
    hal::delay_ms(1000);

    hal::digital_write(LED_PIN, false);  // LED off after init
    LOG_INFO("Hardware initialized");
}

void initSensors() {
    LOG_INFO("Initializing sensors...");

    // Initialize BME280 (temperature, humidity, pressure)
    bmeSensor = new BME280Sensor(BME280_ADDRESS_PRIMARY);
    if (!bmeSensor->begin()) {
        // Try alternate address
        delete bmeSensor;
        bmeSensor = new BME280Sensor(BME280_ADDRESS_SECONDARY);
        if (!bmeSensor->begin()) {
            LOG_ERROR("BME280 not found at any address");
            delete bmeSensor;
            bmeSensor = nullptr;
        }
    }

    if (bmeSensor != nullptr && bmeSensor->isReady()) {
        LOG_INFO("BME280 initialized");
        LOG_INFO("  Temperature: %.1f °C", bmeSensor->readTemperature());
        LOG_INFO("  Humidity: %.0f %%", bmeSensor->readHumidity());
        LOG_INFO("  Pressure: %.0f hPa", bmeSensor->readPressure());
    }

    // Initialize water level sensor (optional)
    waterSensor = new WaterLevelSensor();
    if (waterSensor->begin()) {
        hasWaterSensor = true;
        LOG_INFO("Water level sensor initialized");
        LOG_INFO("  Mount height: %.0f cm", waterSensor->getMountHeight());
        LOG_INFO("  Alarm level: %.0f cm", waterSensor->getAlarmLevel());
    } else {
        LOG_INFO("Water level sensor not available (optional)");
        delete waterSensor;
        waterSensor = nullptr;
    }

    LOG_INFO("Sensors initialized");
}

void initLoRa() {
    LOG_INFO("Initializing LoRaWAN...");

    loraConnection = new LoRaConnection();

    // Initialize credential manager
    loraConnection->getCredentialManager().init();

    // Print current credentials
    loraConnection->getCredentialManager().printCredentials();

    // Set configuration callback
    loraConnection->onConfigReceived([](const NodeConfig& config) {
        LOG_INFO("Received new configuration:");
        LOG_INFO("  Interval: %u seconds", config.intervalSeconds);
        txIntervalSeconds = config.intervalSeconds;
    });

    // Check if credentials are configured
    if (!loraConnection->getCredentialManager().isReadyForOtaa()) {
        LOG_WARN("LoRaWAN credentials not configured!");
        LOG_INFO("Use serial commands to configure:");
        LOG_INFO("  APPEUI=<16 hex chars>");
        LOG_INFO("  APPKEY=<32 hex chars>");
        LOG_INFO("  SAVE");

        currentState = NodeState::ERROR;
        if (display != nullptr) {
            display->showError("No LoRa credentials", 1);
        }
        return;
    }

    // Start join process
    currentState = NodeState::JOINING;
    joinAttempts = 0;

    LOG_INFO("LoRaWAN initialization complete");
}

// ============================================================
// STATE MACHINE
// ============================================================

void runStateMachine() {
    switch (currentState) {
        case NodeState::BOOTING:
            // Should not reach here normally
            currentState = NodeState::JOINING;
            break;

        case NodeState::JOINING:
            // Attempt to join network
            joinAttempts++;

            if (display != nullptr) {
                display->showJoinScreen(
                    loraConnection->getCredentialManager().getCredentials().getDevEuiString().c_str(),
                    true,
                    joinAttempts
                );
            }

            // Blink LED during join
            hal::digital_write(LED_PIN, joinAttempts % 2);

            LOG_INFO("OTAA join attempt %u...", joinAttempts);

            if (loraConnection->connect()) {
                LOG_INFO("Joined network successfully!");
                currentState = NodeState::OPERATIONAL;
                hal::digital_write(LED_PIN, false);

                // Show status screen
                updateDisplay();

                // Send first reading immediately
                collectAndSendReadings();
                lastTxTime = hal::millis();
            } else {
                LOG_WARN("Join failed, retrying in %u seconds", JOIN_RETRY_INTERVAL_SECONDS);

                if (joinAttempts >= MAX_JOIN_RETRIES) {
                    LOG_ERROR("Max join attempts reached, entering deep sleep");
                    currentState = NodeState::ERROR;
                    if (display != nullptr) {
                        display->showError("Join failed", joinAttempts);
                    }
                    hal::delay_ms(3000);
                    enterDeepSleep();
                }

                hal::delay_ms(JOIN_RETRY_INTERVAL_SECONDS * 1000);
            }
            break;

        case NodeState::OPERATIONAL:
            {
                uint32_t now = hal::millis();
                uint32_t elapsed = now - lastTxTime;

                // Check if it's time to transmit
                if (elapsed >= txIntervalSeconds * 1000) {
                    collectAndSendReadings();
                    lastTxTime = now;

                    // If deep sleep is enabled, sleep between transmissions
                    if (DEEP_SLEEP_ENABLED) {
                        currentState = NodeState::SLEEPING;
                    }
                }

                // Update display periodically
                static uint32_t lastDisplayUpdate = 0;
                if (now - lastDisplayUpdate > 5000) {  // Every 5 seconds
                    updateDisplay();
                    lastDisplayUpdate = now;
                }
            }
            break;

        case NodeState::SLEEPING:
            enterDeepSleep();
            break;

        case NodeState::ERROR:
            // Blink LED to indicate error
            hal::digital_write(LED_PIN, (hal::millis() / 200) % 2);

            // Try to recover after some time
            static uint32_t errorStartTime = 0;
            if (errorStartTime == 0) {
                errorStartTime = hal::millis();
            }
            if (hal::millis() - errorStartTime > 60000) {  // 1 minute
                errorStartTime = 0;
                joinAttempts = 0;
                currentState = NodeState::JOINING;
            }
            break;
    }
}

// ============================================================
// SENSOR READING AND TRANSMISSION
// ============================================================

void collectAndSendReadings() {
    LOG_INFO("Collecting sensor readings...");

    std::vector<Reading> readings;

    // Read BME280
    if (bmeSensor != nullptr && bmeSensor->isReady()) {
        Reading temp;
        temp.type = "temperature";
        temp.value = bmeSensor->readTemperature();
        temp.unit = "°C";
        temp.timestamp = hal::timestamp();
        readings.push_back(temp);

        Reading hum;
        hum.type = "humidity";
        hum.value = bmeSensor->readHumidity();
        hum.unit = "%";
        hum.timestamp = hal::timestamp();
        readings.push_back(hum);

        Reading press;
        press.type = "pressure";
        press.value = bmeSensor->readPressure();
        press.unit = "hPa";
        press.timestamp = hal::timestamp();
        readings.push_back(press);

        LOG_INFO("  Temperature: %.1f °C", temp.value);
        LOG_INFO("  Humidity: %.0f %%", hum.value);
        LOG_INFO("  Pressure: %.0f hPa", press.value);
    }

    // Read water level sensor
    if (waterSensor != nullptr && waterSensor->isReady()) {
        Reading water;
        water.type = "water_level";
        water.value = waterSensor->read();
        water.unit = "cm";
        water.timestamp = hal::timestamp();
        readings.push_back(water);

        LOG_INFO("  Water level: %.1f cm", water.value);

        // Check alarm
        if (waterSensor->isAlarmActive()) {
            LOG_WARN("  WATER LEVEL ALARM! (>%.0f cm)", waterSensor->getAlarmLevel());
        }
    }

    // Add battery level
    Reading battery;
    battery.type = "battery";
    battery.value = PowerManager::getBatteryPercent();
    battery.unit = "%";
    battery.timestamp = hal::timestamp();
    readings.push_back(battery);

    LOG_INFO("  Battery: %u %%", (uint8_t)battery.value);

    // Check battery level
    if (PowerManager::isBatteryLow()) {
        LOG_WARN("  LOW BATTERY WARNING!");
    }

    // Show transmitting indicator
    if (display != nullptr) {
        display->showTransmitting(true);
    }
    hal::digital_write(LED_PIN, true);

    // Send readings
    LOG_INFO("Sending %zu readings via LoRaWAN...", readings.size());

    bool success = loraConnection->sendBatch(readings);

    hal::digital_write(LED_PIN, false);
    if (display != nullptr) {
        display->showTransmitting(false);
    }

    if (success) {
        LOG_INFO("Readings sent successfully");
        LOG_INFO("  Frame counter: %u", loraConnection->getFrameCounter());
        LOG_INFO("  RSSI: %d dBm", loraConnection->getLastRssi());
        LOG_INFO("  SNR: %d dB", loraConnection->getLastSnr());
    } else {
        LOG_ERROR("Failed to send readings");
    }
}

// ============================================================
// DISPLAY UPDATE
// ============================================================

void updateDisplay() {
    if (display == nullptr) return;

    switch (display->getCurrentScreen()) {
        case DisplayScreen::STATUS:
            display->showStatusScreen(
                loraConnection->isConnected(),
                loraConnection->getLastRssi(),
                loraConnection->getLastSnr(),
                loraConnection->getFrameCounter(),
                PowerManager::getBatteryPercent()
            );
            break;

        case DisplayScreen::READINGS:
            if (bmeSensor != nullptr && bmeSensor->isReady()) {
                display->showReadingScreen(
                    bmeSensor->readTemperature(),
                    bmeSensor->readHumidity(),
                    bmeSensor->readPressure(),
                    hasWaterSensor ? waterSensor->read() : -1.0f
                );
            }
            break;

        case DisplayScreen::CONFIG:
            display->showConfigScreen(
                loraConnection->getCredentialManager().getCredentials().getDevEuiString().c_str(),
                txIntervalSeconds,
                hal::lora::get_data_rate()
            );
            break;

        default:
            // Show status screen by default
            display->setScreen(DisplayScreen::STATUS);
            break;
    }
}

// ============================================================
// BUTTON HANDLING
// ============================================================

void handleButton() {
    static bool lastButtonState = true;  // Pull-up: HIGH when not pressed
    static uint32_t pressStartTime = 0;

    bool buttonState = hal::digital_read(USER_BUTTON_PIN);

    // Button pressed (LOW)
    if (!buttonState && lastButtonState) {
        pressStartTime = hal::millis();
    }

    // Button released (HIGH)
    if (buttonState && !lastButtonState) {
        uint32_t pressDuration = hal::millis() - pressStartTime;

        if (pressDuration > 50 && pressDuration < 1000) {
            // Short press: cycle display screen
            LOG_DEBUG("Button short press - cycling screen");
            if (display != nullptr) {
                display->nextScreen();
                display->resetTimeout();
                updateDisplay();
            }
        } else if (pressDuration >= 1000 && pressDuration < 5000) {
            // Medium press: force transmission
            LOG_INFO("Button medium press - forcing transmission");
            collectAndSendReadings();
            lastTxTime = hal::millis();
        } else if (pressDuration >= 5000) {
            // Long press: restart device
            LOG_INFO("Button long press - restarting...");
            hal::restart();
        }
    }

    lastButtonState = buttonState;
}

// ============================================================
// SERIAL COMMANDS
// ============================================================

void handleSerialCommands() {
    // Handle credential configuration via serial
    if (loraConnection != nullptr) {
        loraConnection->getCredentialManager().handleSerialConfig();
    }
}

// ============================================================
// DEEP SLEEP
// ============================================================

void enterDeepSleep() {
    LOG_INFO("Preparing for deep sleep...");

    // Save frame counters
    if (loraConnection != nullptr) {
        loraConnection->getCredentialManager().saveFrameCounters();
    }

    // Turn off display
    if (display != nullptr) {
        display->turnOff();
    }

    // Put LoRa radio to sleep
    hal::lora::sleep();

    // Calculate sleep duration
    uint32_t sleepSeconds = txIntervalSeconds;

    // Use adaptive sleep if battery is low
    if (PowerManager::isBatteryLow()) {
        sleepSeconds = PowerManager::deepSleepAdaptive(sleepSeconds);
    } else {
        PowerManager::deepSleep(sleepSeconds);
    }

    // We should never reach here (deep sleep restarts the device)
    LOG_ERROR("Deep sleep failed!");
    currentState = NodeState::OPERATIONAL;
}
