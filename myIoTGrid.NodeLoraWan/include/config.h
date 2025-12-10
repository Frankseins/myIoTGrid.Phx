/**
 * @file config.h
 * @brief Configuration constants for myIoTGrid NodeLoraWan
 *
 * This file contains all compile-time configuration constants for the
 * LoRaWAN sensor node firmware targeting Heltec LoRa32 V3.
 *
 * @version 1.0.0
 * @date 2025-12-10
 * @author myIoTGrid Team
 *
 * Sprint: LoRa-02 - Grid.Sensor LoRaWAN Firmware
 * Hackathon: 12./13. Dezember 2025
 */

#pragma once

#include <cstdint>

// ============================================================
// FIRMWARE VERSION
// ============================================================
// WICHTIG: Bei JEDER Änderung Version erhöhen!
// - X (Major): Breaking Changes, neue Architektur
// - Y (Minor): Neue Features, neue Sensoren
// - Z (Patch): Bugfixes, kleine Verbesserungen
#ifndef FIRMWARE_VERSION
#define FIRMWARE_VERSION "1.0.0"
#endif

// ============================================================
// LoRaWAN CONFIGURATION
// ============================================================

// EU868 Frequenzband (Standard für Europa)
#ifndef LORA_BAND
#define LORA_BAND 868E6
#endif

// LoRaWAN 1.0.x Subband (EU868: 1-8)
#define LORAWAN_SUBBAND 1

// Default Data Rate (SF7 = DR5 für EU868)
#define LORAWAN_DEFAULT_DR 5

// ADR (Adaptive Data Rate) aktivieren
#define LORAWAN_ADR_ENABLED true

// Confirmed Uplinks (Standard: unconfirmed für Batterie-Schonung)
#define LORAWAN_CONFIRMED_UPLINKS false

// Uplink Port für Sensor-Daten
#define LORAWAN_SENSOR_PORT 1

// Downlink Port für Konfiguration
#define LORAWAN_CONFIG_PORT 10

// ============================================================
// HELTEC LORA32 V3 PIN DEFINITIONS
// ============================================================

// --- SX1262 LoRa Radio ---
#ifndef LORA_SCK
#define LORA_SCK 9
#endif

#ifndef LORA_MISO
#define LORA_MISO 11
#endif

#ifndef LORA_MOSI
#define LORA_MOSI 10
#endif

#ifndef LORA_CS
#define LORA_CS 8
#endif

#ifndef LORA_RST
#define LORA_RST 12
#endif

#ifndef LORA_DIO1
#define LORA_DIO1 14
#endif

#ifndef LORA_BUSY
#define LORA_BUSY 13
#endif

// TCXO Spannung für SX1262 (Heltec V3 verwendet 1.8V)
#ifndef LORA_TCXO_VOLTAGE
#define LORA_TCXO_VOLTAGE 1.8
#endif

// --- OLED Display (SSD1306 128x64) ---
#ifndef OLED_SDA
#define OLED_SDA 17
#endif

#ifndef OLED_SCL
#define OLED_SCL 18
#endif

#ifndef OLED_RST
#define OLED_RST 21
#endif

#ifndef OLED_WIDTH
#define OLED_WIDTH 128
#endif

#ifndef OLED_HEIGHT
#define OLED_HEIGHT 64
#endif

#ifndef OLED_ADDRESS
#define OLED_ADDRESS 0x3C
#endif

// --- I2C Bus für Sensoren ---
#ifndef I2C_SDA
#define I2C_SDA 4
#endif

#ifndef I2C_SCL
#define I2C_SCL 15
#endif

// I2C Frequenz
#define I2C_FREQUENCY 400000

// --- Battery Monitoring ---
#ifndef BATTERY_ADC_PIN
#define BATTERY_ADC_PIN 1
#endif

#ifndef BATTERY_DIVIDER_RATIO
#define BATTERY_DIVIDER_RATIO 4.9f
#endif

// Batterie-Schwellwerte
#define BATTERY_MIN_VOLTAGE 3.0f
#define BATTERY_MAX_VOLTAGE 4.2f
#define BATTERY_LOW_THRESHOLD 20  // % unter dem länger geschlafen wird

// --- User Button & LED ---
#ifndef USER_BUTTON_PIN
#define USER_BUTTON_PIN 0
#endif

#ifndef LED_PIN
#define LED_PIN 35
#endif

// --- Ultrasonic Sensor (Water Level) ---
#ifndef ULTRASONIC_TRIG_PIN
#define ULTRASONIC_TRIG_PIN 5
#endif

#ifndef ULTRASONIC_ECHO_PIN
#define ULTRASONIC_ECHO_PIN 4
#endif

// ============================================================
// SENSOR CONFIGURATION
// ============================================================

// Sensor-Adressen (I2C)
#define BME280_ADDRESS_PRIMARY 0x76
#define BME280_ADDRESS_SECONDARY 0x77
#define BME680_ADDRESS_PRIMARY 0x76
#define BME680_ADDRESS_SECONDARY 0x77
#define SHT31_ADDRESS_PRIMARY 0x44
#define SHT31_ADDRESS_SECONDARY 0x45

// Ultraschall-Sensor Konfiguration
#define WATER_LEVEL_MOUNT_HEIGHT_CM 200.0f  // Sensor-Höhe über Grund
#define WATER_LEVEL_ALARM_THRESHOLD_CM 150.0f  // Alarm-Schwelle
#define WATER_LEVEL_TIMEOUT_US 30000  // Echo-Timeout in Mikrosekunden

// Median-Filter für stabile Messungen
#define WATER_LEVEL_FILTER_SIZE 5

