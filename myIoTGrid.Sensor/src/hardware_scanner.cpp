#include "hardware_scanner.h"

#ifdef PLATFORM_ESP32
#include <Wire.h>
#include <OneWire.h>
#include <DallasTemperature.h>

// Known I2C devices database
const I2CDevice HardwareScanner::KNOWN_I2C_DEVICES[] = {
    // Temperature/Humidity sensors
    {0x76, "BME280/BMP280", "temperature"},
    {0x77, "BME280/BMP280 (alt)", "temperature"},
    {0x40, "HDC1080/SHT40", "humidity"},
    {0x44, "SHT31/SHT35", "humidity"},
    {0x45, "SHT31/SHT35 (alt)", "humidity"},

    // Light sensors
    {0x23, "BH1750", "light"},
    {0x5C, "BH1750 (alt)", "light"},
    {0x29, "TSL2561/TSL2591", "light"},
    {0x39, "TSL2561 (alt)", "light"},
    {0x49, "TSL2561 (alt2)", "light"},

    // CO2 sensors
    {0x61, "SCD30", "co2"},
    {0x62, "SCD40/SCD41", "co2"},

    // Air quality
    {0x5A, "CCS811", "co2"},
    {0x5B, "CCS811 (alt)", "co2"},
    {0x58, "SGP30", "co2"},
    {0x59, "SGP30 (alt)", "co2"},

    // Pressure sensors
    {0x60, "MPL3115A2", "pressure"},

    // UV sensors
    {0x38, "VEML6070", "uv"},
    {0x10, "VEML6075", "uv"},

    // Distance sensors
    {0x52, "VL53L0X", "distance"},
    {0x29, "VL53L1X", "distance"},

    // ADC/DAC
    {0x48, "ADS1115/ADS1015", "analog"},
    {0x49, "ADS1115 (alt)", "analog"},
    {0x4A, "ADS1115 (alt2)", "analog"},
    {0x4B, "ADS1115 (alt3)", "analog"},

    // OLED displays (not sensors but useful to detect)
    {0x3C, "SSD1306 OLED", "display"},
    {0x3D, "SSD1306 OLED (alt)", "display"},

    // Real-time clocks
    {0x68, "DS3231/DS1307 RTC", "rtc"},
    {0x57, "DS3231 EEPROM", "rtc"},

    // EEPROM
    {0x50, "AT24C32 EEPROM", "memory"},
    {0x51, "AT24C32 EEPROM", "memory"},

    // Soil moisture (I2C versions)
    {0x20, "Capacitive Soil Sensor", "soil_moisture"},
};

const int HardwareScanner::KNOWN_I2C_DEVICE_COUNT = sizeof(KNOWN_I2C_DEVICES) / sizeof(I2CDevice);

HardwareScanner::HardwareScanner() : _sdaPin(21), _sclPin(22) {
}

void HardwareScanner::begin(int sdaPin, int sclPin) {
    _sdaPin = sdaPin;
    _sclPin = sclPin;
    Wire.begin(_sdaPin, _sclPin);
    Serial.println("[HardwareScanner] Initialized I2C on SDA=" + String(_sdaPin) + ", SCL=" + String(_sclPin));
}

std::vector<DetectedDevice> HardwareScanner::scanAll() {
    _lastResults.clear();

    Serial.println("\n========================================");
    Serial.println("       HARDWARE SCAN STARTING");
    Serial.println("========================================\n");

    // Scan I2C bus
    auto i2cDevices = scanI2C();
    _lastResults.insert(_lastResults.end(), i2cDevices.begin(), i2cDevices.end());

    // Scan common 1-Wire pins
    int oneWirePins[] = {4, 5, 13, 14, 15, 16, 17, 18, 19, 23, 25, 26, 27, 32, 33};
    for (int pin : oneWirePins) {
        auto owDevices = scanOneWire(pin);
        _lastResults.insert(_lastResults.end(), owDevices.begin(), owDevices.end());
    }

    // Scan analog pins
    auto analogDevices = scanAnalogPins();
    _lastResults.insert(_lastResults.end(), analogDevices.begin(), analogDevices.end());

    Serial.println("\n========================================");
    Serial.printf("       SCAN COMPLETE: %d devices found\n", _lastResults.size());
    Serial.println("========================================\n");

    return _lastResults;
}

