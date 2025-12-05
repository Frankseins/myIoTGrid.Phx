/**
 * myIoTGrid.Sensor - WPS Manager Implementation
 */

#include "wps_manager.h"

#ifdef PLATFORM_ESP32
#include <esp_wifi.h>

// Static instance for callback
WPSManager* WPSManager::_instance = nullptr;

// ESP-IDF WPS configuration
#define ESP_WPS_MODE WPS_TYPE_PBC  // Push Button Configuration
#define ESP_MANUFACTURER "myIoTGrid"
#define ESP_MODEL_NUMBER "Sensor-v1"
#define ESP_MODEL_NAME "myIoTGrid Sensor"
#define ESP_DEVICE_NAME "ESP32-Sensor"
#endif

WPSManager::WPSManager()
    : _status(WPSStatus::IDLE)
    , _startTime(0)
    , _initialized(false) {
    _result.success = false;
}

WPSManager::~WPSManager() {
#ifdef PLATFORM_ESP32
    if (_initialized) {
        stopWPS();
        _instance = nullptr;
    }
#endif
}

bool WPSManager::init() {
#ifdef PLATFORM_ESP32
    _instance = this;

    // Register WiFi event handler
    WiFi.onEvent(wpsEventCallback);

    _initialized = true;
    Serial.println("[WPS] Initialized");
    return true;
#else
    Serial.println("[WPS] WPS not supported on this platform");
    _initialized = true;
    return true;
#endif
}

bool WPSManager::startWPS() {
#ifdef PLATFORM_ESP32
    if (!_initialized) {
        Serial.println("[WPS] Not initialized");
        return false;
    }

    if (_status == WPSStatus::SCANNING || _status == WPSStatus::CONNECTING) {
        Serial.println("[WPS] Already active");
        return false;
    }

    Serial.println("[WPS] Starting WPS (Push Button Configuration)...");
    Serial.println("[WPS] Please press WPS button on your router within 2 minutes");

    // Configure WPS
    esp_wps_config_t config;
    memset(&config, 0, sizeof(config));
    config.wps_type = ESP_WPS_MODE;

    // Set device info
    strncpy(config.factory_info.manufacturer, ESP_MANUFACTURER, sizeof(config.factory_info.manufacturer) - 1);
    strncpy(config.factory_info.model_number, ESP_MODEL_NUMBER, sizeof(config.factory_info.model_number) - 1);
    strncpy(config.factory_info.model_name, ESP_MODEL_NAME, sizeof(config.factory_info.model_name) - 1);
    strncpy(config.factory_info.device_name, ESP_DEVICE_NAME, sizeof(config.factory_info.device_name) - 1);

    // Initialize WiFi in station mode
    WiFi.mode(WIFI_STA);

    // Initialize WPS
    esp_err_t err = esp_wifi_wps_enable(&config);
    if (err != ESP_OK) {
        Serial.printf("[WPS] Failed to enable WPS: %s\n", esp_err_to_name(err));
        _status = WPSStatus::FAILED;
        _result.errorMessage = "Failed to enable WPS";
        return false;
    }

    // Start WPS
    err = esp_wifi_wps_start(0);
    if (err != ESP_OK) {
        Serial.printf("[WPS] Failed to start WPS: %s\n", esp_err_to_name(err));
        esp_wifi_wps_disable();
        _status = WPSStatus::FAILED;
        _result.errorMessage = "Failed to start WPS";
        return false;
    }

    _status = WPSStatus::SCANNING;
    _startTime = millis();
    _result.success = false;
    _result.errorMessage = "";

    Serial.println("[WPS] WPS started successfully");
    Serial.println("[WPS] Status: SCANNING for WPS-enabled access point...");

    return true;
#else
    Serial.println("[WPS] WPS not supported on this platform");
    _status = WPSStatus::FAILED;
    _result.errorMessage = "WPS not supported";
    return false;
#endif
}

void WPSManager::stopWPS() {
#ifdef PLATFORM_ESP32
    if (_status == WPSStatus::SCANNING || _status == WPSStatus::CONNECTING) {
        Serial.println("[WPS] Stopping WPS...");
        esp_wifi_wps_disable();
    }
#endif
    _status = WPSStatus::IDLE;
}

WPSStatus WPSManager::getStatus() const {
    return _status;
}

bool WPSManager::isActive() const {
    return _status == WPSStatus::SCANNING || _status == WPSStatus::CONNECTING;
}

