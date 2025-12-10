/**
 * @file hal_lora.cpp
 * @brief LoRaWAN HAL Implementation using RadioLib
 *
 * Implementation of LoRaWAN functions for Heltec LoRa32 V3 (SX1262).
 * Uses RadioLib for LoRaWAN stack.
 *
 * @version 1.0.0
 * @date 2025-12-10
 *
 * Sprint: LoRa-02 - Grid.Sensor LoRaWAN Firmware
 */

#include "hal/hal_lora.h"
#include "hal/hal.h"
#include "config.h"

#include <Arduino.h>
#include <SPI.h>
#include <RadioLib.h>

// ============================================================
// HARDWARE CONFIGURATION
// ============================================================

// SPI instance for LoRa radio
static SPIClass loraSPI(HSPI);

// Module configuration for SX1262
static Module module(
    LORA_CS,
    LORA_DIO1,
    LORA_RST,
    LORA_BUSY,
    loraSPI
);

// SX1262 radio module
static SX1262 radio(&module);

// LoRaWAN node instance
static LoRaWANNode* node = nullptr;

// EU868 band configuration
static const LoRaWANBand_t* band = &EU868;

// ============================================================
// STATE VARIABLES
// ============================================================

static bool radioInitialized = false;
static bool radioSleeping = false;

static hal::lora::JoinStatus currentJoinStatus = hal::lora::JoinStatus::NOT_JOINED;
static hal::lora::TxStatus currentTxStatus = hal::lora::TxStatus::IDLE;
static hal::lora::LoRaError lastError = hal::lora::LoRaError::NONE;

static hal::lora::JoinCallback joinCallback = nullptr;
static hal::lora::TxCallback txCallback = nullptr;
static hal::lora::RxCallback rxCallback = nullptr;

static int16_t lastRssi = 0;
static int8_t lastSnr = 0;

