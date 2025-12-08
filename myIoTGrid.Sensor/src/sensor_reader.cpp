/**
 * myIoTGrid.Sensor - Hardware Sensor Reader Implementation
 *
 * Reads real sensor values from various sensors based on Hub configuration.
 */

#include "sensor_reader.h"
#include "hardware_scanner.h"
#include "uart_manager.h"

// Default I2C pins for ESP32
#define DEFAULT_SDA_PIN 21
#define DEFAULT_SCL_PIN 22

SensorReader::SensorReader()
    : _initialized(false)
#ifdef PLATFORM_ESP32
    , _bme280_0x76(nullptr), _bme280_0x77(nullptr)
    , _bme280_0x76_ready(false), _bme280_0x77_ready(false)
    , _bme680_0x76(nullptr), _bme680_0x77(nullptr)
    , _bme680_0x76_ready(false), _bme680_0x77_ready(false)
    , _sht31_0x44(nullptr), _sht31_0x45(nullptr)
    , _sht31_0x44_ready(false), _sht31_0x45_ready(false)
    , _oneWire(nullptr), _ds18b20(nullptr)
    , _ds18b20_ready(false), _ds18b20_pin(-1)
    , _bh1750_0x23(nullptr), _bh1750_0x5C(nullptr)
    , _bh1750_0x23_ready(false), _bh1750_0x5C_ready(false)
    , _tsl2561_0x29(nullptr), _tsl2561_0x39(nullptr), _tsl2561_0x49(nullptr)
    , _tsl2561_0x29_ready(false), _tsl2561_0x39_ready(false), _tsl2561_0x49_ready(false)
    , _scd30(nullptr), _scd30_ready(false)
    , _scd4x(nullptr), _scd4x_ready(false)
    , _ccs811_0x5A(nullptr), _ccs811_0x5B(nullptr)
    , _ccs811_0x5A_ready(false), _ccs811_0x5B_ready(false)
    , _sgp30(nullptr), _sgp30_ready(false)
    , _vl53l0x(nullptr), _vl53l0x_ready(false)
    , _ads1115_0x48(nullptr), _ads1115_0x49(nullptr)
    , _ads1115_0x48_ready(false), _ads1115_0x49_ready(false)
    , _dht22(nullptr), _dht22_ready(false), _dht22_pin(-1)
    , _ultrasonic_trigger_pin(-1), _ultrasonic_echo_pin(-1), _ultrasonic_ready(false)
    , _gps(nullptr), _gpsSerial(nullptr), _gps_ready(false), _gps_rx_pin(-1), _gps_tx_pin(-1), _gps_debug_ran(false)
    , _sr04m2Serial(nullptr), _sr04m2_ready(false), _sr04m2_rx_pin(-1), _sr04m2_tx_pin(-1)
    , _currentSdaPin(-1), _currentSclPin(-1)
#endif
{
}

SensorReader::~SensorReader() {
#ifdef PLATFORM_ESP32
    delete _bme280_0x76; delete _bme280_0x77;
    delete _bme680_0x76; delete _bme680_0x77;
    delete _sht31_0x44; delete _sht31_0x45;
    delete _ds18b20; delete _oneWire;
    delete _bh1750_0x23; delete _bh1750_0x5C;
    delete _tsl2561_0x29; delete _tsl2561_0x39; delete _tsl2561_0x49;
    delete _scd30; delete _scd4x;
    delete _ccs811_0x5A; delete _ccs811_0x5B;
    delete _sgp30; delete _vl53l0x;
    delete _ads1115_0x48; delete _ads1115_0x49;
    delete _dht22;
    delete _gps;
    delete _sr04m2Serial;
#endif
}

void SensorReader::init() {
    if (_initialized) return;
    Serial.println("[SensorReader] Initializing...");
#ifdef PLATFORM_ESP32
    initI2C(DEFAULT_SDA_PIN, DEFAULT_SCL_PIN);
#endif
    _initialized = true;
    Serial.println("[SensorReader] Initialized");
}

#ifdef PLATFORM_ESP32

void SensorReader::initI2C(int sdaPin, int sclPin) {
    if (sdaPin < 0) sdaPin = DEFAULT_SDA_PIN;
    if (sclPin < 0) sclPin = DEFAULT_SCL_PIN;
    if (_currentSdaPin == sdaPin && _currentSclPin == sclPin) return;

    Serial.printf("[SensorReader] Initializing I2C on SDA=%d, SCL=%d\n", sdaPin, sclPin);
    Wire.end();
    Wire.begin(sdaPin, sclPin);
    _currentSdaPin = sdaPin;
    _currentSclPin = sclPin;
}

uint8_t SensorReader::parseI2CAddress(const String& addressStr) {
    if (addressStr.length() == 0) return 0;
    String addr = addressStr;
    addr.trim();
    addr.toLowerCase();
    if (addr.startsWith("0x")) addr = addr.substring(2);
    return (uint8_t)strtol(addr.c_str(), nullptr, 16);
}

// ============================================================================
// BME280 Implementation
// ============================================================================

bool SensorReader::initBME280(uint8_t address) {
    Serial.printf("[SensorReader] Initializing BME280 at 0x%02X...\n", address);
    Adafruit_BME280** bmePtr = (address == 0x76) ? &_bme280_0x76 : &_bme280_0x77;
    bool* readyPtr = (address == 0x76) ? &_bme280_0x76_ready : &_bme280_0x77_ready;

    if (address != 0x76 && address != 0x77) {
        Serial.printf("[SensorReader] Invalid BME280 address: 0x%02X\n", address);
        return false;
    }
    if (*readyPtr) return true;
    if (!*bmePtr) *bmePtr = new Adafruit_BME280();

    if ((*bmePtr)->begin(address, &Wire)) {
        *readyPtr = true;
        Serial.printf("[SensorReader] BME280 at 0x%02X initialized\n", address);
        return true;
    }
    Serial.printf("[SensorReader] Failed to initialize BME280 at 0x%02X\n", address);
    return false;
}

Adafruit_BME280* SensorReader::getBME280(uint8_t address) {
    if (address == 0x76 && _bme280_0x76_ready) return _bme280_0x76;
    if (address == 0x77 && _bme280_0x77_ready) return _bme280_0x77;
    return nullptr;
}

// ============================================================================
// BME680 Implementation
// ============================================================================

bool SensorReader::initBME680(uint8_t address) {
    Serial.printf("[SensorReader] Initializing BME680 at 0x%02X...\n", address);
    Adafruit_BME680** bmePtr = (address == 0x76) ? &_bme680_0x76 : &_bme680_0x77;
    bool* readyPtr = (address == 0x76) ? &_bme680_0x76_ready : &_bme680_0x77_ready;

    if (address != 0x76 && address != 0x77) return false;
    if (*readyPtr) return true;
    if (!*bmePtr) *bmePtr = new Adafruit_BME680();

    if ((*bmePtr)->begin(address)) {
        (*bmePtr)->setTemperatureOversampling(BME680_OS_8X);
        (*bmePtr)->setHumidityOversampling(BME680_OS_2X);
        (*bmePtr)->setPressureOversampling(BME680_OS_4X);
        (*bmePtr)->setIIRFilterSize(BME680_FILTER_SIZE_3);
        (*bmePtr)->setGasHeater(320, 150);
        *readyPtr = true;
        Serial.printf("[SensorReader] BME680 at 0x%02X initialized\n", address);
        return true;
    }
    return false;
}

Adafruit_BME680* SensorReader::getBME680(uint8_t address) {
    if (address == 0x76 && _bme680_0x76_ready) return _bme680_0x76;
    if (address == 0x77 && _bme680_0x77_ready) return _bme680_0x77;
    return nullptr;
}

// ============================================================================
// SHT31 Implementation
// ============================================================================

bool SensorReader::initSHT31(uint8_t address) {
    Serial.printf("[SensorReader] Initializing SHT31 at 0x%02X...\n", address);
    ClosedCube_SHT31D** shtPtr = (address == 0x44) ? &_sht31_0x44 : &_sht31_0x45;
    bool* readyPtr = (address == 0x44) ? &_sht31_0x44_ready : &_sht31_0x45_ready;

    if (address != 0x44 && address != 0x45) return false;
    if (*readyPtr) return true;
    if (!*shtPtr) *shtPtr = new ClosedCube_SHT31D();

    SHT31D_ErrorCode error = (*shtPtr)->begin(address);
    if (error == SHT3XD_NO_ERROR) {
        *readyPtr = true;
        Serial.printf("[SensorReader] SHT31 at 0x%02X initialized\n", address);
        return true;
    }
    return false;
}

ClosedCube_SHT31D* SensorReader::getSHT31(uint8_t address) {
    if (address == 0x44 && _sht31_0x44_ready) return _sht31_0x44;
    if (address == 0x45 && _sht31_0x45_ready) return _sht31_0x45;
    return nullptr;
}

// ============================================================================
// DS18B20 Implementation
// ============================================================================

