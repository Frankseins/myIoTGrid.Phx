/**
 * myIoTGrid.Sensor - API Client Implementation
 */

#include "api_client.h"
#include <ArduinoJson.h>

#ifdef PLATFORM_ESP32
#include <HTTPClient.h>
#include <WiFiClientSecure.h>
#endif

ApiClient::ApiClient()
    : _timeout(10000)
    , _configured(false) {
}

void ApiClient::configure(const String& baseUrl, const String& nodeId, const String& apiKey) {
    _baseUrl = baseUrl;
    _nodeId = nodeId;
    _apiKey = apiKey;
    _configured = true;

    Serial.printf("[API] Configured: URL=%s, NodeID=%s\n", baseUrl.c_str(), nodeId.c_str());
}

bool ApiClient::isConfigured() const {
    return _configured;
}

bool ApiClient::validateApiKey() {
    if (!_configured) {
        Serial.println("[API] Not configured");
        return false;
    }

    String path = "/api/nodes/validate/" + _nodeId;
    ApiResponse response = httpGet(path);

    if (response.success && response.statusCode == 200) {
        Serial.println("[API] API key validated successfully");
        return true;
    } else {
        Serial.printf("[API] API key validation failed: %d - %s\n",
                      response.statusCode, response.error.c_str());
        return false;
    }
}

HeartbeatResponse ApiClient::sendHeartbeat(const String& firmwareVersion, int batteryLevel) {
    HeartbeatResponse result;
    result.success = false;
    result.nextHeartbeatSeconds = 60;

    if (!_configured) {
        Serial.println("[API] Not configured");
        return result;
    }

    JsonDocument doc;
    doc["nodeId"] = _nodeId;
    if (firmwareVersion.length() > 0) {
        doc["firmwareVersion"] = firmwareVersion;
    }
    if (batteryLevel >= 0) {
        doc["batteryLevel"] = batteryLevel;
    }

    String body;
    serializeJson(doc, body);

    ApiResponse response = httpPost("/api/nodes/heartbeat", body);

    if (response.success && response.statusCode == 200) {
        JsonDocument respDoc;
        DeserializationError error = deserializeJson(respDoc, response.body);

        if (!error) {
            result.success = respDoc["success"] | false;
            result.serverTime = respDoc["serverTime"] | 0;
            result.nextHeartbeatSeconds = respDoc["nextHeartbeatSeconds"] | 60;
        }

        Serial.printf("[API] Heartbeat sent, next in %d seconds\n", result.nextHeartbeatSeconds);
    } else {
        Serial.printf("[API] Heartbeat failed: %d - %s\n",
                      response.statusCode, response.error.c_str());
    }

    return result;
}

bool ApiClient::sendReading(const String& sensorType, double value, const String& unit) {
    if (!_configured) {
        return false;
    }

    JsonDocument doc;
    doc["nodeId"] = _nodeId;
    doc["sensorType"] = sensorType;
    doc["value"] = value;
    if (unit.length() > 0) {
        doc["unit"] = unit;
    }

    String body;
    serializeJson(doc, body);

    ApiResponse response = httpPost("/api/readings", body);

    if (response.success && response.statusCode == 201) {
        Serial.printf("[API] Reading sent: %s = %.2f %s\n",
                      sensorType.c_str(), value, unit.c_str());
        return true;
    } else {
        Serial.printf("[API] Failed to send reading: %d\n", response.statusCode);
        return false;
    }
}

bool ApiClient::sendReadings(const String& readingsJson) {
    if (!_configured) {
        return false;
    }

    ApiResponse response = httpPost("/api/readings/batch", readingsJson);
    return response.success && response.statusCode == 200;
}

String ApiClient::buildUrl(const String& path) const {
    String url = _baseUrl;
    if (!url.endsWith("/") && !path.startsWith("/")) {
        url += "/";
    }
    url += path;
    return url;
}

ApiResponse ApiClient::httpGet(const String& path) {
    ApiResponse result;

#ifdef PLATFORM_ESP32
    HTTPClient http;
    String url = buildUrl(path);

    Serial.printf("[API] GET %s\n", url.c_str());

    http.begin(url);
    http.setTimeout(_timeout);
    http.addHeader("Authorization", "Bearer " + _apiKey);
    http.addHeader("Content-Type", "application/json");

    int httpCode = http.GET();
    result.statusCode = httpCode;

    if (httpCode > 0) {
        result.body = http.getString();
        result.success = (httpCode >= 200 && httpCode < 300);
    } else {
        result.error = http.errorToString(httpCode);
        result.success = false;
    }

    http.end();
#else
    // Simulation
    Serial.printf("[API] Simulated GET %s\n", path.c_str());
    result.statusCode = 200;
    result.success = true;
    result.body = "{}";
#endif

    return result;
}

ApiResponse ApiClient::httpPost(const String& path, const String& body) {
    ApiResponse result;

#ifdef PLATFORM_ESP32
    HTTPClient http;
    String url = buildUrl(path);

    Serial.printf("[API] POST %s: %s\n", url.c_str(), body.c_str());

    http.begin(url);
    http.setTimeout(_timeout);
    http.addHeader("Authorization", "Bearer " + _apiKey);
    http.addHeader("Content-Type", "application/json");

    int httpCode = http.POST(body);
    result.statusCode = httpCode;

    if (httpCode > 0) {
        result.body = http.getString();
        result.success = (httpCode >= 200 && httpCode < 300);
    } else {
        result.error = http.errorToString(httpCode);
        result.success = false;
    }

    http.end();
#else
    // Simulation
    Serial.printf("[API] Simulated POST %s: %s\n", path.c_str(), body.c_str());
    result.statusCode = 200;
    result.success = true;
    result.body = "{\"success\":true,\"serverTime\":0,\"nextHeartbeatSeconds\":60}";
#endif

    return result;
}