namespace hal {
namespace lora {

// ============================================================
// ERROR HANDLING
// ============================================================

static LoRaError translateRadioLibError(int16_t err) {
    switch (err) {
        case RADIOLIB_ERR_NONE:
            return LoRaError::NONE;
        case RADIOLIB_ERR_CHIP_NOT_FOUND:
        case RADIOLIB_ERR_SPI_CMD_FAILED:
            return LoRaError::RADIO_INIT_FAILED;
        case RADIOLIB_ERR_JOIN_NONCE_INVALID:
            return LoRaError::JOIN_REJECTED;
        case RADIOLIB_ERR_TX_TIMEOUT:
            return LoRaError::TX_TIMEOUT;
        case RADIOLIB_ERR_PACKET_TOO_LONG:
            return LoRaError::PAYLOAD_TOO_LARGE;
        default:
            if (err < 0) {
                return LoRaError::UNKNOWN;
            }
            return LoRaError::NONE;
    }
}

const char* get_error_message(LoRaError error) {
    switch (error) {
        case LoRaError::NONE:
            return "No error";
        case LoRaError::RADIO_INIT_FAILED:
            return "Radio initialization failed";
        case LoRaError::JOIN_TIMEOUT:
            return "Join timeout";
        case LoRaError::JOIN_REJECTED:
            return "Join rejected by network";
        case LoRaError::TX_FAILED:
            return "Transmission failed";
        case LoRaError::TX_TIMEOUT:
            return "Transmission timeout";
        case LoRaError::DUTY_CYCLE_LIMITED:
            return "Duty cycle limit exceeded";
        case LoRaError::PAYLOAD_TOO_LARGE:
            return "Payload too large";
        case LoRaError::NOT_JOINED:
            return "Not joined to network";
        case LoRaError::INVALID_CREDENTIALS:
            return "Invalid credentials";
        default:
            return "Unknown error";
    }
}

LoRaError get_last_error() {
    return lastError;
}

// ============================================================
// RADIO INITIALIZATION
// ============================================================

bool init() {
    if (radioInitialized) {
        return true;
    }

    LOG_INFO("Initializing LoRa radio (SX1262)...");

    // Initialize SPI bus
    loraSPI.begin(LORA_SCK, LORA_MISO, LORA_MOSI, LORA_CS);

    // Initialize radio
    int16_t state = radio.begin();
    if (state != RADIOLIB_ERR_NONE) {
        LOG_ERROR("Radio init failed with error: %d", state);
        lastError = translateRadioLibError(state);
        return false;
    }

    // Configure TCXO voltage for SX1262
    state = radio.setTCXO(LORA_TCXO_VOLTAGE);
    if (state != RADIOLIB_ERR_NONE) {
        LOG_WARN("TCXO config failed: %d (may work anyway)", state);
    }

    // Create LoRaWAN node
    node = new LoRaWANNode(&radio, band);

    // Configure LoRaWAN parameters
    node->setADR(LORAWAN_ADR_ENABLED);
    node->setDatarate(LORAWAN_DEFAULT_DR);

    radioInitialized = true;
    radioSleeping = false;
    LOG_INFO("LoRa radio initialized successfully");

    return true;
}

void shutdown() {
    if (!radioInitialized) return;

    sleep();

    if (node != nullptr) {
        delete node;
        node = nullptr;
    }

    radioInitialized = false;
    LOG_INFO("LoRa radio shut down");
}

bool is_initialized() {
    return radioInitialized;
}

// ============================================================
// LORAWAN NETWORK JOIN
// ============================================================

bool join_otaa(
    const uint8_t* devEui,
    const uint8_t* appEui,
    const uint8_t* appKey,
    JoinCallback callback
) {
    if (!radioInitialized || node == nullptr) {
        LOG_ERROR("Radio not initialized");
        lastError = LoRaError::RADIO_INIT_FAILED;
        if (callback) callback(false, lastError);
        return false;
    }

    // Wake radio if sleeping
    if (radioSleeping) {
        wake();
    }

    joinCallback = callback;
    currentJoinStatus = JoinStatus::JOINING;

    LOG_INFO("Starting OTAA join...");

    // Convert byte arrays to uint64_t (LSB first)
    uint64_t devEui64 = 0;
    uint64_t appEui64 = 0;
    for (int i = 0; i < 8; i++) {
        devEui64 |= ((uint64_t)devEui[i]) << (i * 8);
        appEui64 |= ((uint64_t)appEui[i]) << (i * 8);
    }

    // In RadioLib 6.x, beginOTAA just stores credentials, activateOTAA does the join
    node->beginOTAA(appEui64, devEui64, (uint8_t*)appKey, nullptr);

    // Attempt OTAA join - activateOTAA returns the state
    int16_t state = node->activateOTAA();

    if (state == RADIOLIB_LORAWAN_NEW_SESSION || state == RADIOLIB_LORAWAN_SESSION_RESTORED) {
        currentJoinStatus = JoinStatus::JOINED;
        lastError = LoRaError::NONE;
        LOG_INFO("OTAA join successful!");

        // Get link metrics
        lastRssi = radio.getRSSI();
        lastSnr = radio.getSNR();

        if (joinCallback) {
            joinCallback(true, LoRaError::NONE);
        }
        return true;
    } else {
        currentJoinStatus = JoinStatus::JOIN_FAILED;
        lastError = translateRadioLibError(state);
        LOG_ERROR("OTAA join failed: %s (code %d)", get_error_message(lastError), state);

        if (joinCallback) {
            joinCallback(false, lastError);
        }
        return false;
    }
}

bool activate_abp(
    const uint8_t* devAddr,
    const uint8_t* nwkSKey,
    const uint8_t* appSKey
) {
    if (!radioInitialized || node == nullptr) {
        LOG_ERROR("Radio not initialized");
        lastError = LoRaError::RADIO_INIT_FAILED;
        return false;
    }

    // Wake radio if sleeping
    if (radioSleeping) {
        wake();
    }

    LOG_INFO("Activating ABP session...");

    // Convert devAddr to uint32_t
    uint32_t devAddr32 = 0;
    for (int i = 0; i < 4; i++) {
        devAddr32 |= ((uint32_t)devAddr[i]) << (i * 8);
    }

    // RadioLib 6.x ABP API: beginABP(devAddr, fNwkSIntKey, sNwkSIntKey, nwkSEncKey, appSKey)
    // For LoRaWAN 1.0.x compatibility, use same key for all network session keys
    node->beginABP(devAddr32, (uint8_t*)nwkSKey, (uint8_t*)nwkSKey, (uint8_t*)nwkSKey, (uint8_t*)appSKey);

    // Mark as joined (ABP doesn't need activation)
    currentJoinStatus = JoinStatus::JOINED;
    lastError = LoRaError::NONE;
    LOG_INFO("ABP activation successful!");
    return true;
}

JoinStatus get_join_status() {
    return currentJoinStatus;
}

bool is_joined() {
    return currentJoinStatus == JoinStatus::JOINED;
}

// ============================================================
// DATA TRANSMISSION
// ============================================================

bool send(
    uint8_t port,
    const uint8_t* data,
    size_t len,
    bool confirmed,
    TxCallback callback
) {
    if (!radioInitialized || node == nullptr) {
        LOG_ERROR("Radio not initialized");
        lastError = LoRaError::RADIO_INIT_FAILED;
        if (callback) callback(false, lastError);
        return false;
    }

    if (currentJoinStatus != JoinStatus::JOINED) {
        LOG_ERROR("Cannot send: not joined to network");
        lastError = LoRaError::NOT_JOINED;
        if (callback) callback(false, lastError);
        return false;
    }

    // Wake radio if sleeping
    if (radioSleeping) {
        wake();
    }

    txCallback = callback;
    currentTxStatus = TxStatus::TRANSMITTING;

    LOG_INFO("Sending uplink (port %d, %d bytes, %s)...",
             port, len, confirmed ? "confirmed" : "unconfirmed");

    // Debug: Print payload hex
    LOG_DEBUG("Payload: ");
    for (size_t i = 0; i < len; i++) {
        LOG_DEBUG("%02X ", data[i]);
    }

    int16_t state;
    if (confirmed) {
        // Confirmed uplink with downlink reception
        uint8_t rxData[256];
        size_t rxLen = 0;
        state = node->sendReceive((uint8_t*)data, len, port, rxData, &rxLen);

        // Handle downlink if received
        if (rxLen > 0 && rxCallback) {
            rxCallback(port, rxData, rxLen);
        }
    } else {
        // Unconfirmed uplink
        state = node->uplink((uint8_t*)data, len, port);
    }

    if (state == RADIOLIB_ERR_NONE || state == RADIOLIB_LORAWAN_NO_DOWNLINK) {
        currentTxStatus = TxStatus::TX_COMPLETE;
        lastError = LoRaError::NONE;

        // Update link metrics
        lastRssi = radio.getRSSI();
        lastSnr = radio.getSNR();

        LOG_INFO("Uplink sent successfully (RSSI: %d dBm, SNR: %d dB)",
                 lastRssi, lastSnr);

        if (txCallback) {
            txCallback(true, LoRaError::NONE);
        }
        return true;
    } else {
        currentTxStatus = TxStatus::TX_FAILED;
        lastError = translateRadioLibError(state);
        LOG_ERROR("Uplink failed: %s (code %d)", get_error_message(lastError), state);

        if (txCallback) {
            txCallback(false, lastError);
        }
        return false;
    }
}

TxStatus get_tx_status() {
    return currentTxStatus;
}

bool is_tx_ready() {
    return currentTxStatus == TxStatus::IDLE ||
           currentTxStatus == TxStatus::TX_COMPLETE ||
           currentTxStatus == TxStatus::TX_FAILED;
}

uint32_t get_time_until_tx() {
    // RadioLib handles duty cycle internally
    // Return 0 for now (ready to transmit)
    return 0;
}

// ============================================================
// DATA RECEPTION
// ============================================================

void set_rx_callback(RxCallback callback) {
    rxCallback = callback;
}

RxStatus check_rx() {
    // RadioLib handles RX during sendReceive()
    return RxStatus::NO_DATA;
}

// ============================================================
// RADIO CONFIGURATION
// ============================================================

void set_adr(bool enable) {
    if (node != nullptr) {
        node->setADR(enable);
        LOG_INFO("ADR %s", enable ? "enabled" : "disabled");
    }
}

bool get_adr() {
    // RadioLib doesn't expose ADR status directly
    return LORAWAN_ADR_ENABLED;
}

bool set_data_rate(uint8_t dr) {
    if (node != nullptr) {
        node->setDatarate(dr);
        LOG_INFO("Data rate set to DR%d", dr);
        return true;
    }
    return false;
}

uint8_t get_data_rate() {
    // RadioLib doesn't expose current DR directly
    return LORAWAN_DEFAULT_DR;
}

bool set_tx_power(int8_t power) {
    if (radioInitialized) {
        int16_t state = radio.setOutputPower(power);
        if (state == RADIOLIB_ERR_NONE) {
            LOG_INFO("TX power set to %d dBm", power);
            return true;
        }
    }
    return false;
}

int8_t get_tx_power() {
    // Default EU868 TX power
    return 14;
}

// ============================================================
// RADIO STATUS & METRICS
// ============================================================

int16_t get_last_rssi() {
    return lastRssi;
}

int8_t get_last_snr() {
    return lastSnr;
}

uint8_t get_spreading_factor() {
    // Default SF7 for DR5
    return 7;
}

float get_bandwidth() {
    // EU868 uses 125 kHz bandwidth
    return 125.0f;
}

uint32_t get_frame_counter_up() {
    if (node != nullptr) {
        return node->getFCntUp();
    }
    return 0;
}

uint32_t get_frame_counter_down() {
    // RadioLib doesn't expose downlink frame counter directly
    return 0;
}

// ============================================================
// POWER MANAGEMENT
// ============================================================

void sleep() {
    if (!radioInitialized) return;

    radio.sleep();
    radioSleeping = true;
    LOG_DEBUG("Radio entered sleep mode");
}

void wake() {
    if (!radioInitialized) return;

    radio.standby();
    radioSleeping = false;
    LOG_DEBUG("Radio woken from sleep");
}

bool is_sleeping() {
    return radioSleeping;
}

// ============================================================
// EVENT PROCESSING
// ============================================================

void process() {
    // RadioLib handles events internally
    // This function can be used for periodic tasks
}

// ============================================================
// DEBUG & DIAGNOSTICS
// ============================================================

void print_status() {
    LOG_INFO("=== LoRa Radio Status ===");
    LOG_INFO("Initialized: %s", radioInitialized ? "Yes" : "No");
    LOG_INFO("Sleeping: %s", radioSleeping ? "Yes" : "No");
    LOG_INFO("Join Status: %d", (int)currentJoinStatus);
    LOG_INFO("TX Status: %d", (int)currentTxStatus);
    LOG_INFO("Last RSSI: %d dBm", lastRssi);
    LOG_INFO("Last SNR: %d dB", lastSnr);
    LOG_INFO("Frame Counter Up: %u", get_frame_counter_up());
    LOG_INFO("Free Heap: %u bytes", hal::get_free_heap());
    LOG_INFO("=========================");
}

} // namespace lora
} // namespace hal