std::vector<DetectedDevice> HardwareScanner::scanI2C() {
    std::vector<DetectedDevice> devices;

    Serial.println("[I2C] Scanning I2C bus...");
    Serial.println("----------------------------------------");

    int foundCount = 0;

    for (uint8_t address = 1; address < 127; address++) {
        Wire.beginTransmission(address);
        byte error = Wire.endTransmission();

        if (error == 0) {
            foundCount++;
            I2CDevice known = identifyI2CDevice(address);

            DetectedDevice device;
            device.bus = "I2C";
            device.address = address;
            device.deviceName = known.name;
            device.sensorType = known.sensorType;
            device.pin = -1;
            device.value = 0;

            devices.push_back(device);

            Serial.printf("[I2C] 0x%02X - %s (%s)\n",
                address,
                known.name.c_str(),
                known.sensorType.c_str());
        }
    }

    if (foundCount == 0) {
        Serial.println("[I2C] No devices found");
    } else {
        Serial.printf("[I2C] Found %d device(s)\n", foundCount);
    }

    return devices;
}

std::vector<DetectedDevice> HardwareScanner::scanOneWire(int pin) {
    std::vector<DetectedDevice> devices;

    OneWire oneWire(pin);
    DallasTemperature sensors(&oneWire);

    sensors.begin();
    int deviceCount = sensors.getDeviceCount();

    if (deviceCount > 0) {
        Serial.printf("[1-Wire] Pin %d: Found %d device(s)\n", pin, deviceCount);

        for (int i = 0; i < deviceCount; i++) {
            DeviceAddress addr;
            if (sensors.getAddress(addr, i)) {
                // Try to read temperature
                sensors.requestTemperatures();
                float temp = sensors.getTempCByIndex(i);

                DetectedDevice device;
                device.bus = "1-Wire";
                device.address = addr[0];
                device.pin = pin;
                device.value = temp;

                // Identify device type by family code
                switch (addr[0]) {
                    case 0x10:
                        device.deviceName = "DS18S20";
                        device.sensorType = "temperature";
                        break;
                    case 0x22:
                        device.deviceName = "DS1822";
                        device.sensorType = "temperature";
                        break;
                    case 0x28:
                        device.deviceName = "DS18B20";
                        device.sensorType = "temperature";
                        break;
                    case 0x3B:
                        device.deviceName = "DS1825";
                        device.sensorType = "temperature";
                        break;
                    default:
                        device.deviceName = "Unknown 1-Wire";
                        device.sensorType = "unknown";
                }

                devices.push_back(device);

                Serial.printf("[1-Wire] Pin %d: %s (%.2f°C)\n",
                    pin, device.deviceName.c_str(), temp);
            }
        }
    }

    return devices;
}

