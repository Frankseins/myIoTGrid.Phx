/**
 * @file oled_display.cpp
 * @brief OLED Display Implementation for Heltec LoRa32 V3
 *
 * @version 1.0.0
 * @date 2025-12-10
 *
 * Sprint: LoRa-02 - Grid.Sensor LoRaWAN Firmware
 */

#ifdef PLATFORM_ESP32
#include <Arduino.h>
#endif

#include "oled_display.h"
#include "config.h"
#include "hal/hal.h"

#ifdef PLATFORM_ESP32
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

// Display instance
static Adafruit_SSD1306* ssd1306 = nullptr;
#endif

// ============================================================
// CONSTRUCTOR / DESTRUCTOR
// ============================================================

OledDisplay::OledDisplay() {
    lastActivity_ = hal::millis();
}

OledDisplay::~OledDisplay() {
#ifdef PLATFORM_ESP32
    if (ssd1306 != nullptr) {
        delete ssd1306;
        ssd1306 = nullptr;
    }
#endif
}

// ============================================================
// INITIALIZATION
// ============================================================

bool OledDisplay::init() {
#ifdef PLATFORM_ESP32
    LOG_INFO("Initializing OLED display...");

    // Reset display
    pinMode(OLED_RST, OUTPUT);
    digitalWrite(OLED_RST, LOW);
    delay(20);
    digitalWrite(OLED_RST, HIGH);
    delay(20);

    // Initialize I2C for display
    Wire.begin(OLED_SDA, OLED_SCL);

    // Create display instance
    ssd1306 = new Adafruit_SSD1306(WIDTH, HEIGHT, &Wire, OLED_RST);

    if (!ssd1306->begin(SSD1306_SWITCHCAPVCC, OLED_ADDRESS)) {
        LOG_ERROR("SSD1306 initialization failed");
        delete ssd1306;
        ssd1306 = nullptr;
        return false;
    }

    // Configure display
    ssd1306->clearDisplay();
    ssd1306->setTextSize(1);
    ssd1306->setTextColor(SSD1306_WHITE);
    ssd1306->display();

    display_ = ssd1306;
    initialized_ = true;
    displayOn_ = true;
    lastActivity_ = hal::millis();

    LOG_INFO("OLED display initialized");
    return true;
#else
    // Native simulation - always succeeds
    initialized_ = true;
    displayOn_ = true;
    LOG_INFO("[SIM] OLED display initialized");
    return true;
#endif
}

// ============================================================
// SCREEN DISPLAY
// ============================================================

void OledDisplay::showBootScreen(const char* version) {
    if (!initialized_) return;

#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);
    disp->clearDisplay();

    // Logo area
    disp->setTextSize(2);
    disp->setCursor(10, 10);
    disp->print("myIoTGrid");

    // Subtitle
    disp->setTextSize(1);
    disp->setCursor(20, 35);
    disp->print("LoRaWAN Sensor");

    // Version
    disp->setCursor(35, 50);
    disp->print("v");
    disp->print(version);

    disp->display();
#else
    LOG_INFO("[DISPLAY] Boot Screen: myIoTGrid v%s", version);
#endif

    currentScreen_ = DisplayScreen::BOOT;
    lastActivity_ = hal::millis();
}

void OledDisplay::showJoinScreen(const char* devEui, bool joining, uint8_t attempt) {
    if (!initialized_) return;

#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);
    disp->clearDisplay();

    drawHeader("LoRaWAN Join");

    // DevEUI
    disp->setTextSize(1);
    disp->setCursor(0, 16);
    disp->print("DevEUI:");
    disp->setCursor(0, 26);
    disp->setTextSize(1);
    // Show first 8 and last 4 characters
    char shortEui[16];
    snprintf(shortEui, sizeof(shortEui), "%.8s...%.4s",
             devEui, devEui + strlen(devEui) - 4);
    disp->print(shortEui);

    // Status
    disp->setCursor(0, 42);
    if (joining) {
        disp->print("Joining");
        // Animated dots
        for (int i = 0; i < (attempt % 4); i++) {
            disp->print(".");
        }
        disp->setCursor(0, 52);
        disp->print("Attempt: ");
        disp->print(attempt);
    } else {
        disp->print("Waiting...");
    }

    disp->display();
#else
    LOG_INFO("[DISPLAY] Join Screen: DevEUI=%s, joining=%d, attempt=%d",
             devEui, joining, attempt);
#endif

    currentScreen_ = DisplayScreen::JOIN;
    lastActivity_ = hal::millis();
}

