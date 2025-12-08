/**
 * myIoTGrid.Sensor - UART Manager Implementation
 *
 * Dynamic UART port allocation for ESP32.
 */

#include "uart_manager.h"

#ifdef PLATFORM_ESP32

UARTManager& UARTManager::getInstance() {
    static UARTManager instance;
    return instance;
}

UARTManager::UARTManager()
    : _serial1(nullptr)
    , _serial2(nullptr)
{
    // Initialize allocation slots
    for (int i = 0; i < 2; i++) {
        _allocations[i].uartNum = -1;
        _allocations[i].rxPin = -1;
        _allocations[i].txPin = -1;
        _allocations[i].baudRate = 0;
        _allocations[i].owner = "";
        _allocations[i].serial = nullptr;
        _allocations[i].useEspIdf = false;
    }
}

UARTManager::~UARTManager() {
    releaseAll();
    delete _serial1;
    delete _serial2;
}

int UARTManager::allocate(int rxPin, int txPin, int baudRate, const String& owner, bool useEspIdf) {
    Serial.printf("[UARTManager] Allocate request: owner=%s, RX=%d, TX=%d, baud=%d, ESP-IDF=%s\n",
                  owner.c_str(), rxPin, txPin, baudRate, useEspIdf ? "yes" : "no");

    // Check if these pins are already allocated
    int existingUart = getUartForPins(rxPin, txPin);
    if (existingUart > 0) {
        int idx = existingUart - 1;
        // If same owner or compatible config, return existing allocation
        if (_allocations[idx].owner == owner ||
            (_allocations[idx].rxPin == rxPin &&
             (_allocations[idx].txPin == txPin || txPin < 0))) {
            Serial.printf("[UARTManager] Reusing existing UART%d for %s\n", existingUart, owner.c_str());

            // Update baud rate if different (requires reinit)
            if (_allocations[idx].baudRate != baudRate) {
                Serial.printf("[UARTManager] Baud rate changed %d -> %d, reinitializing\n",
                              _allocations[idx].baudRate, baudRate);
                endUart(existingUart);
                _allocations[idx].baudRate = baudRate;
                if (useEspIdf) {
                    initEspIdf(existingUart, rxPin, txPin, baudRate);
                } else {
                    initArduino(existingUart, rxPin, txPin, baudRate);
                }
            }
            return existingUart;
        } else {
            Serial.printf("[UARTManager] ERROR: Pins already in use by %s!\n",
                          _allocations[idx].owner.c_str());
            return -1;
        }
    }

    // Find first available UART
    int uartNum = getFirstAvailable();
    if (uartNum < 0) {
        Serial.println("[UARTManager] ERROR: No UART available!");
        return -1;
    }

    // Allocate
    int idx = uartNum - 1;
    _allocations[idx].uartNum = uartNum;
    _allocations[idx].rxPin = rxPin;
    _allocations[idx].txPin = txPin;
    _allocations[idx].baudRate = baudRate;
    _allocations[idx].owner = owner;
    _allocations[idx].useEspIdf = useEspIdf;

    // Initialize UART
    bool success;
    if (useEspIdf) {
        success = initEspIdf(uartNum, rxPin, txPin, baudRate);
    } else {
        success = initArduino(uartNum, rxPin, txPin, baudRate);
    }

    if (!success) {
        Serial.printf("[UARTManager] ERROR: Failed to initialize UART%d!\n", uartNum);
        _allocations[idx].uartNum = -1;
        _allocations[idx].owner = "";
        return -1;
    }

    Serial.printf("[UARTManager] Allocated UART%d for %s (RX=%d, TX=%d, %d baud)\n",
                  uartNum, owner.c_str(), rxPin, txPin, baudRate);
    return uartNum;
}

bool UARTManager::initArduino(int uartNum, int rxPin, int txPin, int baudRate) {
    HardwareSerial** serialPtr = (uartNum == 1) ? &_serial1 : &_serial2;

    // Create HardwareSerial instance if needed
    if (!*serialPtr) {
        *serialPtr = new HardwareSerial(uartNum);
    }

    // Configure pins and start
    int actualTxPin = (txPin < 0) ? -1 : txPin;  // -1 for RX-only mode
    (*serialPtr)->begin(baudRate, SERIAL_8N1, rxPin, actualTxPin);

    _allocations[uartNum - 1].serial = *serialPtr;

    Serial.printf("[UARTManager] Arduino Serial%d initialized\n", uartNum);
    return true;
}