// ============================================================
// TIMING CONFIGURATION
// ============================================================

// Standard-Übertragungsintervall (Sekunden)
#define DEFAULT_TX_INTERVAL_SECONDS 300  // 5 Minuten

// Minimum Intervall (Sekunden)
#define MIN_TX_INTERVAL_SECONDS 60  // 1 Minute (Fair Use Policy)

// Maximum Intervall (Sekunden)
#define MAX_TX_INTERVAL_SECONDS 3600  // 1 Stunde

// Join-Retry Interval (Sekunden)
#define JOIN_RETRY_INTERVAL_SECONDS 30

// Maximum Join-Versuche bevor Deep Sleep
#define MAX_JOIN_RETRIES 10

// OLED Auto-Off Timeout (Millisekunden)
#define OLED_AUTO_OFF_MS 30000  // 30 Sekunden

// ============================================================
// DEEP SLEEP CONFIGURATION
// ============================================================

// Deep Sleep aktivieren
#define DEEP_SLEEP_ENABLED true

// Zusätzliche Schlafzeit bei niedrigem Batteriestand (Faktor)
#define LOW_BATTERY_SLEEP_MULTIPLIER 2.0f

// Minimum Deep Sleep Zeit (Sekunden)
#define MIN_DEEP_SLEEP_SECONDS 10

// ============================================================
// PAYLOAD CONFIGURATION
// ============================================================

// Maximum Payload-Größe (EU868 DR0/SF12: 51 bytes, DR5/SF7: 242 bytes)
#define MAX_PAYLOAD_SIZE 51  // Konservativ für alle Data Rates

// Sensor Type IDs für Payload-Encoding
namespace SensorTypeId {
    constexpr uint8_t TEMPERATURE = 0x01;
    constexpr uint8_t HUMIDITY = 0x02;
    constexpr uint8_t PRESSURE = 0x03;
    constexpr uint8_t WATER_LEVEL = 0x04;
    constexpr uint8_t BATTERY = 0x05;
    constexpr uint8_t CO2 = 0x06;
    constexpr uint8_t PM25 = 0x07;
    constexpr uint8_t PM10 = 0x08;
    constexpr uint8_t LIGHT = 0x09;
    constexpr uint8_t UV = 0x0A;
    constexpr uint8_t SOIL_MOISTURE = 0x0B;
    constexpr uint8_t WIND_SPEED = 0x0C;
    constexpr uint8_t RAINFALL = 0x0D;
    constexpr uint8_t RSSI = 0x0E;
    constexpr uint8_t SNR = 0x0F;
    constexpr uint8_t UNKNOWN = 0xFF;
}

// ============================================================
// DEBUG CONFIGURATION
// ============================================================

// Debug-Level (0: OFF, 1: ERROR, 2: WARN, 3: INFO, 4: DEBUG)
#ifndef DEBUG_LEVEL
#define DEBUG_LEVEL 3
#endif

// Serial Baudrate
#define SERIAL_BAUD 115200

// Debug-Makros
#if DEBUG_LEVEL >= 1
#define LOG_ERROR(fmt, ...) Serial.printf("[ERROR] " fmt "\n", ##__VA_ARGS__)
#else
#define LOG_ERROR(fmt, ...)
#endif

#if DEBUG_LEVEL >= 2
#define LOG_WARN(fmt, ...) Serial.printf("[WARN]  " fmt "\n", ##__VA_ARGS__)
#else
#define LOG_WARN(fmt, ...)
#endif

#if DEBUG_LEVEL >= 3
#define LOG_INFO(fmt, ...) Serial.printf("[INFO]  " fmt "\n", ##__VA_ARGS__)
#else
#define LOG_INFO(fmt, ...)
#endif

#if DEBUG_LEVEL >= 4
#define LOG_DEBUG(fmt, ...) Serial.printf("[DEBUG] " fmt "\n", ##__VA_ARGS__)
#else
#define LOG_DEBUG(fmt, ...)
#endif

// ============================================================
// NVS (Non-Volatile Storage) KEYS
// ============================================================
namespace NvsKeys {
    constexpr const char* NAMESPACE = "lorawan";
    constexpr const char* APP_EUI = "appEui";
    constexpr const char* APP_KEY = "appKey";
    constexpr const char* DEV_ADDR = "devAddr";
    constexpr const char* NWK_S_KEY = "nwkSKey";
    constexpr const char* APP_S_KEY = "appSKey";
    constexpr const char* FRAME_COUNTER = "frameCounter";
    constexpr const char* TX_INTERVAL = "txInterval";
    constexpr const char* DEVICE_NAME = "deviceName";
}

// ============================================================
// CHIRPSTACK / TTN CONFIGURATION
// ============================================================

// Default Application EUI (muss konfiguriert werden)
// Format: LSB (Little Endian) - wie von TTN/ChirpStack erwartet
#define DEFAULT_APP_EUI { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }

// Default Application Key (muss konfiguriert werden)
// Format: MSB (Big Endian)
#define DEFAULT_APP_KEY { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, \
                          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }

// ============================================================
// STATUS LED PATTERNS
// ============================================================
namespace LedPattern {
    constexpr uint16_t BOOT = 100;           // Schnelles Blinken beim Start
    constexpr uint16_t JOINING = 500;        // Langsames Blinken während Join
    constexpr uint16_t JOINED = 0;           // Aus nach erfolgreichem Join
    constexpr uint16_t TRANSMITTING = 50;    // Sehr schnelles Blinken bei TX
    constexpr uint16_t ERROR = 200;          // Mittleres Blinken bei Fehler
    constexpr uint16_t LOW_BATTERY = 2000;   // Sehr langsam bei niedrigem Akku
}
