/**
 * @file oled_display.h
 * @brief OLED Display Interface for Heltec LoRa32 V3
 *
 * Provides status display on the built-in 128x64 OLED.
 * Features multiple screens (status, readings, config) with
 * auto-off for power saving.
 *
 * @version 1.0.0
 * @date 2025-12-10
 *
 * Sprint: LoRa-02 - Grid.Sensor LoRaWAN Firmware
 */

#pragma once

#include <cstdint>
#include <string>

/**
 * @brief Display Screen Types
 */
enum class DisplayScreen {
    BOOT,           ///< Boot screen with logo/version
    JOIN,           ///< Join progress screen
    STATUS,         ///< Network status screen
    READINGS,       ///< Sensor readings screen
    CONFIG,         ///< Configuration screen
    ERROR           ///< Error display screen
};

/**
 * @brief OLED Display Controller
 *
 * Manages the 128x64 OLED display on Heltec LoRa32 V3.
 * Features:
 * - Multiple display screens
 * - Auto-off after timeout
 * - Button-triggered screen switch
 * - Battery indicator
 */
class OledDisplay {
public:
    OledDisplay();
    ~OledDisplay();

    // === Initialization ===

    /**
     * @brief Initialize display hardware
     * @return true if initialization successful
     */
    bool init();

    /**
     * @brief Check if display is initialized
     * @return true if initialized
     */
    bool isInitialized() const { return initialized_; }

    // === Screen Display ===

    /**
     * @brief Show boot screen with version
     * @param version Firmware version string
     */
    void showBootScreen(const char* version);

    /**
     * @brief Show LoRaWAN join progress screen
     * @param devEui Device EUI string
     * @param joining true if join in progress
     * @param attempt Current join attempt number
     */
    void showJoinScreen(const char* devEui, bool joining, uint8_t attempt = 0);

    /**
     * @brief Show network status screen
     * @param joined true if joined to network
     * @param rssi RSSI in dBm
     * @param snr SNR in dB
     * @param frameCount Uplink frame counter
     * @param battery Battery percentage
     */
    void showStatusScreen(
        bool joined,
        int16_t rssi,
        int8_t snr,
        uint32_t frameCount,
        uint8_t battery
    );

    /**
     * @brief Show sensor readings screen
     * @param temperature Temperature in °C
     * @param humidity Humidity in %
     * @param pressure Pressure in hPa
     * @param waterLevel Water level in cm (optional, -1 to hide)
     */
    void showReadingScreen(
        float temperature,
        float humidity,
        float pressure,
        float waterLevel = -1.0f
    );

    /**
     * @brief Show configuration screen
     * @param devEui Device EUI
     * @param interval Transmission interval in seconds
     * @param dataRate Current data rate
     */
    void showConfigScreen(
        const char* devEui,
        uint32_t interval,
        uint8_t dataRate
    );

    /**
     * @brief Show error message
     * @param message Error message
     * @param code Error code (optional)
     */
    void showError(const char* message, int code = 0);

    /**
     * @brief Show transmission indicator
     * @param sending true if transmission in progress
     */
    void showTransmitting(bool sending);

    // === Screen Management ===

    /**
     * @brief Switch to next screen
     */
    void nextScreen();

    /**
     * @brief Switch to previous screen
     */
    void prevScreen();

    /**
     * @brief Set specific screen
     * @param screen Screen to display
     */
    void setScreen(DisplayScreen screen);

    /**
     * @brief Get current screen
     * @return Current screen type
     */
    DisplayScreen getCurrentScreen() const { return currentScreen_; }

    // === Power Management ===

    /**
     * @brief Turn display off
     */
    void turnOff();

    /**
     * @brief Turn display on
     */
    void turnOn();

    /**
     * @brief Check if display is on
     * @return true if display is active
     */
    bool isOn() const { return displayOn_; }

    /**
     * @brief Reset auto-off timer (called on user interaction)
     */
    void resetTimeout();

    /**
     * @brief Process auto-off timer
     *
     * Must be called regularly in main loop.
     */
    void process();

    // === Utility ===

    /**
     * @brief Clear display
     */
    void clear();

    /**
     * @brief Force display update
     */
    void update();

    /**
     * @brief Set display brightness
     * @param brightness 0-255
     */
    void setBrightness(uint8_t brightness);

    /**
     * @brief Invert display colors
     * @param invert true to invert
     */
    void setInverted(bool invert);

private:
    bool initialized_ = false;
    bool displayOn_ = true;
    DisplayScreen currentScreen_ = DisplayScreen::BOOT;
    uint32_t lastActivity_ = 0;

    // Display dimensions
    static constexpr uint8_t WIDTH = 128;
    static constexpr uint8_t HEIGHT = 64;

    // Auto-off timeout
    static constexpr uint32_t AUTO_OFF_MS = 30000;  // 30 seconds

    // === Drawing Helpers ===

    void drawHeader(const char* title);
    void drawFooter(uint8_t battery);
    void drawBatteryIcon(uint8_t x, uint8_t y, uint8_t percent);
    void drawSignalBars(uint8_t x, uint8_t y, int16_t rssi);
    void drawProgressBar(uint8_t x, uint8_t y, uint8_t width, uint8_t percent);
    void drawCenteredText(const char* text, uint8_t y);

    // Platform-specific implementation
    void* display_ = nullptr;  // Adafruit_SSD1306*
};