bool SensorReader::initDS18B20(int pin) {
    if (pin < 0) return false;
    Serial.printf("[SensorReader] Initializing DS18B20 on pin %d...\n", pin);

    if (_ds18b20_pin != pin) {
        delete _ds18b20; delete _oneWire;
        _ds18b20 = nullptr; _oneWire = nullptr;
        _ds18b20_ready = false;
    }
    if (_ds18b20_ready) return true;

    _oneWire = new OneWire(pin);
    _ds18b20 = new DallasTemperature(_oneWire);
    _ds18b20->begin();
    _ds18b20_pin = pin;

    if (_ds18b20->getDeviceCount() > 0) {
        _ds18b20_ready = true;
        Serial.printf("[SensorReader] DS18B20 initialized, %d device(s)\n", _ds18b20->getDeviceCount());
        return true;
    }
    return false;
}

// ============================================================================
// BH1750 (GY-302) Light Sensor Implementation
// ============================================================================

bool SensorReader::initBH1750(uint8_t address) {
    Serial.printf("[SensorReader] Initializing BH1750 at 0x%02X...\n", address);
    BH1750** bhPtr = (address == 0x23) ? &_bh1750_0x23 : &_bh1750_0x5C;
    bool* readyPtr = (address == 0x23) ? &_bh1750_0x23_ready : &_bh1750_0x5C_ready;

    if (address != 0x23 && address != 0x5C) {
        Serial.printf("[SensorReader] Invalid BH1750 address: 0x%02X\n", address);
        return false;
    }
    if (*readyPtr) return true;
    if (!*bhPtr) *bhPtr = new BH1750(address);

    if ((*bhPtr)->begin(BH1750::CONTINUOUS_HIGH_RES_MODE)) {
        *readyPtr = true;
        Serial.printf("[SensorReader] BH1750 (GY-302) at 0x%02X initialized\n", address);
        return true;
    }
    Serial.printf("[SensorReader] Failed to initialize BH1750 at 0x%02X\n", address);
    return false;
}

BH1750* SensorReader::getBH1750(uint8_t address) {
    if (address == 0x23 && _bh1750_0x23_ready) return _bh1750_0x23;
    if (address == 0x5C && _bh1750_0x5C_ready) return _bh1750_0x5C;
    return nullptr;
}

// ============================================================================
// TSL2561 Light Sensor Implementation
// ============================================================================

bool SensorReader::initTSL2561(uint8_t address) {
    Serial.printf("[SensorReader] Initializing TSL2561 at 0x%02X...\n", address);
    Adafruit_TSL2561_Unified** tslPtr = nullptr;
    bool* readyPtr = nullptr;

    if (address == 0x29) { tslPtr = &_tsl2561_0x29; readyPtr = &_tsl2561_0x29_ready; }
    else if (address == 0x39) { tslPtr = &_tsl2561_0x39; readyPtr = &_tsl2561_0x39_ready; }
    else if (address == 0x49) { tslPtr = &_tsl2561_0x49; readyPtr = &_tsl2561_0x49_ready; }
    else return false;

    if (*readyPtr) return true;
    if (!*tslPtr) *tslPtr = new Adafruit_TSL2561_Unified(address, 12345);

    if ((*tslPtr)->begin()) {
        (*tslPtr)->enableAutoRange(true);
        (*tslPtr)->setIntegrationTime(TSL2561_INTEGRATIONTIME_101MS);
        *readyPtr = true;
        Serial.printf("[SensorReader] TSL2561 at 0x%02X initialized\n", address);
        return true;
    }
    return false;
}

Adafruit_TSL2561_Unified* SensorReader::getTSL2561(uint8_t address) {
    if (address == 0x29 && _tsl2561_0x29_ready) return _tsl2561_0x29;
    if (address == 0x39 && _tsl2561_0x39_ready) return _tsl2561_0x39;
    if (address == 0x49 && _tsl2561_0x49_ready) return _tsl2561_0x49;
    return nullptr;
}

// ============================================================================
// SCD30 CO2 Sensor Implementation
// ============================================================================

bool SensorReader::initSCD30() {
    Serial.println("[SensorReader] Initializing SCD30...");
    if (_scd30_ready) return true;
    if (!_scd30) _scd30 = new SCD30();

    if (_scd30->begin()) {
        _scd30->setMeasurementInterval(2);
        _scd30_ready = true;
        Serial.println("[SensorReader] SCD30 initialized");
        return true;
    }
    return false;
}

// ============================================================================
// SCD4x (SCD40/SCD41) CO2 Sensor Implementation
// ============================================================================

bool SensorReader::initSCD4x() {
    Serial.println("[SensorReader] Initializing SCD4x...");
    if (_scd4x_ready) return true;
    if (!_scd4x) _scd4x = new SensirionI2CScd4x();

    _scd4x->begin(Wire);
    uint16_t error = _scd4x->stopPeriodicMeasurement();
    if (error == 0) {
        error = _scd4x->startPeriodicMeasurement();
        if (error == 0) {
            _scd4x_ready = true;
            Serial.println("[SensorReader] SCD4x initialized");
            return true;
        }
    }
    return false;
}

// ============================================================================
// CCS811 CO2/VOC Sensor Implementation
// ============================================================================

bool SensorReader::initCCS811(uint8_t address) {
    Serial.printf("[SensorReader] Initializing CCS811 at 0x%02X...\n", address);
    Adafruit_CCS811** ccsPtr = (address == 0x5A) ? &_ccs811_0x5A : &_ccs811_0x5B;
    bool* readyPtr = (address == 0x5A) ? &_ccs811_0x5A_ready : &_ccs811_0x5B_ready;

    if (address != 0x5A && address != 0x5B) return false;
    if (*readyPtr) return true;
    if (!*ccsPtr) *ccsPtr = new Adafruit_CCS811();

    if ((*ccsPtr)->begin(address)) {
        // Wait for sensor to be ready
        while (!(*ccsPtr)->available());
        *readyPtr = true;
        Serial.printf("[SensorReader] CCS811 at 0x%02X initialized\n", address);
        return true;
    }
    return false;
}

Adafruit_CCS811* SensorReader::getCCS811(uint8_t address) {
    if (address == 0x5A && _ccs811_0x5A_ready) return _ccs811_0x5A;
    if (address == 0x5B && _ccs811_0x5B_ready) return _ccs811_0x5B;
    return nullptr;
}

// ============================================================================
// SGP30 CO2/VOC Sensor Implementation
// ============================================================================

bool SensorReader::initSGP30() {
    Serial.println("[SensorReader] Initializing SGP30...");
    if (_sgp30_ready) return true;
    if (!_sgp30) _sgp30 = new Adafruit_SGP30();

    if (_sgp30->begin()) {
        _sgp30_ready = true;
        Serial.println("[SensorReader] SGP30 initialized");
        return true;
    }
    return false;
}

// ============================================================================
// VL53L0X Distance Sensor Implementation
// ============================================================================

bool SensorReader::initVL53L0X() {
    Serial.println("[SensorReader] Initializing VL53L0X...");
    if (_vl53l0x_ready) return true;
    if (!_vl53l0x) _vl53l0x = new VL53L0X();

    _vl53l0x->setTimeout(500);
    if (_vl53l0x->init()) {
        _vl53l0x->startContinuous();
        _vl53l0x_ready = true;
        Serial.println("[SensorReader] VL53L0X initialized");
        return true;
    }
    return false;
}

// ============================================================================
// ADS1115 ADC Implementation
// ============================================================================

bool SensorReader::initADS1115(uint8_t address) {
    Serial.printf("[SensorReader] Initializing ADS1115 at 0x%02X...\n", address);
    Adafruit_ADS1115** adsPtr = (address == 0x48) ? &_ads1115_0x48 : &_ads1115_0x49;
    bool* readyPtr = (address == 0x48) ? &_ads1115_0x48_ready : &_ads1115_0x49_ready;

    if (address != 0x48 && address != 0x49) return false;
    if (*readyPtr) return true;
    if (!*adsPtr) *adsPtr = new Adafruit_ADS1115();

    if ((*adsPtr)->begin(address)) {
        (*adsPtr)->setGain(GAIN_ONE);  // +/- 4.096V
        *readyPtr = true;
        Serial.printf("[SensorReader] ADS1115 at 0x%02X initialized\n", address);
        return true;
    }
    return false;
}

Adafruit_ADS1115* SensorReader::getADS1115(uint8_t address) {
    if (address == 0x48 && _ads1115_0x48_ready) return _ads1115_0x48;
    if (address == 0x49 && _ads1115_0x49_ready) return _ads1115_0x49;
    return nullptr;
}

// ============================================================================
// DHT22 Implementation
// ============================================================================

bool SensorReader::initDHT22(int pin) {
    if (pin < 0) return false;
    Serial.printf("[SensorReader] Initializing DHT22 on pin %d...\n", pin);

    if (_dht22_pin != pin) {
        delete _dht22;
        _dht22 = nullptr;
        _dht22_ready = false;
    }
    if (_dht22_ready) return true;

    _dht22 = new DHT(pin, DHT22);
    _dht22->begin();
    _dht22_pin = pin;
    _dht22_ready = true;
    Serial.printf("[SensorReader] DHT22 initialized on pin %d\n", pin);
    return true;
}

