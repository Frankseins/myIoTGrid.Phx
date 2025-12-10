/**
 * @file lora_connection.cpp
 * @brief LoRaWAN Connection Implementation
 *
 * @version 1.0.0
 * @date 2025-12-10
 *
 * Sprint: LoRa-02 - Grid.Sensor LoRaWAN Firmware
 */

#ifdef PLATFORM_ESP32
#include <Arduino.h>
#endif

#include "lora_connection.h"
#include "hal/hal.h"
#include "hal/hal_lora.h"
#include "config.h"

#include <algorithm>
#include <cstring>

// ============================================================
// CONSTRUCTOR / DESTRUCTOR
// ============================================================

LoRaConnection::LoRaConnection() {
    credManager_.generateDevEui();
    credManager_.loadFromNvs();
}

LoRaConnection::~LoRaConnection() {
    disconnect();
}

// ============================================================
// ICONNECTION INTERFACE
// ============================================================

bool LoRaConnection::connect() {
    LOG_INFO("Initializing LoRaWAN connection...");

    // Initialize LoRa radio
    if (!hal::lora::init()) {
        LOG_ERROR("Failed to initialize LoRa radio");
        return false;
    }

    // Check credentials
    if (!credManager_.isReadyForOtaa()) {
        LOG_ERROR("LoRaWAN credentials not configured");
        LOG_INFO("Use serial commands to configure:");
        LOG_INFO("  APPEUI=<16 hex chars>");
        LOG_INFO("  APPKEY=<32 hex chars>");
        LOG_INFO("  SAVE");
        return false;
    }

    const auto& creds = credManager_.getCredentials();

    LOG_INFO("DevEUI: %s", creds.getDevEuiString().c_str());
    LOG_INFO("AppEUI: %s", creds.getAppEuiString().c_str());

    // Set up downlink handler
    hal::lora::set_rx_callback([this](uint8_t port, const uint8_t* data, size_t len) {
        handleDownlink(port, data, len);
    });

    // Attempt OTAA join
    joined_ = hal::lora::join_otaa(
        creds.devEui.data(),
        creds.appEui.data(),
        creds.appKey.data(),
        [this](bool success, hal::lora::LoRaError error) {
            joined_ = success;
            if (success) {
                LOG_INFO("LoRaWAN OTAA join successful!");
                // Save frame counter
                credManager_.saveFrameCounters();
            } else {
                LOG_ERROR("LoRaWAN OTAA join failed: %s",
                          hal::lora::get_error_message(error));
            }
        }
    );

    return joined_;
}

bool LoRaConnection::disconnect() {
    if (!joined_) return true;

    LOG_INFO("Disconnecting LoRaWAN...");

    // Save frame counters before disconnect
    credManager_.saveFrameCounters();

    // Put radio to sleep
    hal::lora::sleep();
    joined_ = false;

    return true;
}

bool LoRaConnection::isConnected() const {
    return joined_ && hal::lora::is_joined();
}

bool LoRaConnection::sendReading(const Reading& reading) {
    if (!isConnected()) {
        LOG_ERROR("Cannot send: not connected");
        return false;
    }

    auto payload = encodeReading(reading);

    LOG_INFO("Sending reading: %s = %.2f %s",
             reading.type.c_str(), reading.value, reading.unit.c_str());

    bool success = hal::lora::send(
        LORAWAN_SENSOR_PORT,
        payload.data(),
        payload.size(),
        LORAWAN_CONFIRMED_UPLINKS
    );

    if (!success) {
        LOG_WARN("Transmission failed, queueing for retry");
        queueForRetry(payload, LORAWAN_SENSOR_PORT, LORAWAN_CONFIRMED_UPLINKS);
    } else {
        // Save frame counter periodically
        if (getFrameCounter() % 10 == 0) {
            credManager_.saveFrameCounters();
        }
    }

    return success;
}

bool LoRaConnection::sendBatch(const std::vector<Reading>& readings) {
    if (!isConnected()) {
        LOG_ERROR("Cannot send: not connected");
        return false;
    }

    if (readings.empty()) {
        LOG_WARN("No readings to send");
        return true;
    }

    auto payload = encodeBatch(readings);

    LOG_INFO("Sending batch of %zu readings (%zu bytes)",
             readings.size(), payload.size());

    bool success = hal::lora::send(
        LORAWAN_SENSOR_PORT,
        payload.data(),
        payload.size(),
        LORAWAN_CONFIRMED_UPLINKS
    );

    if (!success) {
        LOG_WARN("Batch transmission failed, queueing for retry");
        queueForRetry(payload, LORAWAN_SENSOR_PORT, LORAWAN_CONFIRMED_UPLINKS);
    } else {
        // Save frame counter periodically
        if (getFrameCounter() % 10 == 0) {
            credManager_.saveFrameCounters();
        }
    }

    return success;
}

