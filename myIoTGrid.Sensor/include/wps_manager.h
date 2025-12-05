/**
 * myIoTGrid.Sensor - WPS (WiFi Protected Setup) Manager
 *
 * Allows WiFi configuration via router's WPS button.
 * User presses Boot button for 3 seconds to enter WPS mode.
 */

#ifndef WPS_MANAGER_H
#define WPS_MANAGER_H

#include <Arduino.h>
#include <functional>

#ifdef PLATFORM_ESP32
#include <WiFi.h>
#include <esp_wps.h>
#endif

/**
 * WPS Status enum
 */
enum class WPSStatus {
    IDLE,           // Not started
    SCANNING,       // Looking for WPS-enabled AP
    CONNECTING,     // Connecting to AP
    SUCCESS,        // Successfully connected
    TIMEOUT,        // Timeout (120 seconds)
    FAILED          // WPS failed (overlap, etc.)
};

/**
 * WPS Result structure
 */
struct WPSResult {
    bool success;
    String ssid;
    String password;
    String errorMessage;
};

// Callback types
using OnWPSSuccess = std::function<void(const String& ssid, const String& password)>;
using OnWPSFailed = std::function<void(const String& reason)>;
using OnWPSTimeout = std::function<void()>;

/**
 * WPS Manager class
 *
 * Handles WiFi Protected Setup (push-button method).
 * Supports both PBC (Push Button Configuration) and PIN modes.
 */
class WPSManager {
public:
    WPSManager();
    ~WPSManager();

    /**
     * Initialize WPS manager
     * @return true if successful
     */
    bool init();

    /**
     * Start WPS (Push Button Configuration mode)
     * User should press WPS button on router within 120 seconds
     * @return true if WPS started successfully
     */
    bool startWPS();

    /**
     * Stop WPS process
     */
    void stopWPS();

    /**
     * Get current WPS status
     */
    WPSStatus getStatus() const;

    /**
     * Check if WPS is active
     */
    bool isActive() const;

    /**
     * Process WPS events (call in loop)
     */
    void loop();

    /**
     * Get WiFi credentials after successful WPS
     */
    WPSResult getResult() const;

    /**
     * Set callback for successful WPS
     */
    void onSuccess(OnWPSSuccess callback);

    /**
     * Set callback for WPS failure
     */
    void onFailed(OnWPSFailed callback);

    /**
     * Set callback for WPS timeout
     */
    void onTimeout(OnWPSTimeout callback);

    /**
     * Get status name as string
     */
    static const char* getStatusName(WPSStatus status);

private:
    WPSStatus _status;
    unsigned long _startTime;
    WPSResult _result;
    bool _initialized;

    // Callbacks
    OnWPSSuccess _onSuccess;
    OnWPSFailed _onFailed;
    OnWPSTimeout _onTimeout;

    // Configuration
    static const unsigned long WPS_TIMEOUT_MS = 120000;  // 2 minutes
    static const int MAX_CONNECT_RETRIES = 3;
    int _connectRetryCount = 0;

#ifdef PLATFORM_ESP32
    // ESP32-specific WPS handling
    static void wpsEventCallback(arduino_event_id_t event, arduino_event_info_t info);
    static WPSManager* _instance;  // For static callback
    void handleWPSEvent(arduino_event_id_t event, arduino_event_info_t info);
#endif
};

#endif // WPS_MANAGER_H