// ============================================================================
// JSN-SR04T Ultrasonic Sensor Implementation
// ============================================================================

bool SensorReader::initUltrasonic(int triggerPin, int echoPin) {
    if (triggerPin < 0 || echoPin < 0) return false;
    Serial.printf("[SensorReader] Initializing Ultrasonic (JSN-SR04T) trigger=%d, echo=%d...\n", triggerPin, echoPin);

    if (_ultrasonic_ready && _ultrasonic_trigger_pin == triggerPin && _ultrasonic_echo_pin == echoPin) {
        return true;
    }

    pinMode(triggerPin, OUTPUT);
    pinMode(echoPin, INPUT);
    digitalWrite(triggerPin, LOW);

    _ultrasonic_trigger_pin = triggerPin;
    _ultrasonic_echo_pin = echoPin;
    _ultrasonic_ready = true;
    Serial.println("[SensorReader] Ultrasonic sensor initialized");
    return true;
}

// ============================================================================
// NEO-6M GPS Module Implementation
// ============================================================================

bool SensorReader::initGPS(int rxPin, int txPin) {
    if (rxPin < 0 || txPin < 0) return false;
    Serial.printf("[SensorReader] Initializing GPS (NEO-6M) RX=%d, TX=%d...\n", rxPin, txPin);

    if (_gps_ready && _gps_rx_pin == rxPin && _gps_tx_pin == txPin) {
        return true;
    }

    if (!_gps) _gps = new TinyGPSPlus();

    // Use UARTManager to allocate UART dynamically based on pins
    UARTManager& uartMgr = UARTManager::getInstance();
    int uartNum = uartMgr.allocate(rxPin, txPin, 9600, "GPS", false);  // Use Arduino API

    if (uartNum < 0) {
        Serial.println("[SensorReader] Failed to allocate UART for GPS!");
        return false;
    }

    _gpsSerial = uartMgr.getSerial(uartNum);
    if (!_gpsSerial) {
        Serial.println("[SensorReader] Failed to get GPS serial!");
        return false;
    }

    _gps_rx_pin = rxPin;
    _gps_tx_pin = txPin;
    _gps_ready = true;
    Serial.printf("[SensorReader] GPS initialized on UART%d\n", uartNum);
    return true;
}

// ============================================================================
// SR04M-2 Waterproof Ultrasonic Sensor (UART Mode) Implementation
// ============================================================================

// Static baud rate tracking for SR04M-2 / JSN-SR04T / A02YYUW
// UART mode default is 9600 baud (MODE pin open = UART mode)
// TX output is 3.3V TTL compatible - no level shifter needed!
// Try 115200 first (some sensors use higher baud), fallback to 9600
static int sr04m2_current_baud = 115200;
static int sr04m2_fail_count = 0;
static bool sr04m2_auto_mode_detected = false;
static bool sr04m2_try_inverted = false;  // Standard UART (not inverted)

bool SensorReader::initSR04M2(int rxPin, int txPin, int baudRate) {
    // RX pin is required, TX pin is optional (can be -1 for RX-only mode)
    if (rxPin < 0) return false;

    // Use passed baud rate if valid, otherwise use default from static variable
    int actualBaudRate = (baudRate > 0) ? baudRate : sr04m2_current_baud;
    sr04m2_current_baud = actualBaudRate;  // Update static variable for future reads

    Serial.printf("[SensorReader] Initializing SR04M-2 (UART) RX=%d, TX=%s at %d baud...\n",
                  rxPin, txPin < 0 ? "none" : String(txPin).c_str(), actualBaudRate);

    // Use UARTManager to allocate UART dynamically based on pins
    UARTManager& uartMgr = UARTManager::getInstance();
    int uartNum = uartMgr.allocate(rxPin, txPin, actualBaudRate, "SR04M2", true);  // Use ESP-IDF API

    if (uartNum < 0) {
        Serial.println("[SensorReader] Failed to allocate UART for SR04M-2!");
        return false;
    }

    // Try inverted RX if we're getting garbage data (0x00, 0xC0 instead of 0xFF, 0xFE)
    // Some sensors have inverted UART output
    if (sr04m2_try_inverted) {
        uart_port_t uart_port = (uartNum == 1) ? UART_NUM_1 : UART_NUM_2;
        esp_err_t err = uart_set_line_inverse(uart_port, UART_SIGNAL_RXD_INV);
        if (err == ESP_OK) {
            Serial.println("[SR04M-2] RX signal INVERTED enabled");
        }
    }

    _sr04m2_rx_pin = rxPin;
    _sr04m2_tx_pin = txPin;
    _sr04m2_ready = true;
    Serial.printf("[SensorReader] SR04M-2 initialized on UART%d at %d baud (RX-only: %s, inverted: %s)\n",
                  uartNum, actualBaudRate, txPin < 0 ? "yes" : "no", sr04m2_try_inverted ? "yes" : "no");
    return true;
}

// Baud rate is fixed at 9600 for SR04M-2 per spec

#endif // PLATFORM_ESP32

// ============================================================================
// Sensor Initialization Router
// ============================================================================

bool SensorReader::initializeSensor(const SensorAssignmentConfig& config) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();

    int sdaPin = (config.sdaPin > 0) ? config.sdaPin : DEFAULT_SDA_PIN;
    int sclPin = (config.sclPin > 0) ? config.sclPin : DEFAULT_SCL_PIN;
    initI2C(sdaPin, sclPin);

    uint8_t i2cAddr = parseI2CAddress(config.i2cAddress);
    Serial.printf("[SensorReader] Initializing: %s at 0x%02X\n", sensorCode.c_str(), i2cAddr);

    // BME280
    if (sensorCode.indexOf("BME280") >= 0 || sensorCode.indexOf("BMP280") >= 0) {
        return initBME280(i2cAddr == 0 ? 0x76 : i2cAddr);
    }
    // BME680
    if (sensorCode.indexOf("BME680") >= 0) {
        return initBME680(i2cAddr == 0 ? 0x76 : i2cAddr);
    }
    // SHT31
    if (sensorCode.indexOf("SHT31") >= 0 || sensorCode.indexOf("SHT3X") >= 0) {
        return initSHT31(i2cAddr == 0 ? 0x44 : i2cAddr);
    }
    // DS18B20
    if (sensorCode.indexOf("DS18B20") >= 0 || sensorCode.indexOf("DALLAS") >= 0) {
        return initDS18B20(config.oneWirePin > 0 ? config.oneWirePin : 4);
    }
    // BH1750 / GY-302
    if (sensorCode.indexOf("BH1750") >= 0 || sensorCode.indexOf("GY302") >= 0 ||
        sensorCode.indexOf("GY-302") >= 0) {
        return initBH1750(i2cAddr == 0 ? 0x23 : i2cAddr);
    }
    // TSL2561
    if (sensorCode.indexOf("TSL2561") >= 0 || sensorCode.indexOf("TSL2591") >= 0) {
        return initTSL2561(i2cAddr == 0 ? 0x39 : i2cAddr);
    }
    // SCD30
    if (sensorCode.indexOf("SCD30") >= 0) {
        return initSCD30();
    }
    // SCD40/SCD41
    if (sensorCode.indexOf("SCD40") >= 0 || sensorCode.indexOf("SCD41") >= 0 ||
        sensorCode.indexOf("SCD4X") >= 0) {
        return initSCD4x();
    }
    // CCS811
    if (sensorCode.indexOf("CCS811") >= 0) {
        return initCCS811(i2cAddr == 0 ? 0x5A : i2cAddr);
    }
    // SGP30
    if (sensorCode.indexOf("SGP30") >= 0) {
        return initSGP30();
    }
    // VL53L0X
    if (sensorCode.indexOf("VL53L0X") >= 0 || sensorCode.indexOf("VL53L1X") >= 0) {
        return initVL53L0X();
    }
    // ADS1115
    if (sensorCode.indexOf("ADS1115") >= 0 || sensorCode.indexOf("ADS1015") >= 0) {
        return initADS1115(i2cAddr == 0 ? 0x48 : i2cAddr);
    }
    // DHT22
    if (sensorCode.indexOf("DHT22") >= 0 || sensorCode.indexOf("DHT") >= 0 ||
        sensorCode.indexOf("AM2302") >= 0) {
        int pin = config.digitalPin > 0 ? config.digitalPin : 4;  // Default to GPIO 4
        return initDHT22(pin);
    }
    // SR04M-2 Waterproof Ultrasonic (UART Mode) - MUST be checked BEFORE generic SR04!
    if (sensorCode.indexOf("SR04M-2") >= 0 || sensorCode.indexOf("SR04M2") >= 0) {
        // For SR04M-2, we use analogPin as RX and digitalPin as TX
        int rxPin = config.analogPin > 0 ? config.analogPin : 19;  // Default GPIO 19
        int txPin = config.digitalPin > 0 ? config.digitalPin : 18; // Default GPIO 18
        return initSR04M2(rxPin, txPin);
    }
    // JSN-SR04T / HC-SR04 / Generic Ultrasonic (GPIO trigger/echo mode)
    if (sensorCode.indexOf("JSN-SR04T") >= 0 || sensorCode.indexOf("SR04") >= 0 ||
        sensorCode.indexOf("ULTRASONIC") >= 0 || sensorCode.indexOf("HCSR04") >= 0) {
        int trig = config.triggerPin > 0 ? config.triggerPin : 5;  // Default GPIO 5
        int echo = config.echoPin > 0 ? config.echoPin : 18;       // Default GPIO 18
        return initUltrasonic(trig, echo);
    }
    // NEO-6M / GPS
    if (sensorCode.indexOf("NEO-6M") >= 0 || sensorCode.indexOf("NEO6M") >= 0 ||
        sensorCode.indexOf("GPS") >= 0 || sensorCode.indexOf("UBLOX") >= 0) {
        // For GPS, we use analogPin as RX and digitalPin as TX (or defaults)
        int rxPin = config.analogPin > 0 ? config.analogPin : 16;  // Default GPIO 16
        int txPin = config.digitalPin > 0 ? config.digitalPin : 17; // Default GPIO 17
        return initGPS(rxPin, txPin);
    }

    Serial.printf("[SensorReader] Unknown sensor: %s\n", sensorCode.c_str());
    return false;