void OledDisplay::showStatusScreen(
    bool joined,
    int16_t rssi,
    int8_t snr,
    uint32_t frameCount,
    uint8_t battery
) {
    if (!initialized_) return;

#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);
    disp->clearDisplay();

    drawHeader("Status");
    drawFooter(battery);

    disp->setTextSize(1);

    // Join status
    disp->setCursor(0, 16);
    disp->print("Status: ");
    disp->print(joined ? "JOINED" : "NOT JOINED");

    if (joined) {
        // RSSI
        disp->setCursor(0, 26);
        disp->print("RSSI: ");
        disp->print(rssi);
        disp->print(" dBm");

        // Signal bars
        drawSignalBars(100, 24, rssi);

        // SNR
        disp->setCursor(0, 36);
        disp->print("SNR:  ");
        disp->print(snr);
        disp->print(" dB");

        // Frame counter
        disp->setCursor(0, 46);
        disp->print("Frame: ");
        disp->print(frameCount);
    }

    disp->display();
#else
    LOG_INFO("[DISPLAY] Status: joined=%d, RSSI=%d, SNR=%d, FC=%u, Bat=%u%%",
             joined, rssi, snr, frameCount, battery);
#endif

    currentScreen_ = DisplayScreen::STATUS;
    lastActivity_ = hal::millis();
}

void OledDisplay::showReadingScreen(
    float temperature,
    float humidity,
    float pressure,
    float waterLevel
) {
    if (!initialized_) return;

#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);
    disp->clearDisplay();

    drawHeader("Readings");

    // Temperature (large)
    disp->setTextSize(2);
    disp->setCursor(0, 16);
    disp->print(temperature, 1);
    disp->setTextSize(1);
    disp->print(" C");

    // Humidity
    disp->setTextSize(1);
    disp->setCursor(0, 36);
    disp->print("Humidity: ");
    disp->print(humidity, 0);
    disp->print("%");

    // Pressure
    disp->setCursor(0, 46);
    disp->print("Pressure: ");
    disp->print(pressure, 0);
    disp->print(" hPa");

    // Water level (if provided)
    if (waterLevel >= 0) {
        disp->setCursor(0, 56);
        disp->print("Water: ");
        disp->print(waterLevel, 0);
        disp->print(" cm");
    }

    disp->display();
#else
    LOG_INFO("[DISPLAY] Readings: T=%.1f, H=%.0f%%, P=%.0f, W=%.0f",
             temperature, humidity, pressure, waterLevel);
#endif

    currentScreen_ = DisplayScreen::READINGS;
    lastActivity_ = hal::millis();
}

void OledDisplay::showConfigScreen(
    const char* devEui,
    uint32_t interval,
    uint8_t dataRate
) {
    if (!initialized_) return;

#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);
    disp->clearDisplay();

    drawHeader("Config");

    disp->setTextSize(1);

    // DevEUI
    disp->setCursor(0, 16);
    disp->print("DevEUI:");
    disp->setCursor(0, 26);
    char shortEui[16];
    snprintf(shortEui, sizeof(shortEui), "%.8s...%.4s",
             devEui, devEui + strlen(devEui) - 4);
    disp->print(shortEui);

    // Interval
    disp->setCursor(0, 40);
    disp->print("Interval: ");
    disp->print(interval);
    disp->print("s");

    // Data rate
    disp->setCursor(0, 50);
    disp->print("Data Rate: DR");
    disp->print(dataRate);
    disp->print(" (SF");
    disp->print(12 - dataRate);
    disp->print(")");

    disp->display();
#else
    LOG_INFO("[DISPLAY] Config: DevEUI=%s, Interval=%us, DR=%u",
             devEui, interval, dataRate);
#endif

    currentScreen_ = DisplayScreen::CONFIG;
    lastActivity_ = hal::millis();
}

void OledDisplay::showError(const char* message, int code) {
    if (!initialized_) return;

#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);
    disp->clearDisplay();

    drawHeader("ERROR");

    disp->setTextSize(1);
    disp->setCursor(0, 20);
    disp->print(message);

    if (code != 0) {
        disp->setCursor(0, 40);
        disp->print("Code: ");
        disp->print(code);
    }

    disp->display();
#else
    LOG_ERROR("[DISPLAY] Error: %s (code %d)", message, code);
#endif

    currentScreen_ = DisplayScreen::ERROR;
    lastActivity_ = hal::millis();
}

void OledDisplay::showTransmitting(bool sending) {
    if (!initialized_) return;

#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);

    // Draw TX indicator in top-right corner
    if (sending) {
        disp->fillRect(WIDTH - 20, 0, 20, 8, SSD1306_WHITE);
        disp->setTextColor(SSD1306_BLACK);
        disp->setCursor(WIDTH - 18, 0);
        disp->print("TX");
        disp->setTextColor(SSD1306_WHITE);
    } else {
        disp->fillRect(WIDTH - 20, 0, 20, 8, SSD1306_BLACK);
    }

    disp->display();
#else
    if (sending) {
        LOG_INFO("[DISPLAY] TX indicator ON");
    }
#endif
}

// ============================================================
// SCREEN MANAGEMENT
// ============================================================

void OledDisplay::nextScreen() {
    uint8_t current = static_cast<uint8_t>(currentScreen_);
    current = (current + 1) % 4;  // Cycle through STATUS, READINGS, CONFIG
    if (current == 0) current = 2;  // Skip BOOT and JOIN
    currentScreen_ = static_cast<DisplayScreen>(current);
    resetTimeout();
}

