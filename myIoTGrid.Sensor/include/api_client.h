/**
 * myIoTGrid.Sensor - API Client
 * HTTP client for Hub API communication
 */

#ifndef API_CLIENT_H
#define API_CLIENT_H

#include <Arduino.h>
#include <functional>
#include <vector>

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
 * Registration response from Hub
 */
struct RegistrationResponse {
    bool success;
    String nodeId;          // GUID from server
    String serialNumber;
    String name;
    String location;
    int intervalSeconds;
    String connectionEndpoint;
    bool isNewNode;
    String message;
    String error;
};

/**
 * Sensor capability configuration from Hub
 */
struct SensorCapabilityConfig {
    String measurementType;
    String displayName;
    String unit;
};

/**
 * Sensor assignment configuration from Hub
 */
struct SensorAssignmentConfig {
    int endpointId;
    String sensorCode;
    String sensorName;
    String icon;
    String color;
    bool isActive;
    int intervalSeconds;
    String i2cAddress;
    int sdaPin;
    int sclPin;
    int oneWirePin;
    int analogPin;
    int digitalPin;
    int triggerPin;
    int echoPin;
    int baudRate;
    double offsetCorrection;
    double gainCorrection;
    std::vector<SensorCapabilityConfig> capabilities;
};

/**
 * Node sensor configuration response from Hub
 */
struct NodeConfigurationResponse {
    bool success;
    String nodeId;
    String serialNumber;
    String name;
    bool isSimulation;
    int defaultIntervalSeconds;
    std::vector<SensorAssignmentConfig> sensors;
    unsigned long configurationTimestamp;
    String error;
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
     * Register node with Hub (no API key required)
     * This should be called first before any other API calls
     */
    RegistrationResponse registerNode(const String& serialNumber,
                                       const String& firmwareVersion,
                                       const String& hardwareType,
                                       const std::vector<String>& capabilities = {});

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
     * @param sensorType Measurement type (e.g., "temperature", "humidity")
     * @param value The measured value
     * @param unit Unit of measurement (e.g., "°C", "%")
     * @param endpointId Optional endpoint ID to identify which sensor assignment this reading belongs to
     */
    bool sendReading(const String& sensorType, double value, const String& unit = "", int endpointId = -1);

    /**
     * Send batch of readings
     */
    bool sendReadings(const String& readingsJson);

    /**
     * Fetch sensor configuration for this node
     * Returns assigned sensors with their pin configurations
     */
    NodeConfigurationResponse fetchConfiguration(const String& serialNumber);

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