#else
    return false;
#endif
}

// ============================================================================
// Value Reading Router
// ============================================================================

SensorReading SensorReader::readValue(const String& measurementType, const SensorAssignmentConfig& config) {
    String type = measurementType;
    type.toLowerCase();

    if (type.indexOf("temp") >= 0 && type.indexOf("water") < 0) return readTemperature(config);
    if (type.indexOf("water_temp") >= 0) return readTemperature(config);  // DS18B20 water temp
    if (type.indexOf("humid") >= 0 || type.indexOf("hum") >= 0) return readHumidity(config);
    if (type.indexOf("pressure") >= 0 || type.indexOf("press") >= 0) return readPressure(config);
    if (type.indexOf("light") >= 0 || type.indexOf("lux") >= 0 || type.indexOf("illumin") >= 0) return readLight(config);
    if (type.indexOf("co2") >= 0 || type.indexOf("carbon") >= 0) return readCO2(config);
    if (type.indexOf("tvoc") >= 0 || type.indexOf("voc") >= 0) return readTVOC(config);
    if (type.indexOf("gas") >= 0 || type.indexOf("air_quality") >= 0) return readGasResistance(config);
    if (type.indexOf("distance") >= 0 || type.indexOf("range") >= 0) return readDistance(config);
    if (type.indexOf("water_level") >= 0 || type.indexOf("level") >= 0) return readWaterLevel(config);
    if (type.indexOf("analog") >= 0 || type.indexOf("adc") >= 0) return readAnalog(config);
    if (type.indexOf("latitude") >= 0 || type.indexOf("lat") >= 0) return readLatitude(config);
    if (type.indexOf("longitude") >= 0 || type.indexOf("lng") >= 0 || type.indexOf("lon") >= 0) return readLongitude(config);
    if (type.indexOf("altitude") >= 0 || type.indexOf("alt") >= 0) return readAltitude(config);
    if (type.indexOf("speed") >= 0) return readSpeed(config);
    // GPS Status readings
    if (type.indexOf("gps_satellites") >= 0 || type.indexOf("satellites") >= 0) return readGpsSatellites(config);
    if (type.indexOf("gps_fix") >= 0 || type.indexOf("fix_type") >= 0) return readGpsFix(config);
    if (type.indexOf("gps_hdop") >= 0 || type.indexOf("hdop") >= 0) return readGpsHdop(config);

    return SensorReading("Unknown measurement type: " + measurementType);
}

// ============================================================================
// Temperature Reading
// ============================================================================

SensorReading SensorReader::readTemperature(const SensorAssignmentConfig& config) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();
    uint8_t i2cAddr = parseI2CAddress(config.i2cAddress);

    // BME280
    if (sensorCode.indexOf("BME280") >= 0 || sensorCode.indexOf("BMP280") >= 0) {
        if (i2cAddr == 0) i2cAddr = 0x76;
        Adafruit_BME280* bme = getBME280(i2cAddr);
        if (!bme && initBME280(i2cAddr)) bme = getBME280(i2cAddr);
        if (bme) {
            float temp = bme->readTemperature();
            Serial.printf("[SensorReader] BME280 Temp: %.2f°C\n", temp);
            return SensorReading(temp);
        }
        return SensorReading("BME280 not available");
    }

    // BME680
    if (sensorCode.indexOf("BME680") >= 0) {
        if (i2cAddr == 0) i2cAddr = 0x76;
        Adafruit_BME680* bme = getBME680(i2cAddr);
        if (!bme && initBME680(i2cAddr)) bme = getBME680(i2cAddr);
        if (bme && bme->performReading()) {
            Serial.printf("[SensorReader] BME680 Temp: %.2f°C\n", bme->temperature);
            return SensorReading(bme->temperature);
        }
        return SensorReading("BME680 not available");
    }

    // SHT31
    if (sensorCode.indexOf("SHT31") >= 0 || sensorCode.indexOf("SHT3X") >= 0) {
        if (i2cAddr == 0) i2cAddr = 0x44;
        ClosedCube_SHT31D* sht = getSHT31(i2cAddr);
        if (!sht && initSHT31(i2cAddr)) sht = getSHT31(i2cAddr);
        if (sht) {
            SHT31D result = sht->readTempAndHumidity(SHT3XD_REPEATABILITY_HIGH, SHT3XD_MODE_CLOCK_STRETCH, 50);
            if (result.error == SHT3XD_NO_ERROR) {
                Serial.printf("[SensorReader] SHT31 Temp: %.2f°C\n", result.t);
                return SensorReading(result.t);
            }
        }
        return SensorReading("SHT31 not available");
    }

    // DS18B20
    if (sensorCode.indexOf("DS18B20") >= 0 || sensorCode.indexOf("DALLAS") >= 0) {
        int pin = config.oneWirePin > 0 ? config.oneWirePin : 4;
        if (!_ds18b20_ready && !initDS18B20(pin)) return SensorReading("DS18B20 not available");
        if (_ds18b20) {
            _ds18b20->requestTemperatures();
            float temp = _ds18b20->getTempCByIndex(0);
            if (temp != DEVICE_DISCONNECTED_C) {
                Serial.printf("[SensorReader] DS18B20 Temp: %.2f°C\n", temp);
                return SensorReading(temp);
            }
        }
        return SensorReading("DS18B20 disconnected");
    }

    // SCD30 (also has temperature)
    if (sensorCode.indexOf("SCD30") >= 0) {
        if (!_scd30_ready && !initSCD30()) return SensorReading("SCD30 not available");
        if (_scd30 && _scd30->dataAvailable()) {
            float temp = _scd30->getTemperature();
            Serial.printf("[SensorReader] SCD30 Temp: %.2f°C\n", temp);
            return SensorReading(temp);
        }
        return SensorReading("SCD30 data not ready");
    }

    // DHT22
    if (sensorCode.indexOf("DHT22") >= 0 || sensorCode.indexOf("DHT") >= 0 ||
        sensorCode.indexOf("AM2302") >= 0) {
        int pin = config.digitalPin > 0 ? config.digitalPin : 4;
        if (!_dht22_ready && !initDHT22(pin)) return SensorReading("DHT22 not available");
        if (_dht22) {
            float temp = _dht22->readTemperature();
            if (!isnan(temp)) {
                Serial.printf("[SensorReader] DHT22 Temp: %.2f°C\n", temp);
                return SensorReading(temp);
            }
        }
        return SensorReading("DHT22 read failed");
    }

    return SensorReading("No temperature sensor: " + config.sensorCode);
#else
    return SensorReading("Hardware not available on native");
#endif
}

// ============================================================================
// Humidity Reading
// ============================================================================

