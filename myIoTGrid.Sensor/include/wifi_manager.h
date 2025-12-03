/**
 * myIoTGrid.Sensor - WiFi Manager
 * Handles WiFi connection and reconnection
 */

#ifndef WIFI_MANAGER_H
#define WIFI_MANAGER_H

#include <Arduino.h>
#include <functional>

/**
 * WiFi connection status
 */
enum class WiFiStatus {
    DISCONNECTED,
    CONNECTING,
    CONNECTED,
    FAILED
};

/**
 * Callbacks
 */
using OnWiFiConnected = std::function<void(const String& ipAddress)>;
using OnWiFiDisconnected = std::function<void()>;
using OnWiFiFailed = std::function<void(const String& reason)>;

/**
 * WiFi Manager for node connectivity
 */
class WiFiManager {
public:
    WiFiManager();

    /**
     * Connect to WiFi network
     */
    bool connect(const String& ssid, const String& password, int timeoutMs = 30000);

    /**
     * Disconnect from WiFi
     */
    void disconnect();

    /**
     * Check connection status
     */
    WiFiStatus getStatus() const;

    /**
     * Check if connected
     */
    bool isConnected() const;

    /**
     * Get local IP address
     */
    String getIPAddress() const;

    /**
     * Get signal strength (RSSI)
     */
    int getRSSI() const;

    /**
     * Process WiFi events (call in loop)
     */
    void loop();

    /**
     * Set callbacks
     */
    void onConnected(OnWiFiConnected callback);
    void onDisconnected(OnWiFiDisconnected callback);
    void onFailed(OnWiFiFailed callback);

    /**
     * Reconnection settings
     */
    void setAutoReconnect(bool enabled) { _autoReconnect = enabled; }
    bool isAutoReconnectEnabled() const { return _autoReconnect; }

private:
    String _ssid;
    String _password;
    WiFiStatus _status;
    bool _autoReconnect;
    unsigned long _lastReconnectAttempt;
    int _reconnectAttempts;

    OnWiFiConnected _onConnected;
    OnWiFiDisconnected _onDisconnected;
    OnWiFiFailed _onFailed;

    static constexpr int MAX_RECONNECT_ATTEMPTS = 10;
    static constexpr unsigned long RECONNECT_INTERVAL = 5000;

    void attemptReconnect();
};

#endif // WIFI_MANAGER_H