std::vector<DetectedDevice> HardwareScanner::scanAnalogPins() {
    std::vector<DetectedDevice> devices;

    Serial.println("[Analog] Scanning analog pins...");
    Serial.println("----------------------------------------");

    // ESP32 ADC1 pins (GPIO 32-39)
    int analogPins[] = {32, 33, 34, 35, 36, 39};

    for (int pin : analogPins) {
        int rawValue = analogRead(pin);
        float voltage = (rawValue / 4095.0) * 3.3;

        // Detect if something is connected based on voltage level
        // Empty pins usually read near 0 or floating around random values
        // Connected sensors typically show more stable readings

        // Take multiple readings to check stability
        int readings[5];
        for (int i = 0; i < 5; i++) {
            readings[i] = analogRead(pin);
            delay(10);
        }

        // Calculate variance
        float sum = 0;
        for (int i = 0; i < 5; i++) {
            sum += readings[i];
        }
        float avg = sum / 5.0;

        float variance = 0;
        for (int i = 0; i < 5; i++) {
            variance += (readings[i] - avg) * (readings[i] - avg);
        }
        variance /= 5.0;

        // If voltage is in a meaningful range and stable, likely a sensor is connected
        bool likelyConnected = (voltage > 0.1 && voltage < 3.2 && variance < 1000);

        if (likelyConnected) {
            DetectedDevice device;
            device.bus = "Analog";
            device.address = 0;
            device.pin = pin;
            device.value = voltage;

            // Try to identify based on voltage level
            if (voltage > 0.1 && voltage < 1.5) {
                device.deviceName = "Soil Moisture Sensor (wet)";
                device.sensorType = "soil_moisture";
            } else if (voltage >= 1.5 && voltage < 2.5) {
                device.deviceName = "Soil Moisture Sensor (moist)";
                device.sensorType = "soil_moisture";
            } else if (voltage >= 2.5 && voltage < 3.2) {
                device.deviceName = "Soil Moisture Sensor (dry)";
                device.sensorType = "soil_moisture";
            } else {
                device.deviceName = "Analog Sensor";
                device.sensorType = "analog";
            }

            devices.push_back(device);

            Serial.printf("[Analog] Pin %d: %.2fV (Raw: %d) - %s\n",
                pin, voltage, rawValue, device.deviceName.c_str());
        } else {
            Serial.printf("[Analog] Pin %d: %.2fV (Raw: %d) - No sensor detected\n",
                pin, voltage, rawValue);
        }
    }

    return devices;
}

I2CDevice HardwareScanner::identifyI2CDevice(uint8_t address) {
    for (int i = 0; i < KNOWN_I2C_DEVICE_COUNT; i++) {
        if (KNOWN_I2C_DEVICES[i].address == address) {
            return KNOWN_I2C_DEVICES[i];
        }
    }

    // Unknown device
    I2CDevice unknown;
    unknown.address = address;
    unknown.name = "Unknown Device";
    unknown.sensorType = "unknown";
    return unknown;
}

void HardwareScanner::printResults(const std::vector<DetectedDevice>& devices) {
    Serial.println("\n╔════════════════════════════════════════╗");
    Serial.println("║       DETECTED HARDWARE SUMMARY        ║");
    Serial.println("╠════════════════════════════════════════╣");

    if (devices.empty()) {
        Serial.println("║  No devices detected                   ║");
    } else {
        for (const auto& device : devices) {
            String line = "║ ";
            line += device.bus;
            line += " ";

            if (device.bus == "I2C") {
                char addrStr[8];
                sprintf(addrStr, "0x%02X", device.address);
                line += addrStr;
            } else if (device.bus == "UART") {
                line += "RX:" + String(device.rxPin) + "/TX:" + String(device.txPin);
            } else {
                line += "Pin ";
                line += String(device.pin);
            }

            line += ": ";
            line += device.deviceName;

            // Pad to fixed width
            while (line.length() < 41) {
                line += " ";
            }
            line += "║";

            Serial.println(line);
        }
    }

    Serial.println("╚════════════════════════════════════════╝\n");
}

