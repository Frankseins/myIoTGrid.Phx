/**
 * myIoTGrid.Sensor - Debug Log Uploader Implementation
 * Sprint 8: Remote Debug System - Serial Monitor Mode
 *
 * Captures ALL Serial output and sends it to the Hub API
 * as a remote serial monitor.
 */

#include "debug_log_uploader.h"
#include "serial_capture.h"
#include <ArduinoJson.h>

#ifdef PLATFORM_ESP32
#include <HTTPClient.h>
#include <WiFi.h>
#include <WiFiClientSecure.h>
#include <WiFiClient.h>

// ISRG Root X1 - Let's Encrypt Root CA (valid until 2035)
extern const char* rootCACertificate;  // Defined in api_client.cpp
#endif

// Singleton instance
DebugLogUploader& DebugLogUploader::getInstance() {
    static DebugLogUploader instance;
    return instance;
}

DebugLogUploader::DebugLogUploader()
    : _enabled(false)
    , _initialized(false)
    , _lastUploadTime(0)
    , _currentRetry(0) {
}

void DebugLogUploader::begin(const String& baseUrl, const String& serialNumber) {
    _baseUrl = baseUrl;
    _serialNumber = serialNumber;
    _initialized = true;
    _enabled = true;
    _lastUploadTime = millis();

    // Initialize SerialCapture for remote serial monitor
    SerialCapture::getInstance().begin();
    SerialCapture::getInstance().setEnabled(true);

    // Note: We don't use DBG_* macros here to avoid recursive capture
    Serial.printf("[RemoteSerial] Initialized for %s\n", serialNumber.c_str());
}

void DebugLogUploader::configure(const DebugLogUploaderConfig& config) {
    _config = config;
}

void DebugLogUploader::queueLog(const LogEntry& entry) {
    // Legacy method - no longer used in serial monitor mode
    (void)entry;
}

void DebugLogUploader::loop() {
    if (!_enabled || !_initialized) return;

    unsigned long now = millis();
    unsigned long elapsed = now - _lastUploadTime;

    // Check if upload needed
    if (elapsed >= _config.uploadIntervalMs) {
        SerialCapture& capture = SerialCapture::getInstance();

        if (capture.hasData()) {
            uploadSerialLines();
        }

        _lastUploadTime = now;
    }
}

bool DebugLogUploader::uploadNow() {
    if (!_initialized) return true;

#ifdef PLATFORM_ESP32
    if (WiFi.status() != WL_CONNECTED) {
        return false;
    }

    SerialCapture& capture = SerialCapture::getInstance();
    if (!capture.hasData()) {
        return true;  // Nothing to upload
    }

    return uploadSerialLines();
#else
    return true;
#endif
}

bool DebugLogUploader::uploadSerialLines() {
#ifdef PLATFORM_ESP32
    HTTPClient http;

    String url = _baseUrl + "/api/node-debug/serial-output";
    bool isHttps = url.startsWith("https://");

    // Handle HTTPS vs HTTP connections
    if (isHttps) {
        WiFiClientSecure secureClient;
        secureClient.setCACert(rootCACertificate);  // Use Root CA for validation
        http.begin(secureClient, url);
    } else {
        WiFiClient plainClient;
        http.begin(plainClient, url);
    }

    http.setTimeout(10000);
    http.addHeader("Content-Type", "application/json");

    if (_apiKey.length() > 0) {
        http.addHeader("Authorization", "Bearer " + _apiKey);
    }

    // Get captured serial lines
    std::vector<String> lines = SerialCapture::getInstance().getAndClearLines();

    if (lines.empty()) {
        http.end();
        return true;
    }

    // Build payload with raw lines
    JsonDocument doc;
    doc["serialNumber"] = _serialNumber;
    doc["timestamp"] = millis();

    JsonArray linesArray = doc["lines"].to<JsonArray>();
    int count = 0;
    for (const auto& line : lines) {
        if (count >= _config.batchSize) break;
        linesArray.add(line);
        count++;
    }

    String payload;
    serializeJson(doc, payload);

    _stats.uploadAttempts++;

    int httpCode = http.POST(payload);
    bool success = (httpCode >= 200 && httpCode < 300);

    if (success) {
        _stats.entriesUploaded += count;
        _stats.lastUploadTime = millis();
        _currentRetry = 0;
    } else {
        _stats.uploadFailures++;
        _currentRetry++;

        // On failure, lines are lost (they were already cleared from capture)
        // This is acceptable for a serial monitor - we prioritize not blocking
        _stats.entriesDropped += count;

        if (_currentRetry >= _config.maxRetries) {
            _currentRetry = 0;
        }
    }

    http.end();
    return success;
#else
    // Native simulation
    SerialCapture::getInstance().getAndClearLines();
    return true;
#endif
}

bool DebugLogUploader::uploadBatch() {
    // Legacy method - replaced by uploadSerialLines()
    return uploadSerialLines();
}

String DebugLogUploader::buildUploadPayload() {
    // Legacy method - no longer used
    return "{}";
}

void DebugLogUploader::clearQueue() {
    SerialCapture::getInstance().getAndClearLines();
    Serial.println("[RemoteSerial] Buffer cleared");
}