void WPSManager::loop() {
    // Check for timeout
    if (isActive() && (millis() - _startTime >= WPS_TIMEOUT_MS)) {
        Serial.println("[WPS] Timeout - no WPS response received");
        stopWPS();
        _status = WPSStatus::TIMEOUT;
        _result.success = false;
        _result.errorMessage = "WPS timeout - no response from router";

        if (_onTimeout) {
            _onTimeout();
        }
    }
}

WPSResult WPSManager::getResult() const {
    return _result;
}

void WPSManager::onSuccess(OnWPSSuccess callback) {
    _onSuccess = callback;
}

void WPSManager::onFailed(OnWPSFailed callback) {
    _onFailed = callback;
}

void WPSManager::onTimeout(OnWPSTimeout callback) {
    _onTimeout = callback;
}

const char* WPSManager::getStatusName(WPSStatus status) {
    switch (status) {
        case WPSStatus::IDLE:       return "IDLE";
        case WPSStatus::SCANNING:   return "SCANNING";
        case WPSStatus::CONNECTING: return "CONNECTING";
        case WPSStatus::SUCCESS:    return "SUCCESS";
        case WPSStatus::TIMEOUT:    return "TIMEOUT";
        case WPSStatus::FAILED:     return "FAILED";
        default:                    return "UNKNOWN";
    }
}

// ============================================================================
// ESP32 Platform Implementation
// ============================================================================

#ifdef PLATFORM_ESP32

void WPSManager::wpsEventCallback(arduino_event_id_t event, arduino_event_info_t info) {
    if (_instance) {
        _instance->handleWPSEvent(event, info);
    }
}