std::vector<DetectedDevice> HardwareScanner::scanUART(int rxPin, int txPin, int baudRate) {
    std::vector<DetectedDevice> devices;

    Serial.printf("[UART] Scanning RX=%d, TX=%d at %d baud...\n", rxPin, txPin, baudRate);

    // Create a HardwareSerial instance for GPS
    HardwareSerial gpsSerial(1);  // Use UART1
    gpsSerial.begin(baudRate, SERIAL_8N1, rxPin, txPin);

    // Wait a bit for data
    unsigned long startTime = millis();
    String buffer = "";
    bool foundNMEA = false;

    // Listen for up to 2 seconds for NMEA sentences
    while (millis() - startTime < 2000) {
        while (gpsSerial.available()) {
            char c = gpsSerial.read();
            buffer += c;

            // Check for NMEA sentence start
            if (c == '\n' && buffer.length() > 0) {
                // Check for valid NMEA sentences
                if (buffer.startsWith("$GP") || buffer.startsWith("$GN") || buffer.startsWith("$GL")) {
                    foundNMEA = true;
                    Serial.printf("[UART] Found NMEA: %s", buffer.c_str());
                }
                buffer = "";
            }

            // Prevent buffer overflow
            if (buffer.length() > 100) {
                buffer = "";
            }
        }
        delay(10);
    }

    gpsSerial.end();

    if (foundNMEA) {
        DetectedDevice device;
        device.bus = "UART";
        device.address = 0;
        device.deviceName = "NEO-6M GPS Module";
        device.sensorType = "neo-6m";
        device.pin = -1;
        device.rxPin = rxPin;
        device.txPin = txPin;
        device.value = 0;

        devices.push_back(device);
        Serial.printf("[UART] Detected GPS module (NMEA protocol) on RX=%d, TX=%d\n", rxPin, txPin);
    } else {
        Serial.printf("[UART] No GPS module detected on RX=%d, TX=%d\n", rxPin, txPin);
    }

    return devices;
}

std::vector<DetectedDevice> HardwareScanner::scanSR04M2(int rxPin, int txPin) {
    std::vector<DetectedDevice> devices;

    Serial.printf("[SR04M-2] Scanning UART for SR04M-2 on RX=%d, TX=%d...\n", rxPin, txPin);

    // Use Serial1 for SR04M-2
    HardwareSerial sr04Serial(1);
    sr04Serial.begin(9600, SERIAL_8N1, rxPin, txPin);
    delay(100);  // Allow serial to stabilize

    // Clear any pending data
    while (sr04Serial.available()) {
        sr04Serial.read();
    }

    // Send trigger command (0x55)
    sr04Serial.write(0x55);
    sr04Serial.flush();

    // Wait for response and search for 0xFF header (up to 500ms timeout)
    // The sensor might be in auto-mode or we might read mid-packet
    unsigned long startTime = millis();
    bool foundHeader = false;
    uint8_t header = 0;

    while (millis() - startTime < 500) {
        if (sr04Serial.available() > 0) {
            uint8_t byte = sr04Serial.read();
            if (byte == 0xFF) {
                header = byte;
                foundHeader = true;
                break;
            }
        }
        delay(1);
    }

    if (!foundHeader) {
        Serial.printf("[SR04M-2] No valid header found on RX=%d, TX=%d\n", rxPin, txPin);
        sr04Serial.end();
        return devices;
    }

    // Wait for remaining 3 bytes (HIGH, LOW, CHECKSUM)
    startTime = millis();
    while (sr04Serial.available() < 3) {
        if (millis() - startTime > 100) {
            Serial.printf("[SR04M-2] Incomplete data after header on RX=%d, TX=%d\n", rxPin, txPin);
            sr04Serial.end();
            return devices;
        }
        delay(1);
    }

    // Read remaining 3 bytes
    uint8_t highByte = sr04Serial.read();
    uint8_t lowByte = sr04Serial.read();
    uint8_t checksum = sr04Serial.read();

    sr04Serial.end();

    // Validate checksum
    uint8_t calculatedChecksum = (header + highByte + lowByte) & 0xFF;
    if (calculatedChecksum != checksum) {
        Serial.printf("[SR04M-2] Checksum error: calc=0x%02X, recv=0x%02X\n", calculatedChecksum, checksum);
        return devices;
    }

    // Calculate distance in cm
    float distance_cm = (highByte * 256 + lowByte) / 10.0;

    // Valid response - SR04M-2 detected
    DetectedDevice device;
    device.bus = "UART";
    device.address = 0;
    device.deviceName = "SR04M-2 Waterproof Ultrasonic";
    device.sensorType = "sr04m-2";
    device.pin = -1;
    device.rxPin = rxPin;
    device.txPin = txPin;
    device.value = distance_cm;

    devices.push_back(device);
    Serial.printf("[SR04M-2] Detected on RX=%d, TX=%d (distance: %.1f cm)\n", rxPin, txPin, distance_cm);

    return devices;
}

