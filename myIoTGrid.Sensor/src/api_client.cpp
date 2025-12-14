/**
 * myIoTGrid.Sensor - API Client Implementation
 */

#include "api_client.h"
#include "config.h"
#include <ArduinoJson.h>
#include <vector>
#ifdef PLATFORM_NATIVE
#include "ArduinoJsonString.h"
#include <curl/curl.h>
#include <cstdlib>
#endif

#ifdef PLATFORM_ESP32
#include <HTTPClient.h>
#include <WiFiClientSecure.h>
#include <WiFiClient.h>

// DigiCert Global Root G2 - Used by Azure/GeoTrust certificates
// Valid until 2038 - for api.myiotgrid.cloud (Azure App Service)
const char* rootCACertificate = R"(
-----BEGIN CERTIFICATE-----
MIIDjjCCAnagAwIBAgIQAzrx5qcRqaC7KGSxHQn65TANBgkqhkiG9w0BAQsFADBh
MQswCQYDVQQGEwJVUzEVMBMGA1UEChMMRGlnaUNlcnQgSW5jMRkwFwYDVQQLExB3
d3cuZGlnaWNlcnQuY29tMSAwHgYDVQQDExdEaWdpQ2VydCBHbG9iYWwgUm9vdCBH
MjAeFw0xMzA4MDExMjAwMDBaFw0zODAxMTUxMjAwMDBaMGExCzAJBgNVBAYTAlVT
MRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5j
b20xIDAeBgNVBAMTF0RpZ2lDZXJ0IEdsb2JhbCBSb290IEcyMIIBIjANBgkqhkiG
9w0BAQEFAAOCAQ8AMIIBCgKCAQEAuzfNNNx7a8myaJCtSnX/RrohCgiN9RlUyfuI
2/Ou8jqJkTx65qsGGmvPrC3oXgkkRLpimn7Wo6h+4FR1IAWsULecYxpsMNzaHxmx
1x7e/dfgy5SDN67sH0NO3Xss0r0upS/kqbitOtSZpLYl6ZtrAGCSYP9PIUkY92eQ
q2EGnI/yuum06ZIya7XzV+hdG82MHauVBJVJ8zUtluNJbd134/tJS7SsVQepj5Wz
tCO7TG1F8PapspUwtP1MVYwnSlcUfIKdzXOS0xZKBgyMUNGPHgm+F6HmIcr9g+UQ
vIOlCsRnKPZzFBQ9RnbDhxSJITRNrw9FDKZJobq7nMWxM4MphQIDAQABo0IwQDAP
BgNVHRMBAf8EBTADAQH/MA4GA1UdDwEB/wQEAwIBhjAdBgNVHQ4EFgQUTiJUIBiV
5uNu5g/6+rkS7QYXjzkwDQYJKoZIhvcNAQELBQADggEBAGBnKJRvDkhj6zHd6mcY
1Yl9PMCcit5Lp1SBDl1VNhm8hNVg3U2CmK2mAEUeQ1SqAqAzVUKvsgaeLdf2dZWM
yDbBH9bgoIJlrH1mV2THASmYPmfEZeAenSCHnEpy0/dT2SYQH1N0QeW5gnKIDkK0
p5us1rOjqBvjEQFteniobhELUaKkSGL5MHg8A1g9HCPEN1f5LCNN9nvSHLKvX7hj
7KqA0GCghBrzPCPC/ynJmr2VFNL+s1v8DMLE7gUn9K5+y/fGgJNPKOr/bAtvMXZN
udN6eKfBQGvBPw3zfMxG7YBsHSQwGTtXhfcmj/JnYvHzGA0EPdv6Y4UeQvpjMB/X
quk=
-----END CERTIFICATE-----
)";
#endif

#ifdef PLATFORM_NATIVE
// Callback for libcurl to write response data
static size_t WriteCallback(void* contents, size_t size, size_t nmemb, std::string* userp) {
    size_t totalSize = size * nmemb;
    userp->append((char*)contents, totalSize);
    return totalSize;
}
#endif

ApiClient::ApiClient()
    : _timeout(config::HTTP_TIMEOUT_MS)  // Use config value (30s for HTTPS/TLS)
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