void OledDisplay::prevScreen() {
    uint8_t current = static_cast<uint8_t>(currentScreen_);
    if (current <= 2) current = 4;
    current--;
    currentScreen_ = static_cast<DisplayScreen>(current);
    resetTimeout();
}

void OledDisplay::setScreen(DisplayScreen screen) {
    currentScreen_ = screen;
    resetTimeout();
}

// ============================================================
// POWER MANAGEMENT
// ============================================================

void OledDisplay::turnOff() {
    if (!initialized_ || !displayOn_) return;

#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);
    disp->ssd1306_command(SSD1306_DISPLAYOFF);
#endif

    displayOn_ = false;
    LOG_DEBUG("Display turned off");
}

void OledDisplay::turnOn() {
    if (!initialized_ || displayOn_) return;

#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);
    disp->ssd1306_command(SSD1306_DISPLAYON);
#endif

    displayOn_ = true;
    lastActivity_ = hal::millis();
    LOG_DEBUG("Display turned on");
}

void OledDisplay::resetTimeout() {
    lastActivity_ = hal::millis();
    if (!displayOn_) {
        turnOn();
    }
}

void OledDisplay::process() {
    if (!initialized_) return;

    // Auto-off timer
    if (displayOn_ && (hal::millis() - lastActivity_ > AUTO_OFF_MS)) {
        turnOff();
    }
}

// ============================================================
// UTILITY
// ============================================================

void OledDisplay::clear() {
    if (!initialized_) return;

#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);
    disp->clearDisplay();
    disp->display();
#endif
}

void OledDisplay::update() {
    if (!initialized_) return;

#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);
    disp->display();
#endif
}

void OledDisplay::setBrightness(uint8_t brightness) {
    if (!initialized_) return;

#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);
    disp->ssd1306_command(SSD1306_SETCONTRAST);
    disp->ssd1306_command(brightness);
#endif
}

void OledDisplay::setInverted(bool invert) {
    if (!initialized_) return;

#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);
    disp->invertDisplay(invert);
#endif
}

// ============================================================
// DRAWING HELPERS
// ============================================================

void OledDisplay::drawHeader(const char* title) {
#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);
    disp->setTextSize(1);
    disp->setCursor(0, 0);
    disp->print("=== ");
    disp->print(title);
    disp->print(" ===");
#endif
}

void OledDisplay::drawFooter(uint8_t battery) {
#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);

    // Battery percentage
    disp->setCursor(WIDTH - 35, HEIGHT - 8);
    disp->print(battery);
    disp->print("%");

    // Battery icon
    drawBatteryIcon(WIDTH - 12, HEIGHT - 8, battery);
#endif
}

void OledDisplay::drawBatteryIcon(uint8_t x, uint8_t y, uint8_t percent) {
#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);

    // Battery outline
    disp->drawRect(x, y, 10, 6, SSD1306_WHITE);
    disp->drawRect(x + 10, y + 1, 2, 4, SSD1306_WHITE);

    // Fill based on percentage
    uint8_t fillWidth = (percent * 8) / 100;
    if (fillWidth > 0) {
        disp->fillRect(x + 1, y + 1, fillWidth, 4, SSD1306_WHITE);
    }
#endif
}

void OledDisplay::drawSignalBars(uint8_t x, uint8_t y, int16_t rssi) {
#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);

    // Calculate signal strength (0-4 bars)
    uint8_t bars = 0;
    if (rssi > -50) bars = 4;
    else if (rssi > -60) bars = 3;
    else if (rssi > -70) bars = 2;
    else if (rssi > -80) bars = 1;

    // Draw bars
    for (uint8_t i = 0; i < 4; i++) {
        uint8_t barHeight = (i + 1) * 2;
        uint8_t barY = y + 8 - barHeight;

        if (i < bars) {
            disp->fillRect(x + i * 5, barY, 3, barHeight, SSD1306_WHITE);
        } else {
            disp->drawRect(x + i * 5, barY, 3, barHeight, SSD1306_WHITE);
        }
    }
#endif
}

void OledDisplay::drawProgressBar(uint8_t x, uint8_t y, uint8_t width, uint8_t percent) {
#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);

    // Outline
    disp->drawRect(x, y, width, 8, SSD1306_WHITE);

    // Fill
    uint8_t fillWidth = ((width - 2) * percent) / 100;
    if (fillWidth > 0) {
        disp->fillRect(x + 1, y + 1, fillWidth, 6, SSD1306_WHITE);
    }
#endif
}

void OledDisplay::drawCenteredText(const char* text, uint8_t y) {
#ifdef PLATFORM_ESP32
    auto* disp = static_cast<Adafruit_SSD1306*>(display_);

    uint16_t textWidth = strlen(text) * 6;  // 6 pixels per character
    uint8_t x = (WIDTH - textWidth) / 2;

    disp->setCursor(x, y);
    disp->print(text);
#endif
}