void WPSManager::handleWPSEvent(arduino_event_id_t event, arduino_event_info_t info) {
    switch (event) {
        case ARDUINO_EVENT_WPS_ER_SUCCESS:
            Serial.println("[WPS] SUCCESS - Credentials received!");
            _status = WPSStatus::CONNECTING;

            // Extract credentials
            {
                wifi_config_t conf;
                esp_wifi_get_config(WIFI_IF_STA, &conf);

                _result.ssid = String((char*)conf.sta.ssid);
                _result.password = String((char*)conf.sta.password);
                _result.success = true;
                _result.errorMessage = "";

                Serial.printf("[WPS] SSID: %s\n", _result.ssid.c_str());
                Serial.println("[WPS] Password: ******* (hidden)");
            }

            // Disable WPS first
            esp_wifi_wps_disable();

            // Wait a moment for clean state transition
            Serial.println("[WPS] Waiting for WiFi subsystem to stabilize...");
            delay(1000);

            // Reset WiFi completely before connecting
            WiFi.disconnect(true);
            delay(100);
            WiFi.mode(WIFI_STA);
            delay(100);

            // Connect to WiFi with received credentials
            Serial.println("[WPS] Connecting to WiFi...");
            Serial.printf("[WPS] Using SSID: %s, Password length: %d\n",
                         _result.ssid.c_str(), _result.password.length());
            WiFi.begin(_result.ssid.c_str(), _result.password.c_str());
            _connectRetryCount = 0;
            break;

        case ARDUINO_EVENT_WPS_ER_FAILED:
            Serial.println("[WPS] FAILED - WPS negotiation failed");
            esp_wifi_wps_disable();
            _status = WPSStatus::FAILED;
            _result.success = false;
            _result.errorMessage = "WPS negotiation failed";

            if (_onFailed) {
                _onFailed("WPS negotiation failed");
            }
            break;

        case ARDUINO_EVENT_WPS_ER_TIMEOUT:
            Serial.println("[WPS] TIMEOUT - No response from router");
            esp_wifi_wps_disable();
            _status = WPSStatus::TIMEOUT;
            _result.success = false;
            _result.errorMessage = "WPS timeout";

            if (_onTimeout) {
                _onTimeout();
            }
            break;

        case ARDUINO_EVENT_WPS_ER_PIN:
            Serial.println("[WPS] PIN mode detected - not supported");
            Serial.println("[WPS] Please use Push Button Configuration (PBC)");
            break;

        case ARDUINO_EVENT_WPS_ER_PBC_OVERLAP:
            Serial.println("[WPS] PBC Overlap - Multiple WPS devices detected");
            Serial.println("[WPS] Please try again when only your router has WPS active");
            esp_wifi_wps_disable();
            _status = WPSStatus::FAILED;
            _result.success = false;
            _result.errorMessage = "PBC overlap - multiple WPS devices detected";

            if (_onFailed) {
                _onFailed("PBC overlap - multiple WPS devices");
            }
            break;

        case ARDUINO_EVENT_WIFI_STA_GOT_IP:
            if (_status == WPSStatus::CONNECTING) {
                Serial.println("[WPS] Successfully connected to WiFi!");
                Serial.printf("[WPS] IP Address: %s\n", WiFi.localIP().toString().c_str());
                _status = WPSStatus::SUCCESS;

                if (_onSuccess) {
                    _onSuccess(_result.ssid, _result.password);
                }
            }
            break;

        case ARDUINO_EVENT_WIFI_STA_DISCONNECTED:
            if (_status == WPSStatus::CONNECTING) {
                // Get detailed disconnect reason
                uint8_t reason = info.wifi_sta_disconnected.reason;
                Serial.printf("[WPS] WiFi connection failed! Reason code: %d\n", reason);

                // Decode common reason codes
                const char* reasonStr;
                switch (reason) {
                    case 1:  reasonStr = "UNSPECIFIED"; break;
                    case 2:  reasonStr = "AUTH_EXPIRE - Authentication expired"; break;
                    case 3:  reasonStr = "AUTH_LEAVE - Deauthenticated (leaving)"; break;
                    case 4:  reasonStr = "ASSOC_EXPIRE - Association expired"; break;
                    case 6:  reasonStr = "NOT_AUTHED - Class 2 frame from non-authenticated STA"; break;
                    case 7:  reasonStr = "NOT_ASSOCED - Class 3 frame from non-associated STA"; break;
                    case 8:  reasonStr = "ASSOC_LEAVE - Disassociated (leaving BSS)"; break;
                    case 14: reasonStr = "MIC_FAILURE - Message integrity code failure (wrong password?)"; break;
                    case 15: reasonStr = "4WAY_HANDSHAKE_TIMEOUT - 4-way handshake timeout (wrong password or encryption mismatch)"; break;
                    case 16: reasonStr = "GROUP_KEY_UPDATE_TIMEOUT"; break;
                    case 17: reasonStr = "IE_IN_4WAY_DIFFERS - Information element mismatch"; break;
                    case 18: reasonStr = "GROUP_CIPHER_INVALID - Invalid group cipher"; break;
                    case 19: reasonStr = "PAIRWISE_CIPHER_INVALID - Invalid pairwise cipher"; break;
                    case 20: reasonStr = "AKMP_INVALID - Invalid AKMP"; break;
                    case 23: reasonStr = "IEEE802_1X_AUTH_FAILED - 802.1X auth failed"; break;
                    case 24: reasonStr = "CIPHER_SUITE_REJECTED - Cipher suite rejected"; break;
                    case 200: reasonStr = "BEACON_TIMEOUT - Access point not responding"; break;
                    case 201: reasonStr = "NO_AP_FOUND - Access point not found"; break;
                    case 202: reasonStr = "AUTH_FAIL - Authentication failed (wrong password)"; break;
                    case 203: reasonStr = "ASSOC_FAIL - Association failed"; break;
                    case 204: reasonStr = "HANDSHAKE_TIMEOUT - Handshake timeout"; break;
                    case 205: reasonStr = "CONNECTION_FAIL - Connection failed"; break;
                    case 206: reasonStr = "AP_TSF_RESET"; break;
                    case 207: reasonStr = "ROAMING - Roaming"; break;
                    default: reasonStr = "UNKNOWN"; break;
                }
                Serial.printf("[WPS] Disconnect reason: %s\n", reasonStr);

                // Log additional debug info
                Serial.printf("[WPS] SSID attempted: %s\n", _result.ssid.c_str());
                Serial.printf("[WPS] Password length: %d chars\n", _result.password.length());

                // Retry connection for certain errors
                _connectRetryCount++;
                bool shouldRetry = (reason == 8 || reason == 2 || reason == 4 || reason == 200)
                                   && _connectRetryCount < MAX_CONNECT_RETRIES;

                if (shouldRetry) {
                    Serial.printf("[WPS] Retrying connection (%d/%d)...\n",
                                 _connectRetryCount, MAX_CONNECT_RETRIES);
                    delay(2000);  // Wait 2 seconds before retry
                    WiFi.disconnect(true);
                    delay(100);
                    WiFi.mode(WIFI_STA);
                    delay(100);
                    WiFi.begin(_result.ssid.c_str(), _result.password.c_str());
                } else {
                    _status = WPSStatus::FAILED;
                    _result.success = false;
                    char errorMsg[128];
                    snprintf(errorMsg, sizeof(errorMsg), "WiFi failed: %s (code %d)", reasonStr, reason);
                    _result.errorMessage = String(errorMsg);

                    if (_onFailed) {
                        _onFailed(_result.errorMessage.c_str());
                    }
                }
            }
            break;

        default:
            // Ignore other events
            break;
    }
}

#endif // PLATFORM_ESP32