SensorReading SensorReader::readHumidity(const SensorAssignmentConfig& config) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();
    uint8_t i2cAddr = parseI2CAddress(config.i2cAddress);

    // BME280
    if (sensorCode.indexOf("BME280") >= 0) {
        if (i2cAddr == 0) i2cAddr = 0x76;
        Adafruit_BME280* bme = getBME280(i2cAddr);
        if (!bme && initBME280(i2cAddr)) bme = getBME280(i2cAddr);
        if (bme) {
            float hum = bme->readHumidity();
            Serial.printf("[SensorReader] BME280 Humidity: %.2f%%\n", hum);
            return SensorReading(hum);
        }
        return SensorReading("BME280 not available");
    }

    // BME680
    if (sensorCode.indexOf("BME680") >= 0) {
        if (i2cAddr == 0) i2cAddr = 0x76;
        Adafruit_BME680* bme = getBME680(i2cAddr);
        if (!bme && initBME680(i2cAddr)) bme = getBME680(i2cAddr);
        if (bme && bme->performReading()) {
            Serial.printf("[SensorReader] BME680 Humidity: %.2f%%\n", bme->humidity);
            return SensorReading(bme->humidity);
        }
        return SensorReading("BME680 not available");
    }

    // SHT31
    if (sensorCode.indexOf("SHT31") >= 0 || sensorCode.indexOf("SHT3X") >= 0) {
        if (i2cAddr == 0) i2cAddr = 0x44;
        ClosedCube_SHT31D* sht = getSHT31(i2cAddr);
        if (!sht && initSHT31(i2cAddr)) sht = getSHT31(i2cAddr);
        if (sht) {
            SHT31D result = sht->readTempAndHumidity(SHT3XD_REPEATABILITY_HIGH, SHT3XD_MODE_CLOCK_STRETCH, 50);
            if (result.error == SHT3XD_NO_ERROR) {
                Serial.printf("[SensorReader] SHT31 Humidity: %.2f%%\n", result.rh);
                return SensorReading(result.rh);
            }
        }
        return SensorReading("SHT31 not available");
    }

    // SCD30
    if (sensorCode.indexOf("SCD30") >= 0) {
        if (!_scd30_ready && !initSCD30()) return SensorReading("SCD30 not available");
        if (_scd30 && _scd30->dataAvailable()) {
            float hum = _scd30->getHumidity();
            Serial.printf("[SensorReader] SCD30 Humidity: %.2f%%\n", hum);
            return SensorReading(hum);
        }
        return SensorReading("SCD30 data not ready");
    }

    // DHT22
    if (sensorCode.indexOf("DHT22") >= 0 || sensorCode.indexOf("DHT") >= 0 ||
        sensorCode.indexOf("AM2302") >= 0) {
        int pin = config.digitalPin > 0 ? config.digitalPin : 4;
        if (!_dht22_ready && !initDHT22(pin)) return SensorReading("DHT22 not available");
        if (_dht22) {
            float hum = _dht22->readHumidity();
            if (!isnan(hum)) {
                Serial.printf("[SensorReader] DHT22 Humidity: %.2f%%\n", hum);
                return SensorReading(hum);
            }
        }
        return SensorReading("DHT22 read failed");
    }

    return SensorReading("No humidity sensor: " + config.sensorCode);
#else
    return SensorReading("Hardware not available on native");
#endif
}

// ============================================================================
// Pressure Reading
// ============================================================================

SensorReading SensorReader::readPressure(const SensorAssignmentConfig& config) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();
    uint8_t i2cAddr = parseI2CAddress(config.i2cAddress);

    // BME280/BMP280
    if (sensorCode.indexOf("BME280") >= 0 || sensorCode.indexOf("BMP280") >= 0) {
        if (i2cAddr == 0) i2cAddr = 0x76;
        Adafruit_BME280* bme = getBME280(i2cAddr);
        if (!bme && initBME280(i2cAddr)) bme = getBME280(i2cAddr);
        if (bme) {
            float pressure = bme->readPressure() / 100.0F;
            Serial.printf("[SensorReader] BME280 Pressure: %.2f hPa\n", pressure);
            return SensorReading(pressure);
        }
        return SensorReading("BME280 not available");
    }

    // BME680
    if (sensorCode.indexOf("BME680") >= 0) {
        if (i2cAddr == 0) i2cAddr = 0x76;
        Adafruit_BME680* bme = getBME680(i2cAddr);
        if (!bme && initBME680(i2cAddr)) bme = getBME680(i2cAddr);
        if (bme && bme->performReading()) {
            float pressure = bme->pressure / 100.0F;
            Serial.printf("[SensorReader] BME680 Pressure: %.2f hPa\n", pressure);
            return SensorReading(pressure);
        }
        return SensorReading("BME680 not available");
    }

    return SensorReading("No pressure sensor: " + config.sensorCode);
#else
    return SensorReading("Hardware not available on native");
#endif
}

// ============================================================================
// Gas Resistance Reading (BME680)
// ============================================================================

SensorReading SensorReader::readGasResistance(const SensorAssignmentConfig& config) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();
    uint8_t i2cAddr = parseI2CAddress(config.i2cAddress);

    if (sensorCode.indexOf("BME680") >= 0) {
        if (i2cAddr == 0) i2cAddr = 0x76;
        Adafruit_BME680* bme = getBME680(i2cAddr);
        if (!bme && initBME680(i2cAddr)) bme = getBME680(i2cAddr);
        if (bme && bme->performReading()) {
            float gasRes = bme->gas_resistance / 1000.0F;
            Serial.printf("[SensorReader] BME680 Gas: %.2f kOhms\n", gasRes);
            return SensorReading(gasRes);
        }
        return SensorReading("BME680 not available");
    }

    return SensorReading("Gas resistance only on BME680");
#else
    return SensorReading("Hardware not available on native");
#endif
}

// ============================================================================
// Light Reading (BH1750 / GY-302 / TSL2561)
// ============================================================================

SensorReading SensorReader::readLight(const SensorAssignmentConfig& config) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();
    uint8_t i2cAddr = parseI2CAddress(config.i2cAddress);

    // BH1750 / GY-302
    if (sensorCode.indexOf("BH1750") >= 0 || sensorCode.indexOf("GY302") >= 0 ||
        sensorCode.indexOf("GY-302") >= 0) {
        if (i2cAddr == 0) i2cAddr = 0x23;
        BH1750* bh = getBH1750(i2cAddr);
        if (!bh && initBH1750(i2cAddr)) bh = getBH1750(i2cAddr);
        if (bh) {
            float lux = bh->readLightLevel();
            if (lux >= 0) {
                Serial.printf("[SensorReader] BH1750 Light: %.2f lux\n", lux);
                return SensorReading(lux);
            }
        }
        return SensorReading("BH1750 not available");
    }

    // TSL2561
    if (sensorCode.indexOf("TSL2561") >= 0 || sensorCode.indexOf("TSL2591") >= 0) {
        if (i2cAddr == 0) i2cAddr = 0x39;
        Adafruit_TSL2561_Unified* tsl = getTSL2561(i2cAddr);
        if (!tsl && initTSL2561(i2cAddr)) tsl = getTSL2561(i2cAddr);
        if (tsl) {
            sensors_event_t event;
            tsl->getEvent(&event);
            if (event.light > 0) {
                Serial.printf("[SensorReader] TSL2561 Light: %.2f lux\n", event.light);
                return SensorReading(event.light);
            }
        }
        return SensorReading("TSL2561 not available");
    }

    return SensorReading("No light sensor: " + config.sensorCode);
#else
    return SensorReading("Hardware not available on native");
#endif
}

// ============================================================================
// CO2 Reading (SCD30, SCD40, CCS811, SGP30)
// ============================================================================

SensorReading SensorReader::readCO2(const SensorAssignmentConfig& config) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();
    uint8_t i2cAddr = parseI2CAddress(config.i2cAddress);

    // SCD30
    if (sensorCode.indexOf("SCD30") >= 0) {
        if (!_scd30_ready && !initSCD30()) return SensorReading("SCD30 not available");
        if (_scd30 && _scd30->dataAvailable()) {
            float co2 = _scd30->getCO2();
            Serial.printf("[SensorReader] SCD30 CO2: %.0f ppm\n", co2);
            return SensorReading(co2);
        }
        return SensorReading("SCD30 data not ready");
    }

    // SCD40/SCD41
    if (sensorCode.indexOf("SCD40") >= 0 || sensorCode.indexOf("SCD41") >= 0 ||
        sensorCode.indexOf("SCD4X") >= 0) {
        if (!_scd4x_ready && !initSCD4x()) return SensorReading("SCD4x not available");
        if (_scd4x) {
            uint16_t co2;
            float temperature, humidity;
            bool ready = false;
            _scd4x->getDataReadyFlag(ready);
            if (ready) {
                uint16_t error = _scd4x->readMeasurement(co2, temperature, humidity);
                if (error == 0) {
                    Serial.printf("[SensorReader] SCD4x CO2: %d ppm\n", co2);
                    return SensorReading((double)co2);
                }
            }
        }
        return SensorReading("SCD4x data not ready");
    }

    // CCS811
    if (sensorCode.indexOf("CCS811") >= 0) {
        if (i2cAddr == 0) i2cAddr = 0x5A;
        Adafruit_CCS811* ccs = getCCS811(i2cAddr);
        if (!ccs && initCCS811(i2cAddr)) ccs = getCCS811(i2cAddr);
        if (ccs && ccs->available() && !ccs->readData()) {
            uint16_t co2 = ccs->geteCO2();
            Serial.printf("[SensorReader] CCS811 CO2: %d ppm\n", co2);
            return SensorReading((double)co2);
        }
        return SensorReading("CCS811 not available");
    }

    // SGP30
    if (sensorCode.indexOf("SGP30") >= 0) {
        if (!_sgp30_ready && !initSGP30()) return SensorReading("SGP30 not available");
        if (_sgp30 && _sgp30->IAQmeasure()) {
            uint16_t co2 = _sgp30->eCO2;
            Serial.printf("[SensorReader] SGP30 CO2: %d ppm\n", co2);
            return SensorReading((double)co2);
        }
        return SensorReading("SGP30 reading failed");
    }

    return SensorReading("No CO2 sensor: " + config.sensorCode);
#else
    return SensorReading("Hardware not available on native");