ValidationSummary HardwareScanner::validateConfiguration(const std::vector<SensorAssignmentConfig>& configs) {
    ValidationSummary summary;
    summary.totalConfigured = 0;
    summary.foundCount = 0;
    summary.missingCount = 0;

    Serial.println("\n========================================");
    Serial.println("    HARDWARE VALIDATION STARTING");
    Serial.println("========================================\n");

    // First, do a fresh scan of all hardware
    scanAll();

    // Also scan UART for GPS and SR04M-2 if any such sensors are configured
    for (const auto& config : configs) {
        if (!config.isActive) continue;

        String sensorLower = config.sensorCode;
        sensorLower.toLowerCase();

        // Check for GPS sensors that need UART scan
        if (sensorLower.indexOf("neo") >= 0 || sensorLower.indexOf("gps") >= 0) {
            // Default GPS pins on ESP32
            int rxPin = 16;  // Default RX
            int txPin = 17;  // Default TX

            // Use configured pins if available (stored in analogPin for RX, digitalPin for TX)
            if (config.analogPin > 0) rxPin = config.analogPin;
            if (config.digitalPin > 0) txPin = config.digitalPin;

            auto uartDevices = scanUART(rxPin, txPin);
            _lastResults.insert(_lastResults.end(), uartDevices.begin(), uartDevices.end());
        }

        // Check for SR04M-2 sensors that need UART scan
        if (sensorLower.indexOf("sr04m") >= 0) {
            // Default SR04M-2 pins
            int rxPin = 19;  // Default RX
            int txPin = 18;  // Default TX

            // Use configured pins if available (analogPin for RX, digitalPin for TX)
            if (config.analogPin > 0) rxPin = config.analogPin;
            if (config.digitalPin > 0) txPin = config.digitalPin;

            // Try to detect SR04M-2 by sending a ping
            auto sr04m2Devices = scanSR04M2(rxPin, txPin);
            _lastResults.insert(_lastResults.end(), sr04m2Devices.begin(), sr04m2Devices.end());
        }
    }

    // Now validate each configured sensor
    for (const auto& config : configs) {
        if (!config.isActive) continue;

        summary.totalConfigured++;

        ValidationResult result;
        result.sensorCode = config.sensorCode;
        result.sensorName = config.sensorName;
        result.endpointId = config.endpointId;
        result.hardwareFound = false;
        result.detectedAs = "";
        result.message = "";

        // Try to find matching hardware
        for (const auto& device : _lastResults) {
            if (sensorMatchesDevice(config.sensorCode, device)) {
                // Additional check for I2C address match
                if (device.bus == "I2C" && config.i2cAddress.length() > 0) {
                    uint8_t configAddr = parseI2CAddress(config.i2cAddress);
                    if (configAddr != device.address) {
                        continue;  // Address doesn't match
                    }
                }

                // Additional check for 1-Wire pin match
                if (device.bus == "1-Wire" && config.oneWirePin > 0) {
                    if (config.oneWirePin != device.pin) {
                        continue;  // Pin doesn't match
                    }
                }

                // Additional check for Analog pin match
                if (device.bus == "Analog" && config.analogPin > 0) {
                    if (config.analogPin != device.pin) {
                        continue;  // Pin doesn't match
                    }
                }

                result.hardwareFound = true;
                result.detectedAs = device.deviceName + " (" + device.bus + ")";
                result.message = "Hardware found and matches configuration";
                break;
            }
        }

        if (!result.hardwareFound) {
            result.message = "Hardware NOT found! Check connections and configuration.";
            summary.missingCount++;
        } else {
            summary.foundCount++;
        }

        summary.results.push_back(result);
    }

    // Print summary
    printValidationResults(summary);

    return summary;
}

