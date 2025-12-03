/**
 * myIoTGrid.Sensor - WiFi Manager Implementation
 */

#include "wifi_manager.h"

#ifdef PLATFORM_ESP32
#include <WiFi.h>
#endif

WiFiManager::WiFiManager()
    : _status(WiFiStatus::DISCONNECTED)
    , _autoReconnect(true)
    , _lastReconnectAttempt(0)
    , _reconnectAttempts(0) {
}

bool WiFiManager::connect(const String& ssid, const String& password, int timeoutMs) {
    _ssid = ssid;
    _password = password;
    _status = WiFiStatus::CONNECTING;
    _reconnectAttempts = 0;

    Serial.printf("[WiFi] Connecting to %s...\n", ssid.c_str());

#ifdef PLATFORM_ESP32
    WiFi.mode(WIFI_STA);
    WiFi.begin(ssid.c_str(), password.c_str());

    unsigned long start = millis();
    while (WiFi.status() != WL_CONNECTED && (millis() - start) < timeoutMs) {
        delay(500);
        Serial.print(".");
    }
    Serial.println();

    if (WiFi.status() == WL_CONNECTED) {
        _status = WiFiStatus::CONNECTED;
        String ip = WiFi.localIP().toString();
        Serial.printf("[WiFi] Connected! IP: %s\n", ip.c_str());

        if (_onConnected) {
            _onConnected(ip);
        }
        return true;
    } else {
        _status = WiFiStatus::FAILED;
        Serial.println("[WiFi] Connection failed!");

        if (_onFailed) {
            _onFailed("Connection timeout");
        }
        return false;
    }
#else
    // Simulation mode
    Serial.printf("[WiFi] Simulated connection to %s\n", ssid.c_str());
    delay(1000);
    _status = WiFiStatus::CONNECTED;

    if (_onConnected) {
        _onConnected("192.168.1.100");
    }
    return true;
#endif
}

void WiFiManager::disconnect() {
#ifdef PLATFORM_ESP32
    WiFi.disconnect(true);
#endif
    _status = WiFiStatus::DISCONNECTED;
    Serial.println("[WiFi] Disconnected");
}

WiFiStatus WiFiManager::getStatus() const {
    return _status;
}

bool WiFiManager::isConnected() const {
#ifdef PLATFORM_ESP32
    return WiFi.status() == WL_CONNECTED;
#else
    return _status == WiFiStatus::CONNECTED;
#endif
}

String WiFiManager::getIPAddress() const {
#ifdef PLATFORM_ESP32
    if (isConnected()) {
        return WiFi.localIP().toString();
    }
#endif
    return "";
}

int WiFiManager::getRSSI() const {
#ifdef PLATFORM_ESP32
    if (isConnected()) {
        return WiFi.RSSI();
    }
#endif
    return -100;
}

void WiFiManager::loop() {
#ifdef PLATFORM_ESP32
    if (_autoReconnect && _status == WiFiStatus::CONNECTED && WiFi.status() != WL_CONNECTED) {
        Serial.println("[WiFi] Connection lost!");
        _status = WiFiStatus::DISCONNECTED;

        if (_onDisconnected) {
            _onDisconnected();
        }
    }

    if (_autoReconnect && _status == WiFiStatus::DISCONNECTED && _ssid.length() > 0) {
        unsigned long now = millis();
        if (now - _lastReconnectAttempt >= RECONNECT_INTERVAL) {
            attemptReconnect();
        }
    }
#endif
}

void WiFiManager::attemptReconnect() {
    if (_reconnectAttempts >= MAX_RECONNECT_ATTEMPTS) {
        Serial.println("[WiFi] Max reconnect attempts reached");
        _status = WiFiStatus::FAILED;
        if (_onFailed) {
            _onFailed("Max reconnect attempts reached");
        }
        return;
    }

    _reconnectAttempts++;
    _lastReconnectAttempt = millis();

    Serial.printf("[WiFi] Reconnect attempt %d/%d\n", _reconnectAttempts, MAX_RECONNECT_ATTEMPTS);

#ifdef PLATFORM_ESP32
    WiFi.reconnect();
#endif
}

void WiFiManager::onConnected(OnWiFiConnected callback) {
    _onConnected = callback;
}

void WiFiManager::onDisconnected(OnWiFiDisconnected callback) {
    _onDisconnected = callback;
}

void WiFiManager::onFailed(OnWiFiFailed callback) {
    _onFailed = callback;
}