#endif
}

// ============================================================================
// TVOC Reading (CCS811, SGP30)
// ============================================================================

SensorReading SensorReader::readTVOC(const SensorAssignmentConfig& config) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();
    uint8_t i2cAddr = parseI2CAddress(config.i2cAddress);

    // CCS811
    if (sensorCode.indexOf("CCS811") >= 0) {
        if (i2cAddr == 0) i2cAddr = 0x5A;
        Adafruit_CCS811* ccs = getCCS811(i2cAddr);
        if (!ccs && initCCS811(i2cAddr)) ccs = getCCS811(i2cAddr);
        if (ccs && ccs->available() && !ccs->readData()) {
            uint16_t tvoc = ccs->getTVOC();
            Serial.printf("[SensorReader] CCS811 TVOC: %d ppb\n", tvoc);
            return SensorReading((double)tvoc);
        }
        return SensorReading("CCS811 not available");
    }

    // SGP30
    if (sensorCode.indexOf("SGP30") >= 0) {
        if (!_sgp30_ready && !initSGP30()) return SensorReading("SGP30 not available");
        if (_sgp30 && _sgp30->IAQmeasure()) {
            uint16_t tvoc = _sgp30->TVOC;
            Serial.printf("[SensorReader] SGP30 TVOC: %d ppb\n", tvoc);
            return SensorReading((double)tvoc);
        }
        return SensorReading("SGP30 reading failed");
    }

    return SensorReading("No TVOC sensor: " + config.sensorCode);
#else
    return SensorReading("Hardware not available on native");
#endif
}

// ============================================================================
// Distance Reading (VL53L0X, SR04M-2)
// ============================================================================

SensorReading SensorReader::readDistance(const SensorAssignmentConfig& config) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();

    if (sensorCode.indexOf("VL53L0X") >= 0 || sensorCode.indexOf("VL53L1X") >= 0) {
        if (!_vl53l0x_ready && !initVL53L0X()) return SensorReading("VL53L0X not available");
        if (_vl53l0x) {
            uint16_t distance = _vl53l0x->readRangeContinuousMillimeters();
            if (!_vl53l0x->timeoutOccurred()) {
                Serial.printf("[SensorReader] VL53L0X Distance: %d mm\n", distance);
                return SensorReading((double)distance);
            }
        }
        return SensorReading("VL53L0X timeout");
    }

    // SR04M-2 - use readWaterLevel and convert to cm (same sensor, same reading)
    if (sensorCode.indexOf("SR04M-2") >= 0 || sensorCode.indexOf("SR04M2") >= 0) {
        return readWaterLevel(config);
    }

    return SensorReading("No distance sensor: " + config.sensorCode);
#else
    return SensorReading("Hardware not available on native");
#endif
}

// ============================================================================
// Analog Reading (ADS1115)
// ============================================================================

SensorReading SensorReader::readAnalog(const SensorAssignmentConfig& config, int channel) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();
    uint8_t i2cAddr = parseI2CAddress(config.i2cAddress);

    if (sensorCode.indexOf("ADS1115") >= 0 || sensorCode.indexOf("ADS1015") >= 0) {
        if (i2cAddr == 0) i2cAddr = 0x48;
        Adafruit_ADS1115* ads = getADS1115(i2cAddr);
        if (!ads && initADS1115(i2cAddr)) ads = getADS1115(i2cAddr);
        if (ads) {
            int16_t adc = ads->readADC_SingleEnded(channel);
            float voltage = ads->computeVolts(adc);
            Serial.printf("[SensorReader] ADS1115 Ch%d: %.4f V (raw: %d)\n", channel, voltage, adc);
            return SensorReading(voltage);
        }
        return SensorReading("ADS1115 not available");
    }

    // Fallback to ESP32 internal ADC
    if (config.analogPin > 0) {
        int rawValue = analogRead(config.analogPin);
        float voltage = (rawValue / 4095.0) * 3.3;
        Serial.printf("[SensorReader] ESP32 ADC Pin %d: %.2f V\n", config.analogPin, voltage);
        return SensorReading(voltage);
    }

    return SensorReading("No analog sensor: " + config.sensorCode);
#else
    return SensorReading("Hardware not available on native");
#endif
}

// ============================================================================
// Water Level Reading (JSN-SR04T / SR04M-2 Ultrasonic)
// ============================================================================

