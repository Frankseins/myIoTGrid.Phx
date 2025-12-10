/**
 * @file lora_credentials.h
 * @brief LoRaWAN Credentials Management
 *
 * Handles secure storage and retrieval of LoRaWAN credentials.
 * DevEUI is generated from ESP32 MAC address.
 * AppEUI and AppKey are stored in NVS (Non-Volatile Storage).
 *
 * @version 1.0.0
 * @date 2025-12-10
 *
 * Sprint: LoRa-02 - Grid.Sensor LoRaWAN Firmware
 */

#pragma once

#include <cstdint>
#include <string>
#include <array>

/**
 * @brief LoRaWAN Credentials Structure
 *
 * Contains all credentials needed for OTAA and ABP activation.
 */
struct LoRaCredentials {
    // === OTAA Credentials ===
    std::array<uint8_t, 8> devEui;      ///< Device EUI (from MAC address)
    std::array<uint8_t, 8> appEui;      ///< Application EUI (JoinEUI)
    std::array<uint8_t, 16> appKey;     ///< Application Key (AES-128)

    // === ABP Credentials (Fallback) ===
    std::array<uint8_t, 4> devAddr;     ///< Device Address
    std::array<uint8_t, 16> nwkSKey;    ///< Network Session Key
    std::array<uint8_t, 16> appSKey;    ///< Application Session Key

    // === Session State ===
    uint32_t frameCounterUp = 0;        ///< Uplink Frame Counter
    uint32_t frameCounterDown = 0;      ///< Downlink Frame Counter

    /**
     * @brief Check if OTAA credentials are configured
     * @return true if AppEUI and AppKey are non-zero
     */
    bool hasOtaaCredentials() const;

    /**
     * @brief Check if ABP credentials are configured
     * @return true if DevAddr, NwkSKey, AppSKey are non-zero
     */
    bool hasAbpCredentials() const;

    /**
     * @brief Get DevEUI as hex string (LSB format)
     * @return DevEUI as 16-character hex string
     */
    std::string getDevEuiString() const;

    /**
     * @brief Get AppEUI as hex string (LSB format)
     * @return AppEUI as 16-character hex string
     */
    std::string getAppEuiString() const;

    /**
     * @brief Clear all credentials
     */
    void clear();
};

/**
 * @brief LoRaWAN Credential Manager
 *
 * Manages loading, saving, and generating LoRaWAN credentials.
 */
class CredentialManager {
public:
    CredentialManager();
    ~CredentialManager() = default;

    // === Initialization ===

    /**
     * @brief Initialize credential manager
     *
     * Generates DevEUI from MAC and loads stored credentials.
     *
     * @return true if initialized successfully
     */
    bool init();

    // === Credential Access ===

    /**
     * @brief Get current credentials
     * @return Reference to credentials structure
     */
    const LoRaCredentials& getCredentials() const { return credentials_; }

    /**
     * @brief Get DevEUI
     * @return Pointer to 8-byte DevEUI array
     */
    const uint8_t* getDevEui() const { return credentials_.devEui.data(); }

    /**
     * @brief Get AppEUI
     * @return Pointer to 8-byte AppEUI array
     */
    const uint8_t* getAppEui() const { return credentials_.appEui.data(); }

    /**
     * @brief Get AppKey
     * @return Pointer to 16-byte AppKey array
     */
    const uint8_t* getAppKey() const { return credentials_.appKey.data(); }

    // === Credential Management ===

    /**
     * @brief Generate DevEUI from ESP32 MAC address
     *
     * Creates EUI-64 format: MAC[0-2] + 0xFF + 0xFE + MAC[3-5]
     *
     * @return true if generated successfully
     */
    bool generateDevEui();

    /**
     * @brief Set DevEUI from hex string (override MAC-based generation)
     * @param hexString 16-character hex string
     * @return true if parsed successfully
     */
    bool setDevEui(const std::string& hexString);