void HardwareScanner::printValidationResults(const ValidationSummary& summary) {
    Serial.println("\n╔════════════════════════════════════════════════════════════╗");
    Serial.println("║           HARDWARE VALIDATION RESULTS                      ║");
    Serial.println("╠════════════════════════════════════════════════════════════╣");

    for (const auto& result : summary.results) {
        String statusIcon = result.hardwareFound ? "✓" : "✗";
        String statusText = result.hardwareFound ? "FOUND" : "MISSING";

        Serial.printf("║ [%s] %s (Endpoint %d)\n",
            statusIcon.c_str(),
            result.sensorName.c_str(),
            result.endpointId);

        Serial.printf("║     Sensor: %s\n", result.sensorCode.c_str());

        if (result.hardwareFound) {
            Serial.printf("║     Detected as: %s\n", result.detectedAs.c_str());
        } else {
            Serial.printf("║     Status: %s\n", result.message.c_str());
        }

        Serial.println("║────────────────────────────────────────────────────────────");
    }

    Serial.println("╠════════════════════════════════════════════════════════════╣");
    Serial.printf("║ Total: %d configured, %d found, %d missing               \n",
        summary.totalConfigured, summary.foundCount, summary.missingCount);

    if (summary.allFound()) {
        Serial.println("║ Status: ALL HARDWARE VALIDATED ✓                           ║");
    } else {
        Serial.println("║ Status: HARDWARE MISMATCH - Check connections! ✗           ║");
    }

    Serial.println("╚════════════════════════════════════════════════════════════╝\n");
}

bool HardwareScanner::sensorMatchesDevice(const String& sensorCode, const DetectedDevice& device) {
    String sensorLower = sensorCode;
    sensorLower.toLowerCase();

    String deviceTypeLower = device.sensorType;
    deviceTypeLower.toLowerCase();

    String deviceNameLower = device.deviceName;
    deviceNameLower.toLowerCase();

    // Direct sensor type match
    if (sensorLower == deviceTypeLower) {
        return true;
    }

    // Map common sensor codes to device types/names
    // Temperature sensors
    if (sensorLower == "bme280" || sensorLower == "bmp280") {
        return deviceNameLower.indexOf("bme280") >= 0 || deviceNameLower.indexOf("bmp280") >= 0;
    }

    // DS18B20 temperature sensor
    if (sensorLower == "ds18b20") {
        return deviceNameLower.indexOf("ds18b20") >= 0 || deviceNameLower.indexOf("ds18s20") >= 0;
    }

    // Humidity sensors
    if (sensorLower == "sht31" || sensorLower == "sht35") {
        return deviceNameLower.indexOf("sht31") >= 0 || deviceNameLower.indexOf("sht35") >= 0;
    }

    if (sensorLower == "hdc1080") {
        return deviceNameLower.indexOf("hdc1080") >= 0;
    }

    // Light sensors - BH1750 / GY-302 (same sensor, different breakout board names)
    if (sensorLower == "bh1750" || sensorLower == "gy302" || sensorLower == "gy-302") {
        return deviceNameLower.indexOf("bh1750") >= 0;
    }

    if (sensorLower == "tsl2561" || sensorLower == "tsl2591") {
        return deviceNameLower.indexOf("tsl2561") >= 0 || deviceNameLower.indexOf("tsl2591") >= 0;
    }

    // CO2 sensors
    if (sensorLower == "scd30") {
        return deviceNameLower.indexOf("scd30") >= 0;
    }

    if (sensorLower == "scd40" || sensorLower == "scd41") {
        return deviceNameLower.indexOf("scd40") >= 0 || deviceNameLower.indexOf("scd41") >= 0;
    }

    if (sensorLower == "ccs811") {
        return deviceNameLower.indexOf("ccs811") >= 0;
    }

    if (sensorLower == "mh-z19" || sensorLower == "mhz19") {
        return deviceNameLower.indexOf("mh-z19") >= 0 || deviceNameLower.indexOf("mhz19") >= 0;
    }

    // GPS sensors
    if (sensorLower == "neo-6m" || sensorLower == "neo6m" || sensorLower.indexOf("gps") >= 0) {
        return deviceNameLower.indexOf("neo") >= 0 || deviceNameLower.indexOf("gps") >= 0;
    }

    // SR04M-2 Waterproof Ultrasonic (UART Mode)
    if (sensorLower == "sr04m-2" || sensorLower == "sr04m2") {
        return deviceNameLower.indexOf("sr04m") >= 0 || deviceTypeLower == "sr04m-2";
    }

    // Soil moisture
    if (sensorLower.indexOf("soil") >= 0 || sensorLower.indexOf("moisture") >= 0) {
        return deviceTypeLower == "soil_moisture" || deviceNameLower.indexOf("soil") >= 0;
    }

    // Generic analog sensor
    if (sensorLower.indexOf("analog") >= 0) {
        return device.bus == "Analog";
    }

    // Fallback: check if sensor code is contained in device name or type
    return deviceNameLower.indexOf(sensorLower) >= 0 || deviceTypeLower.indexOf(sensorLower) >= 0;
}