bool UARTManager::initEspIdf(int uartNum, int rxPin, int txPin, int baudRate) {
    uart_port_t uart_port = (uartNum == 1) ? UART_NUM_1 : UART_NUM_2;

    // Delete existing driver if any
    uart_driver_delete(uart_port);

    uart_config_t uart_config = {
        .baud_rate = baudRate,
        .data_bits = UART_DATA_8_BITS,
        .parity = UART_PARITY_DISABLE,
        .stop_bits = UART_STOP_BITS_1,
        .flow_ctrl = UART_HW_FLOWCTRL_DISABLE,
        .rx_flow_ctrl_thresh = 0,
        .source_clk = UART_SCLK_APB,
    };

    esp_err_t err = uart_param_config(uart_port, &uart_config);
    if (err != ESP_OK) {
        Serial.printf("[UARTManager] uart_param_config failed: %d\n", err);
        return false;
    }

    // Set pins: TX (ESP->Sensor), RX (Sensor->ESP)
    int actualTxPin = (txPin < 0) ? UART_PIN_NO_CHANGE : txPin;
    err = uart_set_pin(uart_port, actualTxPin, rxPin, UART_PIN_NO_CHANGE, UART_PIN_NO_CHANGE);
    if (err != ESP_OK) {
        Serial.printf("[UARTManager] uart_set_pin failed: %d\n", err);
        return false;
    }

    // Install UART driver with RX buffer
    err = uart_driver_install(uart_port, 256, 0, 0, NULL, 0);
    if (err != ESP_OK) {
        Serial.printf("[UARTManager] uart_driver_install failed: %d\n", err);
        return false;
    }

    _allocations[uartNum - 1].serial = nullptr;  // ESP-IDF doesn't use HardwareSerial

    Serial.printf("[UARTManager] ESP-IDF UART%d initialized\n", uartNum);
    return true;
}

void UARTManager::endUart(int uartNum) {
    if (uartNum < 1 || uartNum > 2) return;

    int idx = uartNum - 1;

    if (_allocations[idx].useEspIdf) {
        uart_port_t uart_port = (uartNum == 1) ? UART_NUM_1 : UART_NUM_2;
        uart_driver_delete(uart_port);
    } else {
        HardwareSerial* serial = _allocations[idx].serial;
        if (serial) {
            serial->end();
        }
    }

    Serial.printf("[UARTManager] UART%d ended\n", uartNum);
}

HardwareSerial* UARTManager::getSerial(int uartNum) {
    if (uartNum < 1 || uartNum > 2) return nullptr;
    int idx = uartNum - 1;

    if (_allocations[idx].uartNum < 0) return nullptr;
    if (_allocations[idx].useEspIdf) return nullptr;

    return _allocations[idx].serial;
}

int UARTManager::getUartForOwner(const String& owner) {
    for (int i = 0; i < 2; i++) {
        if (_allocations[i].uartNum > 0 && _allocations[i].owner == owner) {
            return _allocations[i].uartNum;
        }
    }
    return -1;
}

int UARTManager::getUartForPins(int rxPin, int txPin) {
    for (int i = 0; i < 2; i++) {
        if (_allocations[i].uartNum > 0) {
            bool rxMatch = (_allocations[i].rxPin == rxPin);
            bool txMatch = (txPin < 0) || (_allocations[i].txPin == txPin) || (_allocations[i].txPin < 0);
            if (rxMatch && txMatch) {
                return _allocations[i].uartNum;
            }
        }
    }
    return -1;
}

void UARTManager::release(int uartNum) {
    if (uartNum < 1 || uartNum > 2) return;

    int idx = uartNum - 1;
    if (_allocations[idx].uartNum < 0) return;

    Serial.printf("[UARTManager] Releasing UART%d (was: %s)\n",
                  uartNum, _allocations[idx].owner.c_str());

    endUart(uartNum);

    _allocations[idx].uartNum = -1;
    _allocations[idx].rxPin = -1;
    _allocations[idx].txPin = -1;
    _allocations[idx].baudRate = 0;
    _allocations[idx].owner = "";
    _allocations[idx].serial = nullptr;
    _allocations[idx].useEspIdf = false;
}

void UARTManager::releaseByOwner(const String& owner) {
    int uartNum = getUartForOwner(owner);
    if (uartNum > 0) {
        release(uartNum);
    }
}

void UARTManager::releaseByPins(int rxPin, int txPin) {
    int uartNum = getUartForPins(rxPin, txPin);
    if (uartNum > 0) {
        release(uartNum);
    }
}

bool UARTManager::isAvailable(int uartNum) {
    if (uartNum < 1 || uartNum > 2) return false;
    return _allocations[uartNum - 1].uartNum < 0;
}

int UARTManager::getFirstAvailable() {
    // Prefer UART2 first (UART1 default pins are flash pins)
    if (isAvailable(2)) return 2;
    if (isAvailable(1)) return 1;
    return -1;
}

void UARTManager::printAllocations() {
    Serial.println("\n[UARTManager] Current allocations:");
    Serial.println("----------------------------------------");
    for (int i = 0; i < 2; i++) {
        int uartNum = i + 1;
        if (_allocations[i].uartNum > 0) {
            Serial.printf("  UART%d: %s (RX=%d, TX=%d, %d baud, %s)\n",
                          uartNum,
                          _allocations[i].owner.c_str(),
                          _allocations[i].rxPin,
                          _allocations[i].txPin,
                          _allocations[i].baudRate,
                          _allocations[i].useEspIdf ? "ESP-IDF" : "Arduino");
        } else {
            Serial.printf("  UART%d: available\n", uartNum);
        }
    }
    Serial.println("----------------------------------------\n");
}

void UARTManager::releaseAll() {
    release(1);
    release(2);
}

#endif // PLATFORM_ESP32
