/**
 * @file hal_lora_sim.cpp
 * @brief Simulated LoRaWAN HAL for Native/Linux testing
 *
 * Provides simulation of LoRaWAN functions for testing without hardware.
 *
 * @version 1.0.0
 * @date 2025-12-10
 */

#include "hal/hal_lora.h"
#include "hal/hal.h"

#include <iostream>
#include <cstring>

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

static int16_t lastRssi = -50;
static int8_t lastSnr = 10;
static uint32_t frameCounterUp = 0;
static uint32_t frameCounterDown = 0;
static bool adrEnabled = true;
static uint8_t currentDataRate = 5;
static int8_t currentTxPower = 14;

namespace hal {
namespace lora {

// ============================================================
// ERROR HANDLING
// ============================================================

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
// RADIO INITIALIZATION (Simulated)
// ============================================================

bool init() {
    if (radioInitialized) {
        return true;
    }

    std::cout << "[SIM] LoRa radio initialized (simulated)" << std::endl;
    radioInitialized = true;
    radioSleeping = false;
    return true;
}

void shutdown() {
    radioInitialized = false;
    std::cout << "[SIM] LoRa radio shut down" << std::endl;
}

bool is_initialized() {
    return radioInitialized;
}

// ============================================================
// LORAWAN NETWORK JOIN (Simulated)
// ============================================================

bool join_otaa(
    const uint8_t* devEui,
    const uint8_t* appEui,
    const uint8_t* appKey,
    JoinCallback callback
) {
    if (!radioInitialized) {
        lastError = LoRaError::RADIO_INIT_FAILED;
        if (callback) callback(false, lastError);
        return false;
    }

    joinCallback = callback;
    currentJoinStatus = JoinStatus::JOINING;

    std::cout << "[SIM] OTAA Join requested" << std::endl;
    std::cout << "[SIM] DevEUI: ";
    for (int i = 0; i < 8; i++) std::cout << std::hex << (int)devEui[i];
    std::cout << std::dec << std::endl;

    // Simulate successful join
    hal::delay_ms(100);  // Simulate join delay

    currentJoinStatus = JoinStatus::JOINED;
    lastError = LoRaError::NONE;
    std::cout << "[SIM] OTAA Join successful" << std::endl;

    if (joinCallback) {
        joinCallback(true, LoRaError::NONE);
    }
    return true;
}

bool activate_abp(
    const uint8_t* devAddr,
    const uint8_t* nwkSKey,
    const uint8_t* appSKey
) {
    if (!radioInitialized) {
        lastError = LoRaError::RADIO_INIT_FAILED;
        return false;
    }

    currentJoinStatus = JoinStatus::JOINED;
    std::cout << "[SIM] ABP activation successful" << std::endl;
    return true;
}

JoinStatus get_join_status() {
    return currentJoinStatus;
}

bool is_joined() {
    return currentJoinStatus == JoinStatus::JOINED;
}

// ============================================================
// DATA TRANSMISSION (Simulated)
// ============================================================

bool send(
    uint8_t port,
    const uint8_t* data,
    size_t len,
    bool confirmed,
    TxCallback callback
) {
    if (!radioInitialized) {
        lastError = LoRaError::RADIO_INIT_FAILED;
        if (callback) callback(false, lastError);
        return false;
    }

    if (currentJoinStatus != JoinStatus::JOINED) {
        lastError = LoRaError::NOT_JOINED;
        if (callback) callback(false, lastError);
        return false;
    }

    txCallback = callback;
    currentTxStatus = TxStatus::TRANSMITTING;

    std::cout << "[SIM] Sending uplink (port " << (int)port
              << ", " << len << " bytes, "
              << (confirmed ? "confirmed" : "unconfirmed") << ")" << std::endl;

    // Print payload
    std::cout << "[SIM] Payload: ";
    for (size_t i = 0; i < len; i++) {
        printf("%02X ", data[i]);
    }
    std::cout << std::endl;

    // Simulate transmission
    hal::delay_ms(50);

    frameCounterUp++;
    currentTxStatus = TxStatus::TX_COMPLETE;
    lastError = LoRaError::NONE;

    // Simulate RSSI/SNR
    lastRssi = -50 - (rand() % 30);  // -50 to -80 dBm
    lastSnr = 10 - (rand() % 5);     // 5 to 10 dB

    std::cout << "[SIM] Uplink sent (FC: " << frameCounterUp
              << ", RSSI: " << lastRssi << " dBm, SNR: " << (int)lastSnr << " dB)"
              << std::endl;

    if (txCallback) {
        txCallback(true, LoRaError::NONE);
    }
    return true;
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
    return 0;
}

// ============================================================
// DATA RECEPTION (Simulated)
// ============================================================

void set_rx_callback(RxCallback callback) {
    rxCallback = callback;
}

RxStatus check_rx() {
    return RxStatus::NO_DATA;
}

// ============================================================
// RADIO CONFIGURATION
// ============================================================

void set_adr(bool enable) {
    adrEnabled = enable;
    std::cout << "[SIM] ADR " << (enable ? "enabled" : "disabled") << std::endl;
}

bool get_adr() {
    return adrEnabled;
}

bool set_data_rate(uint8_t dr) {
    currentDataRate = dr;
    std::cout << "[SIM] Data rate set to DR" << (int)dr << std::endl;
    return true;
}

uint8_t get_data_rate() {
    return currentDataRate;
}

bool set_tx_power(int8_t power) {
    currentTxPower = power;
    std::cout << "[SIM] TX power set to " << (int)power << " dBm" << std::endl;
    return true;
}

int8_t get_tx_power() {
    return currentTxPower;
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
    // DR5 = SF7 for EU868
    return 12 - currentDataRate;
}

float get_bandwidth() {
    return 125.0f;
}

uint32_t get_frame_counter_up() {
    return frameCounterUp;
}

uint32_t get_frame_counter_down() {
    return frameCounterDown;
}

// ============================================================
// POWER MANAGEMENT
// ============================================================

void sleep() {
    radioSleeping = true;
    std::cout << "[SIM] Radio entered sleep mode" << std::endl;
}

void wake() {
    radioSleeping = false;
    std::cout << "[SIM] Radio woken from sleep" << std::endl;
}

bool is_sleeping() {
    return radioSleeping;
}

// ============================================================
// EVENT PROCESSING
// ============================================================

void process() {
    // Nothing to do in simulation
}

// ============================================================
// DEBUG & DIAGNOSTICS
// ============================================================

void print_status() {
    std::cout << "=== LoRa Radio Status (Simulated) ===" << std::endl;
    std::cout << "Initialized: " << (radioInitialized ? "Yes" : "No") << std::endl;
    std::cout << "Sleeping: " << (radioSleeping ? "Yes" : "No") << std::endl;
    std::cout << "Join Status: " << (int)currentJoinStatus << std::endl;
    std::cout << "TX Status: " << (int)currentTxStatus << std::endl;
    std::cout << "Last RSSI: " << lastRssi << " dBm" << std::endl;
    std::cout << "Last SNR: " << (int)lastSnr << " dB" << std::endl;
    std::cout << "Frame Counter Up: " << frameCounterUp << std::endl;
    std::cout << "======================================" << std::endl;
}

} // namespace lora
} // namespace hal