    /**
     * @brief Set AppEUI from hex string
     * @param hexString 16-character hex string (LSB format)
     * @return true if parsed successfully
     */
    bool setAppEui(const std::string& hexString);

    /**
     * @brief Set AppEUI from byte array
     * @param eui 8-byte array (LSB format)
     */
    void setAppEui(const uint8_t* eui);

    /**
     * @brief Set AppKey from hex string
     * @param hexString 32-character hex string (MSB format)
     * @return true if parsed successfully
     */
    bool setAppKey(const std::string& hexString);

    /**
     * @brief Set AppKey from byte array
     * @param key 16-byte array (MSB format)
     */
    void setAppKey(const uint8_t* key);

    /**
     * @brief Set ABP credentials
     * @param devAddr 4-byte device address
     * @param nwkSKey 16-byte network session key
     * @param appSKey 16-byte application session key
     */
    void setAbpCredentials(const uint8_t* devAddr, const uint8_t* nwkSKey, const uint8_t* appSKey);

    // === Persistence ===

    /**
     * @brief Load credentials from NVS
     * @return true if loaded successfully
     */
    bool loadFromNvs();

    /**
     * @brief Save credentials to NVS
     * @return true if saved successfully
     */
    bool saveToNvs();

    /**
     * @brief Clear credentials from NVS
     * @return true if cleared successfully
     */
    bool clearNvs();

    /**
     * @brief Load credentials from secrets.h (compile-time)
     * @return true if loaded successfully
     */
    bool loadFromSecrets();

    // === Frame Counter Management ===

    /**
     * @brief Save frame counters to NVS
     *
     * Should be called periodically to preserve counters across reboots.
     *
     * @return true if saved successfully
     */
    bool saveFrameCounters();

    /**
     * @brief Load frame counters from NVS
     * @return true if loaded successfully
     */
    bool loadFrameCounters();

    /**
     * @brief Increment uplink frame counter
     * @return New frame counter value
     */
    uint32_t incrementFrameCounterUp();

    /**
     * @brief Set downlink frame counter
     * @param counter New counter value
     */
    void setFrameCounterDown(uint32_t counter);

    // === Serial Configuration Interface ===

    /**
     * @brief Handle serial configuration commands
     *
     * Processes commands like:
     * - APPEUI=0011223344556677
     * - APPKEY=00112233445566778899AABBCCDDEEFF
     * - SAVE
     * - SHOW
     */
    void handleSerialConfig();

    /**
     * @brief Print credentials to serial (partially masked)
     */
    void printCredentials() const;

    // === Validation ===

    /**
     * @brief Check if credentials are valid for OTAA join
     * @return true if ready for OTAA
     */
    bool isReadyForOtaa() const;

    /**
     * @brief Check if credentials are valid for ABP activation
     * @return true if ready for ABP
     */
    bool isReadyForAbp() const;

private:
    LoRaCredentials credentials_;
    bool initialized_ = false;

    /**
     * @brief Parse hex string to byte array
     * @param hex Hex string
     * @param output Output byte array
     * @param length Expected length in bytes
     * @return true if parsed successfully
     */
    static bool parseHexString(const std::string& hex, uint8_t* output, size_t length);

    /**
     * @brief Convert byte array to hex string
     * @param data Input byte array
     * @param length Array length
     * @return Hex string
     */
    static std::string toHexString(const uint8_t* data, size_t length);

    /**
     * @brief Reverse byte array (for LSB/MSB conversion)
     * @param data Array to reverse
     * @param length Array length
     */
    static void reverseBytes(uint8_t* data, size_t length);
};

// ============================================================
// SECRETS HEADER (Git-ignored, for development)
// ============================================================

#if __has_include("secrets.h")
#include "secrets.h"
#else

// Default credentials (will be overwritten by NVS or serial config)
#ifndef LORAWAN_APP_EUI
#define LORAWAN_APP_EUI { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }
#endif

#ifndef LORAWAN_APP_KEY
#define LORAWAN_APP_KEY { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, \
                          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }
#endif

#endif // __has_include("secrets.h")
