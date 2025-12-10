/**
 * @file lora_connection.h
 * @brief LoRaWAN Connection Implementation
 *
 * Implements IConnection interface for LoRaWAN communication.
 * Handles OTAA join, payload encoding, and transmission.
 *
 * @version 1.0.0
 * @date 2025-12-10
 *
 * Sprint: LoRa-02 - Grid.Sensor LoRaWAN Firmware
 */

#pragma once

#include "connection_interface.h"
#include "lora_credentials.h"

#include <queue>
#include <array>

/**
 * @brief Pending Transmission Structure
 */
struct PendingTx {
    std::vector<uint8_t> payload;
    uint8_t port;
    uint8_t retries;
    bool confirmed;
};

/**
 * @brief LoRaWAN Connection Implementation
 *
 * Provides LoRaWAN connectivity for sensor nodes.
 * Features:
 * - OTAA join with credential management
 * - Compact binary payload encoding
 * - Retry queue for failed transmissions
 * - Downlink handling for configuration updates
 */
class LoRaConnection : public IConnection {
public:
    LoRaConnection();
    ~LoRaConnection() override;

    // === IConnection Interface ===

    bool connect() override;
    bool disconnect() override;
    bool isConnected() const override;

    bool sendReading(const Reading& reading) override;
    bool sendBatch(const std::vector<Reading>& readings) override;

    void onConfigReceived(ConfigCallback callback) override;
    NodeConfig registerNode(const NodeInfo& info) override;

    // === LoRaWAN-specific Methods ===

    /**
     * @brief Set LoRaWAN credentials
     * @param creds Credentials structure
     */
    void setCredentials(const LoRaCredentials& creds);

    /**
     * @brief Get credential manager reference
     * @return Reference to credential manager
     */
    CredentialManager& getCredentialManager() { return credManager_; }

    /**
     * @brief Process LoRaWAN events and retry queue
     *
     * Must be called regularly in main loop.
     */
    void process();

    /**
     * @brief Get last RSSI value
     * @return RSSI in dBm
     */
    int16_t getLastRssi() const;

    /**
     * @brief Get last SNR value
     * @return SNR in dB
     */
    int8_t getLastSnr() const;

    /**
     * @brief Get uplink frame counter
     * @return Frame counter value
     */
    uint32_t getFrameCounter() const;

    /**
     * @brief Check if transmission is in progress
     * @return true if transmitting
     */
    bool isTransmitting() const;

    /**
     * @brief Get number of pending retries
     * @return Queue size
     */
    size_t getPendingCount() const;

private:
    CredentialManager credManager_;
    ConfigCallback configCallback_;
    bool joined_ = false;

    // Retry queue
    std::queue<PendingTx> txQueue_;
    static constexpr uint8_t MAX_RETRIES = 3;
    static constexpr size_t MAX_QUEUE_SIZE = 10;

    // === Payload Encoding ===

    /**
     * @brief Encode single reading to binary payload
     *
     * Format: [TypeID:1][Value:2] = 3 bytes per reading
     * Value is int16_t with 2 decimal places (value * 100)
     *
     * @param reading Reading to encode
     * @return Encoded payload bytes
     */
    std::vector<uint8_t> encodeReading(const Reading& reading);

    /**
     * @brief Encode multiple readings to binary payload
     *
     * Concatenates encoded readings up to MAX_PAYLOAD_SIZE.
     *
     * @param readings Readings to encode
     * @return Encoded payload bytes
     */
    std::vector<uint8_t> encodeBatch(const std::vector<Reading>& readings);

    /**
     * @brief Get sensor type ID for payload encoding
     * @param type Sensor type string
     * @return Type ID byte
     */
    static uint8_t getSensorTypeId(const std::string& type);

    /**
     * @brief Get sensor type string from ID
     * @param typeId Type ID byte
     * @return Sensor type string
     */
    static std::string getSensorTypeString(uint8_t typeId);

    // === Queue Management ===

    /**
     * @brief Process retry queue
     *
     * Attempts to send pending transmissions.
     */
    void processTxQueue();

    /**
     * @brief Add payload to retry queue
     * @param payload Encoded payload
     * @param port LoRaWAN port
     * @param confirmed true for confirmed uplink
     */
    void queueForRetry(const std::vector<uint8_t>& payload, uint8_t port, bool confirmed);

    // === Downlink Handling ===

    /**
     * @brief Handle received downlink data
     * @param port LoRaWAN port
     * @param data Payload data
     * @param len Payload length
     */
    void handleDownlink(uint8_t port, const uint8_t* data, size_t len);

    /**
     * @brief Parse configuration downlink
     * @param data Payload data
     * @param len Payload length
     */
    void parseConfigDownlink(const uint8_t* data, size_t len);
};