SensorReading SensorReader::readWaterLevel(const SensorAssignmentConfig& config) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();

    // SR04M-2 / JSN-SR04T / A02YYUW Waterproof Ultrasonic
    // Check if GPIO mode is configured (triggerPin/echoPin set) - Mode 0 = HC-SR04 style
    // If not, try UART mode (MODE pin open): 9600 baud, continuous auto-mode every ~100ms
    // IMPORTANT: User's sensor may be in Mode 0 (GPIO) even if labeled as "SR04M-2 UART"
    // Check board resistors: if no 200k/360k/470k resistor on MODE pad = GPIO mode!
    bool useGPIOMode = config.triggerPin > 0 && config.echoPin > 0;

    if ((sensorCode.indexOf("SR04M-2") >= 0 || sensorCode.indexOf("SR04M2") >= 0 ||
        sensorCode.indexOf("JSN-SR04T") >= 0 || sensorCode.indexOf("A02YYUW") >= 0) && !useGPIOMode) {

        // SR04M-2 UART Mode (Auto-send every ~100ms)
        // Frame format: 0xFF 0xFE DIST_HIGH DIST_LOW CHECKSUM (5 bytes)
        // Checksum = (DIST_HIGH + DIST_LOW) & 0xFF
        // Sensor TX -> ESP32 RX (GPIO 23 default), no TX needed (-1)

        int rxPin = config.analogPin > 0 ? config.analogPin : 23;   // ESP RX <- Sensor TX (GPIO 23 default)
        int txPin = config.digitalPin > 0 ? config.digitalPin : -1; // ESP TX -> not used for auto-mode

        int baudRate = config.baudRate > 0 ? config.baudRate : 115200;  // Default to 115200 if not configured
        Serial.printf("[SR04M-2] UART mode - RX=GPIO%d, TX=%s, Baud=%d (from config: %d)\n",
                      rxPin, txPin < 0 ? "none" : String(txPin).c_str(), baudRate, config.baudRate);

        // Always reinitialize to apply current baud rate
        _sr04m2_ready = false;
        if (!initSR04M2(rxPin, txPin, baudRate)) {
            return SensorReading("SR04M-2 not available");
        }

        // Get UART port from UARTManager (dynamically allocated)
        UARTManager& uartMgr = UARTManager::getInstance();
        int uartNum_int = uartMgr.getUartForOwner("SR04M2");
        if (uartNum_int < 0) {
            return SensorReading("SR04M-2 UART not allocated");
        }
        const uart_port_t uart_num = (uartNum_int == 1) ? UART_NUM_1 : UART_NUM_2;

        // Clear RX buffer first
        uart_flush_input(uart_num);
        delay(10);

        // Wait for frame (sensor sends every ~100ms in auto-mode)
        // Frame: 0xFF 0xFE DIST_HIGH DIST_LOW CHECKSUM
        unsigned long startTime = millis();
        uint8_t buffer[32];
        int bufferPos = 0;
        bool frameFound = false;
        int frameStartIdx = -1;

        Serial.println("[SR04M-2] Waiting for frame (0xFF 0xFE header)...");

        // Read up to 500ms or until we find a valid frame
        while (millis() - startTime < 500 && bufferPos < 30 && !frameFound) {
            size_t buffered;
            uart_get_buffered_data_len(uart_num, &buffered);

            if (buffered > 0) {
                uint8_t byte;
                int len = uart_read_bytes(uart_num, &byte, 1, pdMS_TO_TICKS(10));
                if (len > 0) {
                    buffer[bufferPos++] = byte;
                    Serial.printf("[SR04M-2] Byte %d: 0x%02X\n", bufferPos, byte);

                    // Look for frame header 0xFF 0xFE
                    if (bufferPos >= 2) {
                        for (int i = 0; i <= bufferPos - 2; i++) {
                            if (buffer[i] == 0xFF && buffer[i + 1] == 0xFE) {
                                frameStartIdx = i;
                                // Check if we have all 5 bytes of the frame
                                if (bufferPos >= frameStartIdx + 5) {
                                    frameFound = true;
                                    Serial.printf("[SR04M-2] Frame found at position %d!\n", frameStartIdx);
                                }
                                break;
                            }
                        }
                    }
                }
            }
            delay(1);
        }

        if (!frameFound) {
            Serial.printf("[SR04M-2] No frame found in %d bytes\n", bufferPos);
            if (bufferPos == 0) {
                Serial.println("[SR04M-2] No data received - check wiring:");
                Serial.printf("  - Sensor TX -> ESP32 GPIO %d (RX)\n", rxPin);
                Serial.println("  - Sensor 5V -> ESP32 5V");
                Serial.println("  - Sensor GND -> ESP32 GND");
            } else {
                Serial.println("[SR04M-2] Data received but no 0xFF 0xFE header found");
                Serial.println("[SR04M-2] Check: sensor is in UART mode (not PWM), baud=9600");
            }
            return SensorReading("SR04M-2 no valid frame");
        }

        // Extract frame data
        uint8_t header1 = buffer[frameStartIdx];     // 0xFF
        uint8_t header2 = buffer[frameStartIdx + 1]; // 0xFE
        uint8_t highByte = buffer[frameStartIdx + 2];
        uint8_t lowByte = buffer[frameStartIdx + 3];
        uint8_t checksum = buffer[frameStartIdx + 4];

        Serial.printf("[SR04M-2] Frame: [0x%02X 0x%02X] H=0x%02X L=0x%02X CS=0x%02X\n",
                      header1, header2, highByte, lowByte, checksum);

        // Validate checksum: (DIST_HIGH + DIST_LOW) & 0xFF
        uint8_t calculatedChecksum = (highByte + lowByte) & 0xFF;
        if (calculatedChecksum != checksum) {
            Serial.printf("[SR04M-2] Checksum error: calc=0x%02X, recv=0x%02X\n",
                         calculatedChecksum, checksum);
            return SensorReading("SR04M-2 checksum error");
        }

        // Calculate distance in cm (value is in mm)
        uint16_t distance_mm = (highByte << 8) | lowByte;
        float distance_cm = distance_mm / 10.0;

        // Valid range is 20-750 cm for SR04M-2
        if (distance_mm < 200 || distance_mm > 7500) {
            Serial.printf("[SR04M-2] Out of range: %u mm (%.1f cm)\n", distance_mm, distance_cm);
            return SensorReading("SR04M-2 out of range");
        }

        Serial.printf("[SR04M-2] SUCCESS! Distance: %u mm (%.2f cm)\n", distance_mm, distance_cm);
        return SensorReading(distance_cm);
    }

    // JSN-SR04T / HC-SR04 / SR04M-2 Ultrasonic (GPIO Trigger/Echo Mode)
    // This is used when triggerPin/echoPin are configured, or for sensors in Mode 0 (HC-SR04 style)
    // ⚠ ECHO pin outputs 5V! Use voltage divider: ECHO → 10kΩ → ESP32 → 15kΩ → GND for ~3.2V
    if (sensorCode.indexOf("JSN-SR04T") >= 0 || sensorCode.indexOf("SR04") >= 0 ||
        sensorCode.indexOf("ULTRASONIC") >= 0 || sensorCode.indexOf("HCSR04") >= 0 ||
        (useGPIOMode && (sensorCode.indexOf("SR04M-2") >= 0 || sensorCode.indexOf("SR04M2") >= 0))) {

        int trig = config.triggerPin > 0 ? config.triggerPin : 23;  // Default TRIG pin
        int echo = config.echoPin > 0 ? config.echoPin : 22;        // Default ECHO pin (needs voltage divider!)

        Serial.printf("[Ultrasonic-GPIO] Mode 0 (HC-SR04 style) - TRIG=GPIO%d, ECHO=GPIO%d\n", trig, echo);

        if (!_ultrasonic_ready && !initUltrasonic(trig, echo)) {
            return SensorReading("Ultrasonic not available");
        }

        // Check ECHO pin state before trigger
        int echoStateBefore = digitalRead(_ultrasonic_echo_pin);
        Serial.printf("[Ultrasonic-GPIO] ECHO pin state before trigger: %s\n", echoStateBefore ? "HIGH" : "LOW");

        // Send pulse
        digitalWrite(_ultrasonic_trigger_pin, LOW);
        delayMicroseconds(2);
        digitalWrite(_ultrasonic_trigger_pin, HIGH);
        delayMicroseconds(10);
        digitalWrite(_ultrasonic_trigger_pin, LOW);

        Serial.println("[Ultrasonic-GPIO] Trigger pulse sent (10µs HIGH)");

        // Measure echo time (timeout after 50ms = ~8.5m range - extended for debugging)
        long duration = pulseIn(_ultrasonic_echo_pin, HIGH, 50000);

        if (duration == 0) {
            // Check ECHO pin state after timeout
            int echoStateAfter = digitalRead(_ultrasonic_echo_pin);
            Serial.printf("[Ultrasonic-GPIO] TIMEOUT! ECHO pin state after: %s\n", echoStateAfter ? "HIGH (stuck!)" : "LOW");
            Serial.println("[Ultrasonic-GPIO] Possible causes:");
            Serial.println("  1. TRIG/ECHO pins swapped - try swapping wires");
            Serial.println("  2. Voltage divider issue - ECHO needs 5V->3.3V divider");
            Serial.println("  3. Sensor not powered (needs 5V VCC)");
            Serial.println("  4. Object too close (<20cm) or too far (>450cm)");
            return SensorReading("Ultrasonic timeout");
        }

        // Calculate distance in cm (speed of sound = 343 m/s = 0.0343 cm/µs)
        // Distance = (duration / 2) * 0.0343
        float distance_cm = (duration / 2.0) * 0.0343;

        Serial.printf("[Ultrasonic-GPIO] SUCCESS! Duration: %ld µs, Distance: %.2f cm\n", duration, distance_cm);
        return SensorReading(distance_cm);
    }

    return SensorReading("No water level sensor: " + config.sensorCode);
#else
    return SensorReading("Hardware not available on native");
#endif
}

// ============================================================================
// GPS Latitude Reading (NEO-6M)
// ============================================================================

SensorReading SensorReader::readLatitude(const SensorAssignmentConfig& config) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();

    if (sensorCode.indexOf("NEO-6M") >= 0 || sensorCode.indexOf("NEO6M") >= 0 ||
        sensorCode.indexOf("GPS") >= 0 || sensorCode.indexOf("UBLOX") >= 0) {

        int rxPin = config.analogPin > 0 ? config.analogPin : 16;
        int txPin = config.digitalPin > 0 ? config.digitalPin : 17;

        if (!_gps_ready && !initGPS(rxPin, txPin)) {
            return SensorReading("GPS not available");
        }

        // Read GPS data (up to 100ms)
        unsigned long start = millis();
        while (millis() - start < 100) {
            while (_gpsSerial->available() > 0) {
                _gps->encode(_gpsSerial->read());
            }
        }

        if (_gps->location.isValid()) {
            double lat = _gps->location.lat();
            Serial.printf("[SensorReader] GPS Latitude: %.6f°\n", lat);
            return SensorReading(lat);
        }

        // Auto-start GPS debug diagnostics once when no fix
        if (!_gps_debug_ran) {
            Serial.println("\n[SensorReader] GPS no fix detected - running diagnostics automatically...\n");
            _gps_debug_ran = true;

            // Release GPS UART allocation via UARTManager to avoid conflict with debugGPS
            UARTManager& uartMgr = UARTManager::getInstance();
            uartMgr.releaseByOwner("GPS");
            _gpsSerial = nullptr;
            _gps_ready = false;

            // Run GPS debug diagnostics (15 seconds)
            HardwareScanner scanner;
            scanner.debugGPS(rxPin, txPin, 15);

            // Re-initialize GPS after debug (UARTManager will allocate fresh)
            Serial.println("\n[SensorReader] Re-initializing GPS after diagnostics...");
            initGPS(rxPin, txPin);
        }

        return SensorReading("GPS no fix");
    }

    return SensorReading("No GPS sensor: " + config.sensorCode);
#else
    return SensorReading("Hardware not available on native");
#endif
}

// ============================================================================
// GPS Longitude Reading (NEO-6M)
// ============================================================================

SensorReading SensorReader::readLongitude(const SensorAssignmentConfig& config) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();

    if (sensorCode.indexOf("NEO-6M") >= 0 || sensorCode.indexOf("NEO6M") >= 0 ||
        sensorCode.indexOf("GPS") >= 0 || sensorCode.indexOf("UBLOX") >= 0) {

        int rxPin = config.analogPin > 0 ? config.analogPin : 16;
        int txPin = config.digitalPin > 0 ? config.digitalPin : 17;

        if (!_gps_ready && !initGPS(rxPin, txPin)) {
            return SensorReading("GPS not available");
        }

        // Read GPS data (up to 100ms)
        unsigned long start = millis();
        while (millis() - start < 100) {
            while (_gpsSerial->available() > 0) {
                _gps->encode(_gpsSerial->read());
            }
        }

        if (_gps->location.isValid()) {
            double lng = _gps->location.lng();
            Serial.printf("[SensorReader] GPS Longitude: %.6f°\n", lng);
            return SensorReading(lng);
        }
        return SensorReading("GPS no fix");
    }

    return SensorReading("No GPS sensor: " + config.sensorCode);
#else
    return SensorReading("Hardware not available on native");
#endif
}

// ============================================================================
// GPS Altitude Reading (NEO-6M)
// ============================================================================

