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
    Serial.printf("[WiFi] Password length: %d chars\n", password.length());

#ifdef PLATFORM_ESP32
    // Ensure clean state
    WiFi.disconnect(true);
    delay(100);
    WiFi.mode(WIFI_STA);

    // Scan for networks to check if target SSID is visible on 2.4GHz
    Serial.println("[WiFi] Scanning for networks...");
    int numNetworks = WiFi.scanNetworks(false, true);  // sync scan, show hidden
    bool targetFound = false;

    Serial.printf("[WiFi] Found %d networks:\n", numNetworks);
    for (int i = 0; i < numNetworks && i < 10; i++) {  // Show max 10
        String foundSSID = WiFi.SSID(i);
        int32_t rssi = WiFi.RSSI(i);
        uint8_t channel = WiFi.channel(i);
        wifi_auth_mode_t authMode = WiFi.encryptionType(i);

        const char* authNames[] = {"OPEN", "WEP", "WPA_PSK", "WPA2_PSK", "WPA_WPA2_PSK",
                                   "WPA2_ENTERPRISE", "WPA3_PSK", "WPA2_WPA3_PSK", "WAPI_PSK"};
        const char* authName = (authMode < 9) ? authNames[authMode] : "UNKNOWN";

        Serial.printf("[WiFi]   %d. \"%s\" (Ch:%d, %s, RSSI:%d dBm)\n",
                      i + 1, foundSSID.c_str(), channel, authName, rssi);

        if (foundSSID == ssid) {
            targetFound = true;
            Serial.printf("[WiFi] >>> Target SSID found! Ch:%d, Auth:%s, Signal:%d dBm\n",
                          channel, authName, rssi);

            if (authMode == WIFI_AUTH_WPA3_PSK) {
                Serial.println("[WiFi] WARNING: WPA3-only detected - may cause issues!");
                Serial.println("[WiFi] Try setting AP to WPA2/WPA3 mixed mode");
            }
        }
    }
    WiFi.scanDelete();

    if (!targetFound) {
        Serial.println("[WiFi] WARNING: Target SSID NOT found in scan!");
        Serial.println("[WiFi] Possible causes:");
        Serial.println("[WiFi]   - AP is on 5GHz (ESP32 only supports 2.4GHz!)");
        Serial.println("[WiFi]   - AP is too far away");
        Serial.println("[WiFi]   - SSID is hidden");
        Serial.println("[WiFi]   - AP is offline");
    }

    WiFi.begin(ssid.c_str(), password.c_str());

    unsigned long start = millis();
    wl_status_t lastStatus = WL_IDLE_STATUS;

    while (WiFi.status() != WL_CONNECTED && (millis() - start) < timeoutMs) {
        wl_status_t currentStatus = WiFi.status();

        // Log status changes
        if (currentStatus != lastStatus) {
            const char* statusNames[] = {
                "IDLE", "NO_SSID_AVAIL", "SCAN_COMPLETED", "CONNECTED",
                "CONNECT_FAILED", "CONNECTION_LOST", "DISCONNECTED"
            };
            if (currentStatus <= WL_DISCONNECTED) {
                Serial.printf("\n[WiFi] Status: %s\n", statusNames[currentStatus]);
            }
            lastStatus = currentStatus;

            // Early exit on definitive failures
            if (currentStatus == WL_NO_SSID_AVAIL) {
                Serial.println("[WiFi] ERROR: SSID not found!");
                Serial.println("[WiFi] Check: Is the AP on 2.4GHz? (ESP32 doesn't support 5GHz)");
                break;
            }
            if (currentStatus == WL_CONNECT_FAILED) {
                Serial.println("[WiFi] ERROR: Connection failed - wrong password?");
                break;
            }
        }

        delay(500);
        Serial.print(".");
    }
    Serial.println();

    if (WiFi.status() == WL_CONNECTED) {
        _status = WiFiStatus::CONNECTED;
        String ip = WiFi.localIP().toString();
        Serial.printf("[WiFi] Connected! IP: %s\n", ip.c_str());
        Serial.printf("[WiFi] RSSI: %d dBm\n", WiFi.RSSI());
        Serial.printf("[WiFi] Channel: %d\n", WiFi.channel());

        if (_onConnected) {
            _onConnected(ip);
        }
        return true;
    } else {
        _status = WiFiStatus::FAILED;
        wl_status_t finalStatus = WiFi.status();

        Serial.println("[WiFi] Connection failed!");
        Serial.printf("[WiFi] Final status code: %d\n", finalStatus);

        // Helpful diagnostics
        switch (finalStatus) {
            case WL_NO_SSID_AVAIL:
                Serial.println("[WiFi] SSID not found - check AP name and 2.4GHz band");
                break;
            case WL_CONNECT_FAILED:
                Serial.println("[WiFi] Auth failed - check password");
                break;
            case WL_IDLE_STATUS:
            case WL_DISCONNECTED:
                Serial.println("[WiFi] Timeout - AP may be too far or overloaded");
                break;
            default:
                Serial.println("[WiFi] Unknown error");
        }

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
