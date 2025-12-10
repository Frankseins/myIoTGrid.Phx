/**
 * @file connection_interface.h
 * @brief Abstract interface for sensor data connections
 *
 * Defines the IConnection interface for sending sensor readings
 * to the backend. Implementations include LoRaConnection.
 *
 * @version 1.0.0
 * @date 2025-12-10
 *
 * Sprint: LoRa-02 - Grid.Sensor LoRaWAN Firmware
 */

#pragma once

#include <cstdint>
#include <string>
#include <vector>
#include <functional>

/**
 * @brief Sensor Reading Data
 */
struct Reading {
    std::string deviceId;       ///< Device identifier
    std::string type;           ///< Sensor type (e.g., "temperature")
    float value;                ///< Measured value
    std::string unit;           ///< Unit of measurement (e.g., "°C")
    uint32_t timestamp;         ///< Unix timestamp (seconds)
};

/**
 * @brief Node Information for Registration
 */
struct NodeInfo {
    std::string deviceId;       ///< Device identifier
    std::string firmwareVersion;///< Firmware version string
    std::string hardwareType;   ///< Hardware type (e.g., "heltec_lora32_v3")
    std::vector<std::string> sensorTypes;  ///< Supported sensor types
};

/**
 * @brief Configuration received from backend
 */
struct NodeConfig {
    std::string nodeId;         ///< Assigned node ID
    uint32_t intervalSeconds;   ///< Transmission interval in seconds
    bool adrEnabled;            ///< ADR enabled flag
    uint8_t dataRate;           ///< Initial data rate
};

/**
 * @brief Callback type for configuration updates
 */
using ConfigCallback = std::function<void(const NodeConfig& config)>;

/**
 * @brief Abstract Connection Interface
 *
 * Base class for all connection implementations.
 * LoRaConnection implements this interface for LoRaWAN communication.
 */
class IConnection {
public:
    virtual ~IConnection() = default;

    // === Connection Lifecycle ===

    /**
     * @brief Establish connection to backend
     *
     * For LoRaWAN: Performs OTAA join.
     *
     * @return true if connection established
     */
    virtual bool connect() = 0;

    /**
     * @brief Disconnect from backend
     *
     * For LoRaWAN: Puts radio to sleep.
     *
     * @return true if disconnected successfully
     */
    virtual bool disconnect() = 0;

    /**
     * @brief Check if connected
     *
     * For LoRaWAN: Returns true if joined to network.
     *
     * @return true if connected
     */
    virtual bool isConnected() const = 0;

    // === Data Transmission ===

    /**
     * @brief Send a single sensor reading
     *
     * For LoRaWAN: Encodes reading as compact binary payload.
     *
     * @param reading Sensor reading to send
     * @return true if sent successfully
     */
    virtual bool sendReading(const Reading& reading) = 0;

    /**
     * @brief Send multiple readings in one transmission
     *
     * For LoRaWAN: Combines readings into single payload.
     *
     * @param readings Vector of readings to send
     * @return true if sent successfully
     */
    virtual bool sendBatch(const std::vector<Reading>& readings) = 0;

    // === Configuration ===

    /**
     * @brief Set callback for configuration updates
     *
     * Called when new configuration is received from backend.
     *
     * @param callback Function to call with new config
     */
    virtual void onConfigReceived(ConfigCallback callback) = 0;

    /**
     * @brief Register node with backend
     *
     * For LoRaWAN: May use first uplink as registration.
     *
     * @param info Node information
     * @return Configuration from backend
     */
    virtual NodeConfig registerNode(const NodeInfo& info) = 0;
};
