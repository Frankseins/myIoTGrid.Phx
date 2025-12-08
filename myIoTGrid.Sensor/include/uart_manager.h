/**
 * myIoTGrid.Sensor - UART Manager
 *
 * Dynamic UART port allocation for ESP32 based on pin configuration.
 * ESP32 has 3 UARTs (0, 1, 2) with GPIO matrix allowing any UART on any pin.
 *
 * UART0 = Reserved for USB serial (pins 1/3)
 * UART1 = Available for sensors (default pins 9/10 are flash, but can remap)
 * UART2 = Available for sensors (default pins 16/17)
 *
 * This manager dynamically allocates UART1 or UART2 based on availability,
 * preventing conflicts when multiple UART devices are used.
 */

#ifndef UART_MANAGER_H
#define UART_MANAGER_H

#include <Arduino.h>

#ifdef PLATFORM_ESP32
#include <HardwareSerial.h>
#include <driver/uart.h>

/**
 * UART allocation info
 */
struct UARTAllocation {
    int uartNum;              // UART number (1 or 2), -1 if not allocated
    int rxPin;                // RX pin
    int txPin;                // TX pin (-1 if not used)
    int baudRate;             // Baud rate
    String owner;             // Owner identifier (e.g., "GPS", "SR04M2")
    HardwareSerial* serial;   // Arduino serial instance
    bool useEspIdf;           // Use ESP-IDF API instead of Arduino
};

/**
 * UART Manager - Singleton for managing ESP32 UART allocations
 */
class UARTManager {
public:
    /**
     * Get singleton instance
     */
    static UARTManager& getInstance();

    /**
     * Allocate a UART for the given pins and owner
     * @param rxPin RX pin (required)
     * @param txPin TX pin (-1 for RX-only mode)
     * @param baudRate Baud rate
     * @param owner Identifier string for debugging
     * @param useEspIdf Use ESP-IDF API (for SR04M-2 etc) instead of Arduino
     * @return UART number (1 or 2) on success, -1 on failure
     */
    int allocate(int rxPin, int txPin, int baudRate, const String& owner, bool useEspIdf = false);

    /**
     * Get HardwareSerial instance for a UART number
     * @param uartNum UART number (1 or 2)
     * @return HardwareSerial pointer or nullptr if not allocated or using ESP-IDF
     */
    HardwareSerial* getSerial(int uartNum);

    /**
     * Get UART number for a given owner
     * @param owner Owner identifier
     * @return UART number (1 or 2), -1 if not found
     */
    int getUartForOwner(const String& owner);

    /**
     * Get UART number for given pins (checks if already allocated)
     * @param rxPin RX pin
     * @param txPin TX pin (-1 to ignore)
     * @return UART number (1 or 2), -1 if not found
     */
    int getUartForPins(int rxPin, int txPin = -1);

    /**
     * Release a UART allocation by UART number
     * @param uartNum UART number (1 or 2)
     */
    void release(int uartNum);

    /**
     * Release a UART allocation by owner
     * @param owner Owner identifier
     */
    void releaseByOwner(const String& owner);

    /**
     * Release a UART allocation by pins
     * @param rxPin RX pin
     * @param txPin TX pin (-1 to match any)
     */
    void releaseByPins(int rxPin, int txPin = -1);

    /**
     * Check if a UART is available
     * @param uartNum UART number (1 or 2)
     * @return true if available
     */
    bool isAvailable(int uartNum);

    /**
     * Get first available UART number
     * @return UART number (1 or 2), -1 if none available
     */
    int getFirstAvailable();

    /**
     * Print current UART allocations for debugging
     */
    void printAllocations();

    /**
     * End all UART allocations (cleanup)
     */
    void releaseAll();

private:
    UARTManager();
    ~UARTManager();

    // Prevent copying
    UARTManager(const UARTManager&) = delete;
    UARTManager& operator=(const UARTManager&) = delete;

    // UART allocations (index 0 = UART1, index 1 = UART2)
    UARTAllocation _allocations[2];

    // Arduino HardwareSerial instances (created on demand)
    HardwareSerial* _serial1;
    HardwareSerial* _serial2;

    /**
     * Initialize a UART using ESP-IDF API
     */
    bool initEspIdf(int uartNum, int rxPin, int txPin, int baudRate);

    /**
     * Initialize a UART using Arduino API
     */
    bool initArduino(int uartNum, int rxPin, int txPin, int baudRate);

    /**
     * End a UART
     */
    void endUart(int uartNum);
};

#endif // PLATFORM_ESP32

#endif // UART_MANAGER_H
