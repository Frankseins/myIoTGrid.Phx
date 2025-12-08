#include "hardware_scanner.h"
#include "uart_manager.h"

#ifdef PLATFORM_ESP32
#include <Wire.h>
#include <OneWire.h>
#include <DallasTemperature.h>
#include <driver/uart.h>  // For UART_PIN_NO_CHANGE

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

    // Use UARTManager to allocate UART dynamically
    UARTManager& uartMgr = UARTManager::getInstance();
    int uartNum = uartMgr.allocate(rxPin, txPin, baudRate, "GPS_SCAN", false);  // Arduino API

    if (uartNum < 0) {
        Serial.println("[UART] Failed to allocate UART for GPS scan!");
        return devices;
    }

    HardwareSerial* gpsSerial = uartMgr.getSerial(uartNum);
    if (!gpsSerial) {
        Serial.println("[UART] Failed to get serial for GPS scan!");
        uartMgr.release(uartNum);
        return devices;
    }

    // Wait a bit for data
    unsigned long startTime = millis();
    String buffer = "";
    bool foundNMEA = false;

    // Listen for up to 2 seconds for NMEA sentences
    while (millis() - startTime < 2000) {
        while (gpsSerial->available()) {
            char c = gpsSerial->read();
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

    // Release UART allocation after scan
    uartMgr.releaseByOwner("GPS_SCAN");

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

std::vector<DetectedDevice> HardwareScanner::scanSR04M2(int rxPin, int txPin, int baudRate) {
    std::vector<DetectedDevice> devices;

    // Handle RX-only mode (TX=-1 means no TX pin)
    bool rxOnlyMode = (txPin < 0);

    // Use configured baud rate or default to 115200
    int actualBaudRate = (baudRate > 0) ? baudRate : 115200;

    Serial.printf("[SR04M-2] Scanning UART for SR04M-2 on RX=%d, TX=%s (%d baud, RX-only=%s)...\n",
                  rxPin, txPin < 0 ? "none" : String(txPin).c_str(), actualBaudRate, rxOnlyMode ? "yes" : "no");

    // Use UARTManager to allocate UART dynamically
    UARTManager& uartMgr = UARTManager::getInstance();
    int uartNum = uartMgr.allocate(rxPin, txPin, actualBaudRate, "SR04M2_SCAN", false);  // Arduino API

    if (uartNum < 0) {
        Serial.println("[SR04M-2] Failed to allocate UART for SR04M-2 scan!");
        return devices;
    }

    HardwareSerial* sr04Serial = uartMgr.getSerial(uartNum);
    if (!sr04Serial) {
        Serial.println("[SR04M-2] Failed to get serial for SR04M-2 scan!");
        uartMgr.release(uartNum);
        return devices;
    }

    delay(100);  // Allow serial to stabilize

    // Clear any pending data
    while (sr04Serial->available()) {
        sr04Serial->read();
    }

    // Only send trigger command if TX is connected
    // SR04M-2 in auto-mode sends data continuously without trigger
    if (!rxOnlyMode) {
        sr04Serial->write(0x55);
        sr04Serial->flush();
    }

    // Wait for response and search for 0xFF 0xFE header (up to 500ms timeout)
    // Frame format: 0xFF 0xFE DIST_HIGH DIST_LOW CHECKSUM
    unsigned long startTime = millis();
    bool foundHeader = false;
    uint8_t header = 0;

    while (millis() - startTime < 500) {
        if (sr04Serial->available() > 0) {
            uint8_t byte = sr04Serial->read();
            // Look for 0xFF header (first byte of frame)
            if (byte == 0xFF) {
                header = byte;
                foundHeader = true;
                break;
            }
        }
        delay(1);
    }

    if (!foundHeader) {
        Serial.printf("[SR04M-2] No valid header found on RX=%d, TX=%s\n", rxPin, txPin < 0 ? "none" : String(txPin).c_str());
        uartMgr.releaseByOwner("SR04M2_SCAN");
        return devices;
    }

    // Wait for remaining 3 bytes (0xFE, DIST_H, DIST_L) + checksum
    // Frame: 0xFF 0xFE DIST_HIGH DIST_LOW CHECKSUM
    startTime = millis();
    while (sr04Serial->available() < 4) {
        if (millis() - startTime > 200) {
            Serial.printf("[SR04M-2] Incomplete data after 0xFF on RX=%d, TX=%s\n", rxPin, txPin < 0 ? "none" : String(txPin).c_str());
            uartMgr.releaseByOwner("SR04M2_SCAN");
            return devices;
        }
        delay(1);
    }

    // Read remaining 4 bytes: 0xFE, DIST_HIGH, DIST_LOW, CHECKSUM
    uint8_t secondHeader = sr04Serial->read();  // Should be 0xFE
    uint8_t highByte = sr04Serial->read();
    uint8_t lowByte = sr04Serial->read();
    uint8_t checksum = sr04Serial->read();

    // Release UART allocation after scan
    uartMgr.releaseByOwner("SR04M2_SCAN");

    // Verify second header byte
    if (secondHeader != 0xFE) {
        Serial.printf("[SR04M-2] Invalid second header: expected 0xFE, got 0x%02X\n", secondHeader);
        return devices;
    }

    // Validate checksum: (DIST_HIGH + DIST_LOW) & 0xFF
    uint8_t calculatedChecksum = (highByte + lowByte) & 0xFF;
    if (calculatedChecksum != checksum) {
        Serial.printf("[SR04M-2] Checksum error: calc=0x%02X, recv=0x%02X\n", calculatedChecksum, checksum);
        return devices;
    }

    // Calculate distance in mm (highByte * 256 + lowByte)
    uint16_t distance_mm = (highByte << 8) | lowByte;
    float distance_cm = distance_mm / 10.0;

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
    Serial.printf("[SR04M-2] Detected on RX=%d, TX=%s (distance: %.1f cm)\n", rxPin, txPin < 0 ? "none" : String(txPin).c_str(), distance_cm);

    return devices;
}

void HardwareScanner::debugGPS(int rxPin, int txPin, int durationSeconds) {
    Serial.println("\n╔════════════════════════════════════════════════════════════╗");
    Serial.println("║               GPS DIAGNOSTICS (NEO-6M)                     ║");
    Serial.println("╠════════════════════════════════════════════════════════════╣");
    Serial.printf("║ RX Pin: %d, TX Pin: %d, Duration: %d seconds               \n", rxPin, txPin, durationSeconds);
    Serial.println("╠════════════════════════════════════════════════════════════╣");
    Serial.println("║ TROUBLESHOOTING TIPS:                                      ║");
    Serial.println("║ 1. Antenna: Ceramic side (beige/brown) facing UP (sky)     ║");
    Serial.println("║ 2. First fix: Can take 2-3 minutes outdoors                ║");
    Serial.println("║ 3. Power: 3.3V may be marginal, try 5V if available        ║");
    Serial.println("║ 4. Location: Clear view of sky, no metal/concrete above    ║");
    Serial.println("║ 5. LED: Some modules have LED that blinks on fix           ║");
    Serial.println("╠════════════════════════════════════════════════════════════╣");
    Serial.println("║ Waiting for NMEA data...                                   ║");
    Serial.println("╚════════════════════════════════════════════════════════════╝\n");

    // Use UARTManager to allocate UART dynamically
    UARTManager& uartMgr = UARTManager::getInstance();
    int uartNum = uartMgr.allocate(rxPin, txPin, 9600, "GPS_DEBUG", false);  // Arduino API

    if (uartNum < 0) {
        Serial.println("[GPS DEBUG] Failed to allocate UART!");
        return;
    }

    HardwareSerial* gpsSerial = uartMgr.getSerial(uartNum);
    if (!gpsSerial) {
        Serial.println("[GPS DEBUG] Failed to get serial!");
        uartMgr.release(uartNum);
        return;
    }
    delay(100);

    unsigned long startTime = millis();
    unsigned long duration = durationSeconds * 1000UL;
    int nmeaCount = 0;
    int gsvCount = 0;      // Satellites in view
    int ggaCount = 0;      // Fix data
    int rmcCount = 0;      // Recommended minimum
    int bytesReceived = 0;
    bool hasValidFix = false;
    int satellitesInView = 0;
    int satellitesUsed = 0;
    String lastGGA = "";
    String lastRMC = "";
    String buffer = "";
    unsigned long lastStatusPrint = 0;

    Serial.println("[GPS DEBUG] Raw NMEA Output:");
    Serial.println("────────────────────────────────────────────────────────────");

    while (millis() - startTime < duration) {
        while (gpsSerial->available()) {
            char c = gpsSerial->read();
            bytesReceived++;
            Serial.print(c);  // Raw output

            buffer += c;

            if (c == '\n') {
                // Parse NMEA sentence
                if (buffer.startsWith("$GPGGA") || buffer.startsWith("$GNGGA")) {
                    ggaCount++;
                    lastGGA = buffer;
                    // Parse fix quality and satellites
                    // $GPGGA,time,lat,N/S,lon,E/W,fix,sats,hdop,alt,M,...
                    int commaCount = 0;
                    for (unsigned int i = 0; i < buffer.length() && commaCount < 8; i++) {
                        if (buffer[i] == ',') {
                            commaCount++;
                            if (commaCount == 6) {
                                // Fix quality (0=invalid, 1=GPS, 2=DGPS)
                                int fixQuality = buffer.substring(i+1, buffer.indexOf(',', i+1)).toInt();
                                hasValidFix = (fixQuality > 0);
                            }
                            if (commaCount == 7) {
                                // Number of satellites
                                satellitesUsed = buffer.substring(i+1, buffer.indexOf(',', i+1)).toInt();
                            }
                        }
                    }
                }
                else if (buffer.startsWith("$GPGSV") || buffer.startsWith("$GNGSV") || buffer.startsWith("$GLGSV")) {
                    gsvCount++;
                    // Parse total satellites in view from first GSV sentence
                    // $GPGSV,totalMsgs,msgNum,totalSats,...
                    int commaCount = 0;
                    for (unsigned int i = 0; i < buffer.length() && commaCount < 4; i++) {
                        if (buffer[i] == ',') {
                            commaCount++;
                            if (commaCount == 3) {
                                int sats = buffer.substring(i+1, buffer.indexOf(',', i+1)).toInt();
                                if (sats > satellitesInView) satellitesInView = sats;
                            }
                        }
                    }
                }
                else if (buffer.startsWith("$GPRMC") || buffer.startsWith("$GNRMC")) {
                    rmcCount++;
                    lastRMC = buffer;
                }

                if (buffer.startsWith("$GP") || buffer.startsWith("$GN") || buffer.startsWith("$GL")) {
                    nmeaCount++;
                }

                buffer = "";
            }

            // Prevent buffer overflow
            if (buffer.length() > 120) {
                buffer = "";
            }
        }

        // Status update every 5 seconds
        unsigned long elapsed = (millis() - startTime) / 1000;
        if (elapsed > 0 && elapsed % 5 == 0 && millis() - lastStatusPrint > 4000) {
            Serial.printf("\n[GPS] Status at %lus: %d bytes, %d NMEA sentences, %d sats in view\n",
                elapsed, bytesReceived, nmeaCount, satellitesInView);
            lastStatusPrint = millis();
        }
    }

    // Release UART allocation
    uartMgr.releaseByOwner("GPS_DEBUG");

    // Final summary
    Serial.println("\n════════════════════════════════════════════════════════════");
    Serial.println("                    GPS DIAGNOSTIC SUMMARY");
    Serial.println("════════════════════════════════════════════════════════════");
    Serial.printf("Total bytes received:     %d\n", bytesReceived);
    Serial.printf("Total NMEA sentences:     %d\n", nmeaCount);
    Serial.printf("  - GGA (Fix data):       %d\n", ggaCount);
    Serial.printf("  - RMC (Position):       %d\n", rmcCount);
    Serial.printf("  - GSV (Satellites):     %d\n", gsvCount);
    Serial.printf("Satellites in view:       %d\n", satellitesInView);
    Serial.printf("Satellites used for fix:  %d\n", satellitesUsed);
    Serial.printf("Valid fix obtained:       %s\n", hasValidFix ? "YES" : "NO");
    Serial.println("════════════════════════════════════════════════════════════");

    // Diagnosis
    Serial.println("\nDIAGNOSIS:");
    if (bytesReceived == 0) {
        Serial.println("[X] NO DATA RECEIVED!");
        Serial.printf("    -> Check wiring: GPS TX -> ESP32 RX (GPIO %d)\n", rxPin);
        Serial.println("    -> Check power: GPS needs stable 3.3V or 5V");
        Serial.println("    -> Check baud rate: NEO-6M default is 9600");
    } else if (nmeaCount == 0) {
        Serial.println("[X] Data received but NO valid NMEA sentences!");
        Serial.println("    -> Wrong baud rate? Try 4800 or 38400");
        Serial.println("    -> GPS module may be damaged");
    } else if (!hasValidFix && satellitesInView == 0) {
        Serial.println("[!] NMEA data OK, but NO satellites in view!");
        Serial.println("    -> Antenna not connected or damaged?");
        Serial.println("    -> Ceramic antenna facing wrong direction?");
        Serial.println("    -> Indoor location - need clear sky view");
    } else if (!hasValidFix && satellitesInView > 0) {
        Serial.printf("[!] Satellites visible (%d) but NO FIX yet!\n", satellitesInView);
        Serial.println("    -> Cold start: Wait 2-3 minutes for first fix");
        Serial.println("    -> Need 4+ satellites for 3D fix");
        Serial.println("    -> Move to location with better sky view");
    } else if (hasValidFix) {
        Serial.println("[OK] GPS is working! Fix obtained.");
        Serial.printf("     -> Using %d satellites\n", satellitesUsed);
        if (lastGGA.length() > 0) {
            Serial.printf("     -> Last GGA: %s", lastGGA.c_str());
        }
    }

    Serial.println("════════════════════════════════════════════════════════════\n");
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
            // Default SR04M-2 pins - RX only mode (auto-send sensor)
            int rxPin = 4;   // Default RX (sensor TX -> ESP32 RX)
            int txPin = -1;  // Not used for auto-send mode
            int baudRate = config.baudRate;  // Use configured baud rate from database

            // Use configured pins if available (analogPin for RX, digitalPin for TX)
            if (config.analogPin > 0) rxPin = config.analogPin;
            if (config.digitalPin > 0) txPin = config.digitalPin;

            // Try to detect SR04M-2 (RX-only mode for auto-send sensors)
            auto sr04m2Devices = scanSR04M2(rxPin, txPin, baudRate);
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

void HardwareScanner::debugGPS(int rxPin, int txPin, int durationSeconds) {
    Serial.println("[GPS DEBUG] Not available on native platform");
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
