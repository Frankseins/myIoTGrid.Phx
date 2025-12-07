#ifndef HARDWARE_SCANNER_H
#define HARDWARE_SCANNER_H

#include <Arduino.h>
#include <vector>
#include "config.h"
#include "api_client.h"  // For SensorAssignmentConfig

// Known I2C device addresses and their names
struct I2CDevice {
    uint8_t address;
    String name;
    String sensorType;  // Matches backend sensor types
};

// Known UART/GPS devices
struct UARTDevice {
    String protocol;      // "NMEA", "UBX", etc.
    String name;
    String sensorType;
};

// Detected device info
struct DetectedDevice {
    String bus;           // "I2C", "1-Wire", "Analog", "Digital", "UART"
    uint8_t address;      // I2C address or pin number
    String deviceName;    // Human-readable name
    String sensorType;    // Backend sensor type code
    int pin;              // Pin number (for 1-Wire, Analog, Digital)
    int rxPin;            // RX pin (for UART)
    int txPin;            // TX pin (for UART)
    float value;          // Current reading (if applicable)
};

// Validation result for a configured sensor
struct ValidationResult {
    String sensorCode;
    String sensorName;
    int endpointId;
    bool hardwareFound;
    String detectedAs;
    String message;
};

// Overall validation summary
struct ValidationSummary {
    int totalConfigured;
    int foundCount;
    int missingCount;
    std::vector<ValidationResult> results;
    bool allFound() const { return missingCount == 0; }
};

class HardwareScanner {
public:
    HardwareScanner();

    // Initialize scanner with I2C pins
    void begin(int sdaPin = 21, int sclPin = 22);

    // Scan all buses and return detected devices
    std::vector<DetectedDevice> scanAll();

    // Individual bus scans
    std::vector<DetectedDevice> scanI2C();
    std::vector<DetectedDevice> scanOneWire(int pin);
    std::vector<DetectedDevice> scanAnalogPins();
    std::vector<DetectedDevice> scanUART(int rxPin, int txPin, int baudRate = 9600);
    std::vector<DetectedDevice> scanSR04M2(int rxPin, int txPin);

    // Validate configured sensors against detected hardware
    ValidationSummary validateConfiguration(const std::vector<SensorAssignmentConfig>& configs);

    // Print scan results to Serial
    void printResults(const std::vector<DetectedDevice>& devices);

    // Print validation results to Serial
    void printValidationResults(const ValidationSummary& summary);

    // Get last scan results
    const std::vector<DetectedDevice>& getLastScanResults() const { return _lastResults; }

private:
    int _sdaPin;
    int _sclPin;
    std::vector<DetectedDevice> _lastResults;

    // I2C device database
    static const I2CDevice KNOWN_I2C_DEVICES[];
    static const int KNOWN_I2C_DEVICE_COUNT;

    // Helper to identify I2C device
    I2CDevice identifyI2CDevice(uint8_t address);

    // Helper to check if a sensor code matches a detected device
    bool sensorMatchesDevice(const String& sensorCode, const DetectedDevice& device);

    // Helper to parse I2C address string (e.g., "0x76" -> 118)
    uint8_t parseI2CAddress(const String& addressStr);
};

#endif // HARDWARE_SCANNER_H
