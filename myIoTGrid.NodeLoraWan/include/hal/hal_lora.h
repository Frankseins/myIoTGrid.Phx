/**
 * @file hal_lora.h
 * @brief Hardware Abstraction Layer for LoRaWAN Radio
 *
 * Defines platform-independent interfaces for LoRaWAN operations.
 * Implementation uses RadioLib for SX1262 (Heltec LoRa32 V3).
 *
 * @version 1.0.0
 * @date 2025-12-10
 *
 * Sprint: LoRa-02 - Grid.Sensor LoRaWAN Firmware
 */

#pragma once

#include <cstdint>
#include <cstddef>
#include <functional>

namespace hal {
namespace lora {

// ============================================================
// STATUS ENUMS
// ============================================================

/**
 * @brief LoRaWAN Join Status
 */
enum class JoinStatus {
    NOT_JOINED,     ///< Device has not attempted to join
    JOINING,        ///< Join procedure in progress
    JOINED,         ///< Successfully joined network
    JOIN_FAILED     ///< Join procedure failed
};

/**
 * @brief LoRaWAN Transmission Status
 */
enum class TxStatus {
    IDLE,           ///< Ready for transmission
    TRANSMITTING,   ///< Transmission in progress
    TX_COMPLETE,    ///< Transmission completed successfully
    TX_FAILED,      ///< Transmission failed
    TX_TIMEOUT      ///< Transmission timed out
};

/**
 * @brief LoRaWAN Receive Status
 */
enum class RxStatus {
    NO_DATA,        ///< No downlink data received
    DATA_RECEIVED,  ///< Downlink data available
    RX_ERROR        ///< Receive error
};

/**
 * @brief LoRaWAN Error Codes
 */
enum class LoRaError {
    NONE = 0,
    RADIO_INIT_FAILED,
    JOIN_TIMEOUT,
    JOIN_REJECTED,
    TX_FAILED,
    TX_TIMEOUT,
    DUTY_CYCLE_LIMITED,
    PAYLOAD_TOO_LARGE,
    NOT_JOINED,
    INVALID_CREDENTIALS,
    UNKNOWN
};

// ============================================================
// CALLBACK TYPES
// ============================================================

/**
 * @brief Callback for join completion
 * @param success true if join succeeded
 * @param error Error code if failed
 */
using JoinCallback = std::function<void(bool success, LoRaError error)>;

/**
 * @brief Callback for transmission completion
 * @param success true if transmission succeeded
 * @param error Error code if failed
 */
using TxCallback = std::function<void(bool success, LoRaError error)>;

/**
 * @brief Callback for received downlink data
 * @param port LoRaWAN port number
 * @param data Pointer to received data
 * @param len Length of received data
 */
using RxCallback = std::function<void(uint8_t port, const uint8_t* data, size_t len)>;

// ============================================================
// RADIO INITIALIZATION
// ============================================================

/**
 * @brief Initialize LoRa radio hardware
 *
 * Must be called before any other LoRa operations.
 * Configures SPI bus and initializes SX1262 radio.
 *
 * @return true if initialization successful
 */
bool init();

/**
 * @brief Shutdown LoRa radio
 *
 * Puts radio in sleep mode and releases resources.
 */
void shutdown();

/**
 * @brief Check if radio is initialized
 * @return true if radio is initialized and ready
 */
bool is_initialized();

// ============================================================
// LORAWAN NETWORK JOIN
// ============================================================

/**
 * @brief Join LoRaWAN network using OTAA
 *
 * Over-The-Air Activation (preferred method).
 * DevEUI, AppEUI, and AppKey must be configured.
 *
 * @param devEui Device EUI (8 bytes, LSB format)
 * @param appEui Application EUI (8 bytes, LSB format)
 * @param appKey Application Key (16 bytes, MSB format)
 * @param callback Optional callback for join result
 * @return true if join request was sent successfully
 */
bool join_otaa(
    const uint8_t* devEui,
    const uint8_t* appEui,
    const uint8_t* appKey,
    JoinCallback callback = nullptr
);

/**
 * @brief Activate LoRaWAN session using ABP
 *
 * Activation By Personalization (fallback method).
 * Uses pre-provisioned session keys.
 *
 * @param devAddr Device Address (4 bytes)
 * @param nwkSKey Network Session Key (16 bytes)
 * @param appSKey Application Session Key (16 bytes)
 * @return true if activation successful
 */
bool activate_abp(
    const uint8_t* devAddr,
    const uint8_t* nwkSKey,
    const uint8_t* appSKey
);

/**
 * @brief Get current join status
 * @return Current JoinStatus
 */
JoinStatus get_join_status();

/**
 * @brief Check if device is joined to network
 * @return true if joined
 */
bool is_joined();

// ============================================================
// DATA TRANSMISSION
// ============================================================

/**
 * @brief Send uplink data
 *
 * @param port LoRaWAN port number (1-223)
 * @param data Pointer to data buffer
 * @param len Length of data
 * @param confirmed true for confirmed uplink (requires ACK)
 * @param callback Optional callback for transmission result
 * @return true if transmission was queued successfully
 */
bool send(
    uint8_t port,
    const uint8_t* data,
    size_t len,
    bool confirmed = false,
    TxCallback callback = nullptr
);

/**
 * @brief Get current transmission status
 * @return Current TxStatus
 */
TxStatus get_tx_status();

/**
 * @brief Check if radio is ready for transmission
 * @return true if ready to send
 */
bool is_tx_ready();

/**
 * @brief Get time until next transmission allowed (duty cycle)
 * @return Milliseconds until next TX allowed, 0 if ready
 */
uint32_t get_time_until_tx();

// ============================================================
// DATA RECEPTION
// ============================================================

/**
 * @brief Set callback for received downlink data
 * @param callback Function to call when data is received
 */
void set_rx_callback(RxCallback callback);

/**
 * @brief Check for received downlink data
 * @return RxStatus indicating if data is available
 */
RxStatus check_rx();

// ============================================================
// RADIO CONFIGURATION
// ============================================================

/**
 * @brief Enable or disable Adaptive Data Rate
 * @param enable true to enable ADR
 */
void set_adr(bool enable);

/**
 * @brief Get ADR status
 * @return true if ADR is enabled
 */
bool get_adr();

/**
 * @brief Set data rate (0-15 depending on region)
 * @param dr Data rate value
 * @return true if data rate was set successfully
 */
bool set_data_rate(uint8_t dr);

/**
 * @brief Get current data rate
 * @return Current data rate value
 */
uint8_t get_data_rate();

/**
 * @brief Set TX power in dBm
 * @param power Power in dBm (2-20 for EU868)
 * @return true if power was set successfully
 */
bool set_tx_power(int8_t power);

/**
 * @brief Get current TX power
 * @return TX power in dBm
 */
int8_t get_tx_power();

// ============================================================
// RADIO STATUS & METRICS
// ============================================================

/**
 * @brief Get RSSI of last received packet
 * @return RSSI in dBm
 */
int16_t get_last_rssi();

/**
 * @brief Get SNR of last received packet
 * @return SNR in dB
 */
int8_t get_last_snr();

/**
 * @brief Get current spreading factor
 * @return Spreading factor (7-12)
 */
uint8_t get_spreading_factor();

/**
 * @brief Get current bandwidth
 * @return Bandwidth in kHz
 */
float get_bandwidth();

/**
 * @brief Get uplink frame counter
 * @return Frame counter value
 */
uint32_t get_frame_counter_up();

/**
 * @brief Get downlink frame counter
 * @return Frame counter value
 */
uint32_t get_frame_counter_down();

// ============================================================
// POWER MANAGEMENT
// ============================================================

/**
 * @brief Put radio in sleep mode
 *
 * Reduces power consumption to minimum.
 * Radio must be woken up before use.
 */
void sleep();

/**
 * @brief Wake radio from sleep mode
 *
 * Must be called after sleep() before any operations.
 */
void wake();

/**
 * @brief Check if radio is in sleep mode
 * @return true if radio is sleeping
 */
bool is_sleeping();

// ============================================================
// EVENT PROCESSING
// ============================================================

/**
 * @brief Process LoRaWAN events
 *
 * Must be called regularly in main loop.
 * Handles receive windows, callbacks, etc.
 */
void process();

// ============================================================
// DEBUG & DIAGNOSTICS
// ============================================================

/**
 * @brief Get last error code
 * @return Last LoRaError
 */
LoRaError get_last_error();

/**
 * @brief Get error message for error code
 * @param error Error code
 * @return Human-readable error message
 */
const char* get_error_message(LoRaError error);

/**
 * @brief Print radio status to serial
 */
void print_status();

} // namespace lora
} // namespace hal