uint8_t HardwareScanner::parseI2CAddress(const String& addressStr) {
    if (addressStr.length() == 0) return 0;

    String addrLower = addressStr;
    addrLower.toLowerCase();

    // Handle "0x" prefix
    if (addrLower.startsWith("0x")) {
        return (uint8_t)strtol(addrLower.c_str() + 2, nullptr, 16);
    }

    // Try decimal
    return (uint8_t)addressStr.toInt();
}

#else
// Native platform stubs - hardware scanning not available

const I2CDevice HardwareScanner::KNOWN_I2C_DEVICES[] = {};
const int HardwareScanner::KNOWN_I2C_DEVICE_COUNT = 0;

HardwareScanner::HardwareScanner() : _sdaPin(21), _sclPin(22) {}

void HardwareScanner::begin(int sdaPin, int sclPin) {
    _sdaPin = sdaPin;
    _sclPin = sclPin;
}

std::vector<DetectedDevice> HardwareScanner::scanAll() {
    return std::vector<DetectedDevice>();
}

std::vector<DetectedDevice> HardwareScanner::scanI2C() {
    return std::vector<DetectedDevice>();
}

std::vector<DetectedDevice> HardwareScanner::scanOneWire(int pin) {
    return std::vector<DetectedDevice>();
}

std::vector<DetectedDevice> HardwareScanner::scanAnalogPins() {
    return std::vector<DetectedDevice>();
}

std::vector<DetectedDevice> HardwareScanner::scanUART(int rxPin, int txPin, int baudRate) {
    return std::vector<DetectedDevice>();
}

I2CDevice HardwareScanner::identifyI2CDevice(uint8_t address) {
    I2CDevice unknown;
    unknown.address = address;
    unknown.name = "Unknown";
    unknown.sensorType = "unknown";
    return unknown;
}

void HardwareScanner::printResults(const std::vector<DetectedDevice>& devices) {
    // No-op on native platform
}

ValidationSummary HardwareScanner::validateConfiguration(const std::vector<SensorAssignmentConfig>& configs) {
    // On native platform, always report all configured sensors as found (simulation mode)
    ValidationSummary summary;
    summary.totalConfigured = 0;
    summary.foundCount = 0;
    summary.missingCount = 0;

    for (const auto& config : configs) {
        if (!config.isActive) continue;

        summary.totalConfigured++;
        summary.foundCount++;

        ValidationResult result;
        result.sensorCode = config.sensorCode;
        result.sensorName = config.sensorName;
        result.endpointId = config.endpointId;
        result.hardwareFound = true;
        result.detectedAs = "Simulated";
        result.message = "Simulation mode - hardware validation skipped";

        summary.results.push_back(result);
    }

    return summary;
}

void HardwareScanner::printValidationResults(const ValidationSummary& summary) {
    // No-op on native platform
}

bool HardwareScanner::sensorMatchesDevice(const String& sensorCode, const DetectedDevice& device) {
    return false;
}

uint8_t HardwareScanner::parseI2CAddress(const String& addressStr) {
    return 0;
}

#endif // PLATFORM_ESP32