SensorReading SensorReader::readAltitude(const SensorAssignmentConfig& config) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();

    if (sensorCode.indexOf("NEO-6M") >= 0 || sensorCode.indexOf("NEO6M") >= 0 ||
        sensorCode.indexOf("GPS") >= 0 || sensorCode.indexOf("UBLOX") >= 0) {

        int rxPin = config.analogPin > 0 ? config.analogPin : 16;
        int txPin = config.digitalPin > 0 ? config.digitalPin : 17;

        if (!_gps_ready && !initGPS(rxPin, txPin)) {
            return SensorReading("GPS not available");
        }

        // Read GPS data (up to 100ms)
        unsigned long start = millis();
        while (millis() - start < 100) {
            while (_gpsSerial->available() > 0) {
                _gps->encode(_gpsSerial->read());
            }
        }

        if (_gps->altitude.isValid()) {
            double alt = _gps->altitude.meters();
            Serial.printf("[SensorReader] GPS Altitude: %.2f m\n", alt);
            return SensorReading(alt);
        }
        return SensorReading("GPS altitude not available");
    }

    return SensorReading("No GPS sensor: " + config.sensorCode);
#else
    return SensorReading("Hardware not available on native");
#endif
}

// ============================================================================
// GPS Speed Reading (NEO-6M)
// ============================================================================

SensorReading SensorReader::readSpeed(const SensorAssignmentConfig& config) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();

    if (sensorCode.indexOf("NEO-6M") >= 0 || sensorCode.indexOf("NEO6M") >= 0 ||
        sensorCode.indexOf("GPS") >= 0 || sensorCode.indexOf("UBLOX") >= 0) {

        int rxPin = config.analogPin > 0 ? config.analogPin : 16;
        int txPin = config.digitalPin > 0 ? config.digitalPin : 17;

        if (!_gps_ready && !initGPS(rxPin, txPin)) {
            return SensorReading("GPS not available");
        }

        // Read GPS data (up to 100ms)
        unsigned long start = millis();
        while (millis() - start < 100) {
            while (_gpsSerial->available() > 0) {
                _gps->encode(_gpsSerial->read());
            }
        }

        if (_gps->speed.isValid()) {
            double speed = _gps->speed.kmph();
            Serial.printf("[SensorReader] GPS Speed: %.2f km/h\n", speed);
            return SensorReading(speed);
        }
        return SensorReading("GPS speed not available");
    }

    return SensorReading("No GPS sensor: " + config.sensorCode);
#else
    return SensorReading("Hardware not available on native");
#endif
}

// ============================================================================
// GPS Satellites Reading (NEO-6M)
// ============================================================================

SensorReading SensorReader::readGpsSatellites(const SensorAssignmentConfig& config) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();

    if (sensorCode.indexOf("NEO-6M") >= 0 || sensorCode.indexOf("NEO6M") >= 0 ||
        sensorCode.indexOf("GPS") >= 0 || sensorCode.indexOf("UBLOX") >= 0) {

        int rxPin = config.analogPin > 0 ? config.analogPin : 16;
        int txPin = config.digitalPin > 0 ? config.digitalPin : 17;

        if (!_gps_ready && !initGPS(rxPin, txPin)) {
            return SensorReading("GPS not available");
        }

        // Read GPS data (up to 100ms)
        unsigned long start = millis();
        while (millis() - start < 100) {
            while (_gpsSerial->available() > 0) {
                _gps->encode(_gpsSerial->read());
            }
        }

        if (_gps->satellites.isValid()) {
            int satellites = _gps->satellites.value();
            Serial.printf("[SensorReader] GPS Satellites: %d\n", satellites);
            return SensorReading((double)satellites);
        }
        // Return 0 satellites if not valid (cold start)
        Serial.println("[SensorReader] GPS Satellites: 0 (no valid data)");
        return SensorReading(0.0);
    }

    return SensorReading("No GPS sensor: " + config.sensorCode);
#else
    return SensorReading("Hardware not available on native");
#endif
}

// ============================================================================
// GPS Fix Type Reading (NEO-6M)
// Returns: 0 = no fix, 1 = GPS fix, 2 = DGPS fix
// ============================================================================

SensorReading SensorReader::readGpsFix(const SensorAssignmentConfig& config) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();

    if (sensorCode.indexOf("NEO-6M") >= 0 || sensorCode.indexOf("NEO6M") >= 0 ||
        sensorCode.indexOf("GPS") >= 0 || sensorCode.indexOf("UBLOX") >= 0) {

        int rxPin = config.analogPin > 0 ? config.analogPin : 16;
        int txPin = config.digitalPin > 0 ? config.digitalPin : 17;

        if (!_gps_ready && !initGPS(rxPin, txPin)) {
            return SensorReading("GPS not available");
        }

        // Read GPS data (up to 100ms)
        unsigned long start = millis();
        while (millis() - start < 100) {
            while (_gpsSerial->available() > 0) {
                _gps->encode(_gpsSerial->read());
            }
        }

        // Determine fix type based on location validity and satellite count
        // TinyGPS++ doesn't expose fix quality directly, so we infer it
        int fixType = 0;
        if (_gps->location.isValid()) {
            // Has valid fix
            if (_gps->satellites.isValid() && _gps->satellites.value() >= 4) {
                fixType = 3; // 3D fix (4+ satellites)
            } else {
                fixType = 2; // 2D fix (less than 4 satellites)
            }
        }

        Serial.printf("[SensorReader] GPS Fix Type: %d\n", fixType);
        return SensorReading((double)fixType);
    }

    return SensorReading("No GPS sensor: " + config.sensorCode);
#else
    return SensorReading("Hardware not available on native");
#endif
}

// ============================================================================
// GPS HDOP (Horizontal Dilution of Precision) Reading (NEO-6M)
// Lower is better: <1 = Ideal, 1-2 = Excellent, 2-5 = Good, 5-10 = Moderate
// ============================================================================

SensorReading SensorReader::readGpsHdop(const SensorAssignmentConfig& config) {
#ifdef PLATFORM_ESP32
    String sensorCode = config.sensorCode;
    sensorCode.toUpperCase();

    if (sensorCode.indexOf("NEO-6M") >= 0 || sensorCode.indexOf("NEO6M") >= 0 ||
        sensorCode.indexOf("GPS") >= 0 || sensorCode.indexOf("UBLOX") >= 0) {

        int rxPin = config.analogPin > 0 ? config.analogPin : 16;
        int txPin = config.digitalPin > 0 ? config.digitalPin : 17;

        if (!_gps_ready && !initGPS(rxPin, txPin)) {
            return SensorReading("GPS not available");
        }

        // Read GPS data (up to 100ms)
        unsigned long start = millis();
        while (millis() - start < 100) {
            while (_gpsSerial->available() > 0) {
                _gps->encode(_gpsSerial->read());
            }
        }

        if (_gps->hdop.isValid()) {
            double hdop = _gps->hdop.hdop();
            Serial.printf("[SensorReader] GPS HDOP: %.2f\n", hdop);
            return SensorReading(hdop);
        }
        // Return 99.99 as invalid/unknown HDOP
        Serial.println("[SensorReader] GPS HDOP: 99.99 (no valid data)");
        return SensorReading(99.99);
    }

    return SensorReading("No GPS sensor: " + config.sensorCode);
#else
    return SensorReading("Hardware not available on native");
#endif
}

// ============================================================================
// Helper Functions
// ============================================================================

bool SensorReader::isSensorAvailable(const SensorAssignmentConfig& config) {
    return initializeSensor(config);
}

String SensorReader::getSensorType(const String& sensorCode) {
    String code = sensorCode;
    code.toUpperCase();

    if (code.indexOf("BME280") >= 0) return "BME280";
    if (code.indexOf("BMP280") >= 0) return "BMP280";
    if (code.indexOf("BME680") >= 0) return "BME680";
    if (code.indexOf("SHT31") >= 0 || code.indexOf("SHT3X") >= 0) return "SHT31";
    if (code.indexOf("DS18B20") >= 0 || code.indexOf("DALLAS") >= 0) return "DS18B20";
    if (code.indexOf("BH1750") >= 0 || code.indexOf("GY302") >= 0 || code.indexOf("GY-302") >= 0) return "BH1750";
    if (code.indexOf("TSL2561") >= 0) return "TSL2561";
    if (code.indexOf("SCD30") >= 0) return "SCD30";
    if (code.indexOf("SCD40") >= 0 || code.indexOf("SCD41") >= 0) return "SCD4x";
    if (code.indexOf("CCS811") >= 0) return "CCS811";
    if (code.indexOf("SGP30") >= 0) return "SGP30";
    if (code.indexOf("VL53L0X") >= 0 || code.indexOf("VL53L1X") >= 0) return "VL53L0X";
    if (code.indexOf("ADS1115") >= 0 || code.indexOf("ADS1015") >= 0) return "ADS1115";
    if (code.indexOf("SR04M-2") >= 0 || code.indexOf("SR04M2") >= 0) return "SR04M-2";
    if (code.indexOf("NEO-6M") >= 0 || code.indexOf("NEO6M") >= 0 || code.indexOf("GPS") >= 0) return "NEO-6M";
    if (code.indexOf("DHT22") >= 0 || code.indexOf("AM2302") >= 0) return "DHT22";
    if (code.indexOf("JSN-SR04T") >= 0 || code.indexOf("HCSR04") >= 0) return "JSN-SR04T";

    return "UNKNOWN";
}
