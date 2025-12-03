/**
 * myIoTGrid.Sensor - API Client
 * HTTP client for Hub API communication
 */

#ifndef API_CLIENT_H
#define API_CLIENT_H

#include <Arduino.h>
#include <functional>

/**
 * API response structure
 */
struct ApiResponse {
    int statusCode;
    String body;
    bool success;
    String error;

    ApiResponse() : statusCode(0), success(false) {}
};

/**
 * Heartbeat response from Hub
 */
struct HeartbeatResponse {
    bool success;
    unsigned long serverTime;
    int nextHeartbeatSeconds;
};

/**
 * API Client for Hub communication
 */
class ApiClient {
public:
    ApiClient();

    /**
     * Configure API client
     */
    void configure(const String& baseUrl, const String& nodeId, const String& apiKey);

    /**
     * Validate API key with Hub
     */
    bool validateApiKey();

    /**
     * Send heartbeat to Hub
     */
    HeartbeatResponse sendHeartbeat(const String& firmwareVersion = "", int batteryLevel = -1);

    /**
     * Send sensor reading to Hub
     */
    bool sendReading(const String& sensorType, double value, const String& unit = "");

    /**
     * Send batch of readings
     */
    bool sendReadings(const String& readingsJson);

    /**
     * Check if configured
     */
    bool isConfigured() const;

    /**
     * Get base URL
     */
    String getBaseUrl() const { return _baseUrl; }

    /**
     * Set connection timeout
     */
    void setTimeout(int timeoutMs) { _timeout = timeoutMs; }

private:
    String _baseUrl;
    String _nodeId;
    String _apiKey;
    int _timeout;
    bool _configured;

    /**
     * Make HTTP GET request
     */
    ApiResponse httpGet(const String& path);

    /**
     * Make HTTP POST request
     */
    ApiResponse httpPost(const String& path, const String& body);

    /**
     * Build full URL
     */
    String buildUrl(const String& path) const;

    /**
     * Add authorization header
     */
    void addAuthHeader(String& headers) const;
};

#endif // API_CLIENT_H