void LoRaConnection::onConfigReceived(ConfigCallback callback) {
    configCallback_ = callback;
}

NodeConfig LoRaConnection::registerNode(const NodeInfo& info) {
    // For LoRaWAN, registration happens implicitly through first uplink
    // Return default config
    NodeConfig config;
    config.nodeId = credManager_.getCredentials().getDevEuiString();
    config.intervalSeconds = DEFAULT_TX_INTERVAL_SECONDS;
    config.adrEnabled = LORAWAN_ADR_ENABLED;
    config.dataRate = LORAWAN_DEFAULT_DR;
    return config;
}

// ============================================================
// LORAWAN-SPECIFIC METHODS
// ============================================================

void LoRaConnection::setCredentials(const LoRaCredentials& creds) {
    // Copy credentials (would need a setter in CredentialManager)
}

void LoRaConnection::process() {
    // Process LoRa events
    hal::lora::process();

    // Process retry queue
    processTxQueue();
}

int16_t LoRaConnection::getLastRssi() const {
    return hal::lora::get_last_rssi();
}

int8_t LoRaConnection::getLastSnr() const {
    return hal::lora::get_last_snr();
}

uint32_t LoRaConnection::getFrameCounter() const {
    return hal::lora::get_frame_counter_up();
}

bool LoRaConnection::isTransmitting() const {
    return hal::lora::get_tx_status() == hal::lora::TxStatus::TRANSMITTING;
}

size_t LoRaConnection::getPendingCount() const {
    return txQueue_.size();
}

// ============================================================
// PAYLOAD ENCODING
// ============================================================

std::vector<uint8_t> LoRaConnection::encodeReading(const Reading& reading) {
    // myIoTGrid LoRa Payload Format:
    // [TypeID:1 byte][Value:2 bytes (int16, 2 decimal places)]
    // Total: 3 bytes per sensor

    std::vector<uint8_t> payload;

    uint8_t typeId = getSensorTypeId(reading.type);

    // Convert value to int16 with 2 decimal places
    // Special handling for pressure (use 1 decimal place due to larger values)
    int16_t encodedValue;
    if (reading.type == "pressure") {
        encodedValue = static_cast<int16_t>(reading.value * 10);  // 1 decimal
    } else {
        encodedValue = static_cast<int16_t>(reading.value * 100); // 2 decimals
    }

    payload.push_back(typeId);
    payload.push_back((encodedValue >> 8) & 0xFF);  // High byte (MSB)
    payload.push_back(encodedValue & 0xFF);          // Low byte (LSB)

    return payload;
}

std::vector<uint8_t> LoRaConnection::encodeBatch(const std::vector<Reading>& readings) {
    // Multiple readings concatenated
    // Max ~51 bytes for EU868 DR0 (SF12)
    // 3 bytes per sensor = max 17 sensors

    std::vector<uint8_t> payload;

    for (const auto& reading : readings) {
        auto encoded = encodeReading(reading);
        payload.insert(payload.end(), encoded.begin(), encoded.end());

        // Safety limit (leave room for overhead)
        if (payload.size() >= MAX_PAYLOAD_SIZE - 3) {
            LOG_WARN("Payload size limit reached, truncating batch");
            break;
        }
    }

    return payload;
}

uint8_t LoRaConnection::getSensorTypeId(const std::string& type) {
    if (type == "temperature")   return SensorTypeId::TEMPERATURE;
    if (type == "humidity")      return SensorTypeId::HUMIDITY;
    if (type == "pressure")      return SensorTypeId::PRESSURE;
    if (type == "water_level")   return SensorTypeId::WATER_LEVEL;
    if (type == "battery")       return SensorTypeId::BATTERY;
    if (type == "co2")           return SensorTypeId::CO2;
    if (type == "pm25")          return SensorTypeId::PM25;
    if (type == "pm10")          return SensorTypeId::PM10;
    if (type == "light")         return SensorTypeId::LIGHT;
    if (type == "uv")            return SensorTypeId::UV;
    if (type == "soil_moisture") return SensorTypeId::SOIL_MOISTURE;
    if (type == "wind_speed")    return SensorTypeId::WIND_SPEED;
    if (type == "rainfall")      return SensorTypeId::RAINFALL;
    if (type == "rssi")          return SensorTypeId::RSSI;
    if (type == "snr")           return SensorTypeId::SNR;

    return SensorTypeId::UNKNOWN;
}