RegistrationResponse ApiClient::registerNode(const String& serialNumber,
                                              const String& firmwareVersion,
                                              const String& hardwareType,
                                              const std::vector<String>& capabilities) {
    RegistrationResponse result;
    result.success = false;
    result.intervalSeconds = 60;

    if (_baseUrl.length() == 0) {
        Serial.println("[API] Base URL not set for registration");
        result.error = "Base URL not configured";
        return result;
    }

    // Build registration JSON
    JsonDocument doc;
    doc["serialNumber"] = serialNumber;
    if (firmwareVersion.length() > 0) {
        doc["firmwareVersion"] = firmwareVersion;
    }
    if (hardwareType.length() > 0) {
        doc["hardwareType"] = hardwareType;
    }
    if (!capabilities.empty()) {
        JsonArray capsArray = doc["capabilities"].to<JsonArray>();
        for (const auto& cap : capabilities) {
            capsArray.add(cap);
        }
    }

    String body;
    serializeJson(doc, body);

    Serial.printf("[API] Registering node: %s\n", serialNumber.c_str());

    ApiResponse response = httpPost("/api/Nodes/register", body);

    if (response.success && response.statusCode == 200) {
        JsonDocument respDoc;
        DeserializationError error = deserializeJson(respDoc, response.body);

        if (!error) {
            result.success = true;
            result.nodeId = respDoc["nodeId"].as<String>();
            result.serialNumber = respDoc["serialNumber"].as<String>();
            result.name = respDoc["name"].as<String>();
            result.location = respDoc["location"].as<String>();
            result.intervalSeconds = respDoc["intervalSeconds"] | 60;
            result.isNewNode = respDoc["isNewNode"] | false;
            result.message = respDoc["message"].as<String>();

            // Get connection endpoint
            if (respDoc["connection"].is<JsonObject>()) {
                result.connectionEndpoint = respDoc["connection"]["endpoint"].as<String>();
            }

            Serial.printf("[API] Registration successful: %s (%s)\n",
                          result.name.c_str(), result.isNewNode ? "new" : "existing");
        } else {
            result.error = "Failed to parse response";
            Serial.printf("[API] JSON parse error: %s\n", error.c_str());
        }
    } else {
        result.error = response.error.length() > 0 ? response.error : "Registration failed";
        Serial.printf("[API] Registration failed: %d - %s\n",
                      response.statusCode, result.error.c_str());
    }

    return result;
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

bool ApiClient::sendReading(const String& sensorType, double value, const String& unit, int endpointId) {
    if (!_configured) {
        return false;
    }

    // Backend expects CreateSensorReadingDto:
    // { DeviceId, Type, Value, Unit?, Timestamp?, EndpointId? }
    JsonDocument doc;
    doc["deviceId"] = _nodeId;    // SerialNumber (e.g., SIM-8F470D6C-0001)
    doc["type"] = sensorType;     // Measurement type (e.g., temperature, humidity)
    if (sensorType.indexOf("lat") >= 0 || sensorType.indexOf("lon") >= 0) {
      doc["value"] = String(value, 6);  // as string with 6 decimals
    } else {
      doc["value"] = value;
    }
    if (unit.length() > 0) {
        doc["unit"] = unit;
    }
    if (endpointId >= 0) {
        doc["endpointId"] = endpointId;  // Identifies which sensor assignment this reading belongs to
    }

    String body;
    serializeJson(doc, body);

    ApiResponse response = httpPost("/api/readings", body);

    if (response.success && response.statusCode == 201) {
        Serial.printf("[API] Reading sent: %s = %.2f %s\n",
                      sensorType.c_str(), value, unit.c_str());
        return true;
    } else {
        Serial.printf("[API] Failed to send reading: %d - %s\n",
                      response.statusCode, response.body.c_str());
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

bool ApiClient::sendHardwareStatus(const String& serialNumber,
                                    const String& firmwareVersion,
                                    const String& hardwareType,
                                    const String& detectedDevicesJson,
                                    const String& storageJson,
                                    const String& busStatusJson) {
    if (_baseUrl.length() == 0) {
        Serial.println("[API] Base URL not set for hardware status report");
        return false;
    }

    // Build the complete JSON body matching ReportHardwareStatusDto
    String body = "{";
    body += "\"serialNumber\":\"" + serialNumber + "\",";
    body += "\"firmwareVersion\":\"" + firmwareVersion + "\",";
    body += "\"hardwareType\":\"" + hardwareType + "\",";
    body += "\"detectedDevices\":" + detectedDevicesJson + ",";
    body += "\"storage\":" + storageJson + ",";
    body += "\"busStatus\":" + busStatusJson;
    body += "}";

    Serial.println("[API] Sending hardware status report...");

    ApiResponse response = httpPost("/api/node-debug/hardware-status", body);

    if (response.success && response.statusCode == 200) {
        Serial.println("[API] Hardware status report sent successfully");
        return true;
    } else {
        Serial.printf("[API] Hardware status report failed: %d - %s\n",
                      response.statusCode, response.error.c_str());
        return false;
    }
}

NodeConfigurationResponse ApiClient::fetchConfiguration(const String& serialNumber) {
    NodeConfigurationResponse result;
    result.success = false;
    result.defaultIntervalSeconds = 60;

    if (_baseUrl.length() == 0) {
        Serial.println("[API] Base URL not set for configuration fetch");
        result.error = "Base URL not configured";
        return result;
    }

    String path = "/api/nodes/" + serialNumber + "/configuration";
    Serial.printf("[API] Fetching configuration for: %s\n", serialNumber.c_str());

    ApiResponse response = httpGet(path);

    if (response.success && response.statusCode == 200) {
        JsonDocument respDoc;
        DeserializationError error = deserializeJson(respDoc, response.body);

        if (!error) {
            result.success = true;
            result.nodeId = respDoc["nodeId"].as<String>();
            result.serialNumber = respDoc["serialNumber"].as<String>();
            result.name = respDoc["name"].as<String>();
            result.isSimulation = respDoc["isSimulation"] | false;
            result.defaultIntervalSeconds = respDoc["defaultIntervalSeconds"] | 60;

            // Sprint OS-01: Parse storageMode from API
            // Default: LOCAL_AUTOSYNC (3) - store locally and sync when possible
            result.storageMode = respDoc["storageMode"] | 3;

            // Parse sensors array
            JsonArray sensorsArray = respDoc["sensors"].as<JsonArray>();
            for (JsonObject sensorObj : sensorsArray) {
                SensorAssignmentConfig sensor;
                sensor.endpointId = sensorObj["endpointId"] | 0;
                sensor.sensorCode = sensorObj["sensorCode"].as<String>();
                sensor.sensorName = sensorObj["sensorName"].as<String>();
                sensor.icon = sensorObj["icon"].as<String>();
                sensor.color = sensorObj["color"].as<String>();
                sensor.isActive = sensorObj["isActive"] | true;
                sensor.intervalSeconds = sensorObj["intervalSeconds"] | 60;
                sensor.i2cAddress = sensorObj["i2CAddress"].as<String>();
                sensor.sdaPin = sensorObj["sdaPin"] | -1;
                sensor.sclPin = sensorObj["sclPin"] | -1;
                sensor.oneWirePin = sensorObj["oneWirePin"] | -1;
                sensor.analogPin = sensorObj["analogPin"] | -1;
                sensor.digitalPin = sensorObj["digitalPin"] | -1;
                sensor.triggerPin = sensorObj["triggerPin"] | -1;
                sensor.echoPin = sensorObj["echoPin"] | -1;
                sensor.baudRate = sensorObj["baudRate"] | -1;
                sensor.offsetCorrection = sensorObj["offsetCorrection"] | 0.0;
                sensor.gainCorrection = sensorObj["gainCorrection"] | 1.0;

                // Parse capabilities array
                JsonArray capsArray = sensorObj["capabilities"].as<JsonArray>();
                for (JsonObject capObj : capsArray) {
                    SensorCapabilityConfig cap;
                    cap.measurementType = capObj["measurementType"].as<String>();
                    cap.displayName = capObj["displayName"].as<String>();
                    cap.unit = capObj["unit"].as<String>();
                    sensor.capabilities.push_back(cap);
                }

                result.sensors.push_back(sensor);
            }

            // Sprint OS-01: Log storage mode
            const char* storageModeNames[] = {"RemoteOnly", "LocalAndRemote", "LocalOnly", "LocalAutoSync"};
            const char* storageModeName = (result.storageMode >= 0 && result.storageMode <= 3)
                                          ? storageModeNames[result.storageMode] : "Unknown";
            Serial.printf("[API] Configuration loaded: %d sensors, StorageMode=%s (%d)\n",
                          (int)result.sensors.size(), storageModeName, result.storageMode);

            for (const auto& s : result.sensors) {
                Serial.printf("[API]   - %s (%s): Endpoint %d, Interval %ds\n",
                              s.sensorName.c_str(), s.sensorCode.c_str(),
                              s.endpointId, s.intervalSeconds);
            }
        } else {
            result.error = "Failed to parse configuration response";
            Serial.printf("[API] JSON parse error: %s\n", error.c_str());
        }
    } else if (response.statusCode == 404) {
        // Node not found or no configuration - this is OK, node might not be configured yet
        result.error = "No configuration found";
        Serial.println("[API] No configuration found for this node (not configured yet)");
    } else {
        result.error = response.error.length() > 0 ? response.error : "Failed to fetch configuration";
        Serial.printf("[API] Configuration fetch failed: %d - %s\n",
                      response.statusCode, result.error.c_str());
    }

    return result;
}

DebugConfigurationResponse ApiClient::fetchDebugConfiguration(const String& serialNumber) {
    DebugConfigurationResponse result;
    result.success = false;
    result.debugLevel = 1;  // Default: Normal
    result.enableRemoteLogging = false;

    if (_baseUrl.length() == 0) {
        Serial.println("[API] Base URL not set for debug config fetch");
        result.error = "Base URL not configured";
        return result;
    }

    String path = "/api/nodes/" + serialNumber + "/debug";
    Serial.printf("[API] Fetching debug configuration for: %s\n", serialNumber.c_str());

    ApiResponse response = httpGet(path);

    if (response.success && response.statusCode == 200) {
        JsonDocument respDoc;
        DeserializationError error = deserializeJson(respDoc, response.body);

        if (!error) {
            result.success = true;
            result.nodeId = respDoc["nodeId"].as<String>();

            // Parse debugLevel - can be string ("Production", "Normal", "Debug") or int (0, 1, 2)
            JsonVariant levelVar = respDoc["debugLevel"];
            if (levelVar.is<const char*>()) {
                String levelStr = levelVar.as<String>();
                if (levelStr == "Production" || levelStr == "production") {
                    result.debugLevel = 0;
                } else if (levelStr == "Normal" || levelStr == "normal") {
                    result.debugLevel = 1;
                } else if (levelStr == "Debug" || levelStr == "debug") {
                    result.debugLevel = 2;
                }
            } else {
                result.debugLevel = levelVar | 1;
            }

            result.enableRemoteLogging = respDoc["enableRemoteLogging"] | false;
            result.lastDebugChange = respDoc["lastDebugChange"].as<String>();

            const char* levelNames[] = {"Production", "Normal", "Debug"};
            const char* levelName = (result.debugLevel >= 0 && result.debugLevel <= 2)
                                    ? levelNames[result.debugLevel] : "Unknown";
            Serial.printf("[API] Debug config loaded: Level=%s (%d), RemoteLogging=%s\n",
                          levelName, result.debugLevel,
                          result.enableRemoteLogging ? "enabled" : "disabled");
        } else {
            result.error = "Failed to parse debug configuration response";
            Serial.printf("[API] JSON parse error: %s\n", error.c_str());
        }
    } else if (response.statusCode == 404) {
        // Node not found - use defaults
        result.error = "No debug configuration found";
        Serial.println("[API] No debug configuration found (using defaults)");
    } else {
        result.error = response.error.length() > 0 ? response.error : "Failed to fetch debug configuration";
        Serial.printf("[API] Debug config fetch failed: %d - %s\n",
                      response.statusCode, result.error.c_str());
    }

    return result;
}

TimeResponse ApiClient::fetchTime() {
    TimeResponse result;
    result.success = false;
    result.unixTimestamp = 0;

    if (_baseUrl.length() == 0) {
        result.error = "Base URL not configured";
        return result;
    }

    String path = "/api/time";
    Serial.println("[API] Fetching time from Hub...");

    ApiResponse response = httpGet(path);

    if (response.success && response.statusCode == 200) {
        JsonDocument doc;
        DeserializationError error = deserializeJson(doc, response.body);

        if (!error) {
            result.success = true;
            result.unixTimestamp = doc["unixTimestamp"] | 0L;
            Serial.printf("[API] Hub time: %ld (Unix timestamp)\n", result.unixTimestamp);
        } else {
            result.error = "Failed to parse time response";
            Serial.printf("[API] Time JSON parse error: %s\n", error.c_str());
        }
    } else {
        result.error = response.error.length() > 0 ? response.error : "Failed to fetch time";
        Serial.printf("[API] Time fetch failed: %d - %s\n", response.statusCode, result.error.c_str());
    }

    return result;
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
    String url = buildUrl(path);
    Serial.printf("[API] GET %s\n", url.c_str());

    HTTPClient http;
    bool isHttps = url.startsWith("https://");

    if (isHttps) {
        WiFiClientSecure secureClient;
        secureClient.setInsecure();  // Skip certificate validation
        http.begin(secureClient, url);
    } else {
        WiFiClient plainClient;
        http.begin(plainClient, url);
    }

    http.setTimeout(_timeout);
    http.addHeader("Authorization", "Bearer " + _apiKey);
    http.addHeader("Content-Type", "application/json");

    Serial.printf("[API] GET request (timeout: %d ms)...\n", _timeout);
    unsigned long requestStart = millis();

    int httpCode = http.GET();
    unsigned long requestTime = millis() - requestStart;
    Serial.printf("[API] Response: HTTP %d (%lu ms)\n", httpCode, requestTime);
    result.statusCode = httpCode;

    if (httpCode > 0) {
        result.body = http.getString();
        result.success = (httpCode >= 200 && httpCode < 300);
        if (!result.success) {
            Serial.printf("[API] Server error: %s\n", result.body.c_str());
        }
    } else {
        result.error = http.errorToString(httpCode);
        result.success = false;
        Serial.printf("[API] Connection error: %s (code: %d)\n", result.error.c_str(), httpCode);

        // Detailed error codes
        switch (httpCode) {
            case -1: Serial.println("[API] Error: CONNECTION_REFUSED"); break;
            case -2: Serial.println("[API] Error: SEND_HEADER_FAILED"); break;
            case -3: Serial.println("[API] Error: SEND_PAYLOAD_FAILED"); break;
            case -4: Serial.println("[API] Error: NOT_CONNECTED"); break;
            case -5: Serial.println("[API] Error: CONNECTION_LOST"); break;
            case -6: Serial.println("[API] Error: NO_STREAM"); break;
            case -7: Serial.println("[API] Error: NO_HTTP_SERVER"); break;
            case -8: Serial.println("[API] Error: TOO_LESS_RAM"); break;
            case -9: Serial.println("[API] Error: ENCODING"); break;
            case -10: Serial.println("[API] Error: STREAM_WRITE"); break;
            case -11: Serial.println("[API] Error: READ_TIMEOUT"); break;
            default: Serial.printf("[API] Error: Unknown code %d\n", httpCode); break;
        }
    }

    http.end();
#elif defined(PLATFORM_NATIVE)
    // Native implementation using libcurl
    String url = buildUrl(path);
    Serial.printf("[API] GET %s\n", url.c_str());

    CURL* curl = curl_easy_init();
    if (curl) {
        std::string responseBody;
        struct curl_slist* headers = NULL;

        headers = curl_slist_append(headers, "Content-Type: application/json");
        if (_apiKey.length() > 0) {
            String authHeader = "Authorization: Bearer " + _apiKey;
            headers = curl_slist_append(headers, authHeader.c_str());
        }

        curl_easy_setopt(curl, CURLOPT_URL, url.c_str());
        curl_easy_setopt(curl, CURLOPT_HTTPHEADER, headers);
        curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteCallback);
        curl_easy_setopt(curl, CURLOPT_WRITEDATA, &responseBody);
        curl_easy_setopt(curl, CURLOPT_TIMEOUT_MS, _timeout);

        // Allow self-signed certificates (for development)
        const char* insecure = std::getenv("HUB_INSECURE");
        if (insecure && strcmp(insecure, "true") == 0) {
            curl_easy_setopt(curl, CURLOPT_SSL_VERIFYPEER, 0L);
            curl_easy_setopt(curl, CURLOPT_SSL_VERIFYHOST, 0L);
        }

        CURLcode res = curl_easy_perform(curl);

        if (res == CURLE_OK) {
            long httpCode;
            curl_easy_getinfo(curl, CURLINFO_RESPONSE_CODE, &httpCode);
            result.statusCode = (int)httpCode;
            result.body = String(responseBody.c_str());
            result.success = (httpCode >= 200 && httpCode < 300);
        } else {
            result.error = String(curl_easy_strerror(res));
            result.success = false;
            result.statusCode = 0;
            Serial.printf("[API] CURL error: %s\n", result.error.c_str());
        }

        curl_slist_free_all(headers);
        curl_easy_cleanup(curl);
    } else {
        result.error = "Failed to initialize CURL";
        result.success = false;
    }
#endif

    return result;
}

ApiResponse ApiClient::httpPost(const String& path, const String& body) {
    ApiResponse result;

#ifdef PLATFORM_ESP32
    String url = buildUrl(path);
    Serial.printf("[API] POST %s: %s\n", url.c_str(), body.c_str());

    HTTPClient http;
    bool isHttps = url.startsWith("https://");

    if (isHttps) {
        WiFiClientSecure secureClient;
        secureClient.setInsecure();  // Skip certificate validation
        http.begin(secureClient, url);
    } else {
        WiFiClient plainClient;
        http.begin(plainClient, url);
    }

    http.setTimeout(_timeout);
    http.addHeader("Authorization", "Bearer " + _apiKey);
    http.addHeader("Content-Type", "application/json");

    Serial.printf("[API] POST request (timeout: %d ms)...\n", _timeout);
    unsigned long requestStart = millis();

    int httpCode = http.POST(body);
    unsigned long requestTime = millis() - requestStart;

    result.statusCode = httpCode;
    Serial.printf("[API] Response: HTTP %d (%lu ms)\n", httpCode, requestTime);

    if (httpCode > 0) {
        result.body = http.getString();
        result.success = (httpCode >= 200 && httpCode < 300);
        if (!result.success) {
            Serial.printf("[API] Server error: %s\n", result.body.c_str());
        }
    } else {
        result.error = http.errorToString(httpCode);
        result.success = false;
        Serial.printf("[API] Connection error: %s (code: %d)\n", result.error.c_str(), httpCode);

        // Detailed error codes
        switch (httpCode) {
            case -1: Serial.println("[API] Error: CONNECTION_REFUSED"); break;
            case -2: Serial.println("[API] Error: SEND_HEADER_FAILED"); break;
            case -3: Serial.println("[API] Error: SEND_PAYLOAD_FAILED"); break;
            case -4: Serial.println("[API] Error: NOT_CONNECTED"); break;
            case -5: Serial.println("[API] Error: CONNECTION_LOST"); break;
            case -6: Serial.println("[API] Error: NO_STREAM"); break;
            case -7: Serial.println("[API] Error: NO_HTTP_SERVER"); break;
            case -8: Serial.println("[API] Error: TOO_LESS_RAM"); break;
            case -9: Serial.println("[API] Error: ENCODING"); break;
            case -10: Serial.println("[API] Error: STREAM_WRITE"); break;
            case -11: Serial.println("[API] Error: READ_TIMEOUT"); break;
            default: Serial.printf("[API] Error: Unknown code %d\n", httpCode); break;
        }
    }

    http.end();
#elif defined(PLATFORM_NATIVE)
    // Native implementation using libcurl
    String url = buildUrl(path);
    Serial.printf("[API] POST %s: %s\n", url.c_str(), body.c_str());

    CURL* curl = curl_easy_init();
    if (curl) {
        std::string responseBody;
        struct curl_slist* headers = NULL;

        headers = curl_slist_append(headers, "Content-Type: application/json");
        if (_apiKey.length() > 0) {
            String authHeader = "Authorization: Bearer " + _apiKey;
            headers = curl_slist_append(headers, authHeader.c_str());
        }

        curl_easy_setopt(curl, CURLOPT_URL, url.c_str());
        curl_easy_setopt(curl, CURLOPT_HTTPHEADER, headers);
        curl_easy_setopt(curl, CURLOPT_POSTFIELDS, body.c_str());
        curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteCallback);
        curl_easy_setopt(curl, CURLOPT_WRITEDATA, &responseBody);
        curl_easy_setopt(curl, CURLOPT_TIMEOUT_MS, _timeout);

        // Allow self-signed certificates (for development)
        const char* insecure = std::getenv("HUB_INSECURE");
        if (insecure && strcmp(insecure, "true") == 0) {
            curl_easy_setopt(curl, CURLOPT_SSL_VERIFYPEER, 0L);
            curl_easy_setopt(curl, CURLOPT_SSL_VERIFYHOST, 0L);
        }

        CURLcode res = curl_easy_perform(curl);

        if (res == CURLE_OK) {
            long httpCode;
            curl_easy_getinfo(curl, CURLINFO_RESPONSE_CODE, &httpCode);
            result.statusCode = (int)httpCode;
            result.body = String(responseBody.c_str());
            result.success = (httpCode >= 200 && httpCode < 300);
        } else {
            result.error = String(curl_easy_strerror(res));
            result.success = false;
            result.statusCode = 0;
            Serial.printf("[API] CURL error: %s\n", result.error.c_str());
        }

        curl_slist_free_all(headers);
        curl_easy_cleanup(curl);
    } else {
        result.error = "Failed to initialize CURL";
        result.success = false;
    }
#endif

    return result;
}