std::string LoRaConnection::getSensorTypeString(uint8_t typeId) {
    switch (typeId) {
        case SensorTypeId::TEMPERATURE:   return "temperature";
        case SensorTypeId::HUMIDITY:      return "humidity";
        case SensorTypeId::PRESSURE:      return "pressure";
        case SensorTypeId::WATER_LEVEL:   return "water_level";
        case SensorTypeId::BATTERY:       return "battery";
        case SensorTypeId::CO2:           return "co2";
        case SensorTypeId::PM25:          return "pm25";
        case SensorTypeId::PM10:          return "pm10";
        case SensorTypeId::LIGHT:         return "light";
        case SensorTypeId::UV:            return "uv";
        case SensorTypeId::SOIL_MOISTURE: return "soil_moisture";
        case SensorTypeId::WIND_SPEED:    return "wind_speed";
        case SensorTypeId::RAINFALL:      return "rainfall";
        case SensorTypeId::RSSI:          return "rssi";
        case SensorTypeId::SNR:           return "snr";
        default:                          return "unknown";
    }
}

// ============================================================
// QUEUE MANAGEMENT
// ============================================================

void LoRaConnection::processTxQueue() {
    if (txQueue_.empty()) return;

    // Check if radio is ready
    if (!hal::lora::is_tx_ready()) return;

    auto& pending = txQueue_.front();

    LOG_DEBUG("Retrying transmission (attempt %d/%d)",
              MAX_RETRIES - pending.retries + 1, MAX_RETRIES);

    bool success = hal::lora::send(
        pending.port,
        pending.payload.data(),
        pending.payload.size(),
        pending.confirmed
    );

    if (success) {
        LOG_INFO("Retry successful");
        txQueue_.pop();
    } else if (--pending.retries == 0) {
        LOG_ERROR("Max retries reached, dropping transmission");
        txQueue_.pop();
    }
}

void LoRaConnection::queueForRetry(const std::vector<uint8_t>& payload, uint8_t port, bool confirmed) {
    if (txQueue_.size() >= MAX_QUEUE_SIZE) {
        LOG_WARN("Retry queue full, dropping oldest entry");
        txQueue_.pop();
    }

    PendingTx pending;
    pending.payload = payload;
    pending.port = port;
    pending.retries = MAX_RETRIES;
    pending.confirmed = confirmed;

    txQueue_.push(pending);
}

// ============================================================
// DOWNLINK HANDLING
// ============================================================

void LoRaConnection::handleDownlink(uint8_t port, const uint8_t* data, size_t len) {
    LOG_INFO("Downlink received (port %d, %d bytes)", port, len);

    // Print hex for debugging
    LOG_DEBUG("Data: ");
    for (size_t i = 0; i < len; i++) {
        LOG_DEBUG("%02X ", data[i]);
    }

    switch (port) {
        case LORAWAN_CONFIG_PORT:
            parseConfigDownlink(data, len);
            break;

        default:
            LOG_WARN("Unknown downlink port: %d", port);
            break;
    }
}

void LoRaConnection::parseConfigDownlink(const uint8_t* data, size_t len) {
    // Configuration downlink format:
    // [0]: Interval in minutes (1 byte)
    // [1]: Flags (1 byte, optional)
    //      Bit 0: ADR enable
    //      Bit 1-3: Reserved
    //      Bit 4-7: Reserved

    if (len < 1) {
        LOG_WARN("Config downlink too short");
        return;
    }

    NodeConfig config;
    config.nodeId = credManager_.getCredentials().getDevEuiString();

    // Parse interval (minutes to seconds)
    uint8_t intervalMinutes = data[0];
    config.intervalSeconds = intervalMinutes * 60;

    // Validate interval
    if (config.intervalSeconds < MIN_TX_INTERVAL_SECONDS) {
        config.intervalSeconds = MIN_TX_INTERVAL_SECONDS;
    } else if (config.intervalSeconds > MAX_TX_INTERVAL_SECONDS) {
        config.intervalSeconds = MAX_TX_INTERVAL_SECONDS;
    }

    LOG_INFO("New configuration: interval = %u seconds", config.intervalSeconds);

    // Parse flags if present
    if (len >= 2) {
        uint8_t flags = data[1];
        config.adrEnabled = (flags & 0x01) != 0;
        hal::lora::set_adr(config.adrEnabled);
        LOG_INFO("ADR %s", config.adrEnabled ? "enabled" : "disabled");
    } else {
        config.adrEnabled = LORAWAN_ADR_ENABLED;
    }

    config.dataRate = hal::lora::get_data_rate();

    // Notify callback
    if (configCallback_) {
        configCallback_(config);
    }
}
