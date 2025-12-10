# myIoTGrid.NodeLoraWan

**LoRaWAN Sensor Node Firmware für Heltec LoRa32 V3**

[![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)](https://github.com/myiotgrid/myiotgrid)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-ESP32--S3-orange.svg)](https://www.espressif.com/)
[![LoRaWAN](https://img.shields.io/badge/LoRaWAN-EU868-purple.svg)](https://lora-alliance.org/)

---

## Übersicht

Diese Firmware ermöglicht die Nutzung eines **Heltec WiFi LoRa32 V3** als LoRaWAN-Sensor-Node für die myIoTGrid-Plattform. Der Sensor erfasst Umgebungsdaten und sendet diese über LoRaWAN an die Grid.LoRa Bridge und weiter an den Grid.Hub.

### Features

- 📡 **LoRaWAN 1.0.x** mit OTAA/ABP Aktivierung
- 🌡️ **BME280** Temperatur, Luftfeuchtigkeit, Luftdruck
- 💧 **Ultraschall-Wassersensor** für Pegelstand-Messung
- 🔋 **Deep Sleep** für Batteriebetrieb (Monate!)
- 📺 **OLED Display** für Status-Anzeige
- ⚙️ **Konfiguration** via Serial oder Downlink
- 🔄 **ADR** (Adaptive Data Rate) für optimale Übertragung

---

## Hardware

### Heltec WiFi LoRa32 V3

| Komponente | Beschreibung |
|------------|--------------|
| **MCU** | ESP32-S3 (240 MHz, Dual Core) |
| **LoRa** | Semtech SX1262 |
| **Display** | OLED 128x64 (SSD1306) |
| **Frequenz** | EU868 (867-869 MHz) |
| **Leistung** | bis +22 dBm |

### Pin-Belegung

```
┌─────────────────────────────────────────────────────────────┐
│              HELTEC LORA32 V3 PIN MAPPING                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  LoRa SX1262:                    I2C Sensoren:             │
│  ├── SCK   = GPIO 9              ├── SDA  = GPIO 41        │
│  ├── MISO  = GPIO 11             └── SCL  = GPIO 42        │
│  ├── MOSI  = GPIO 10                                       │
│  ├── CS    = GPIO 8              OLED Display:             │
│  ├── RST   = GPIO 12             ├── SDA  = GPIO 17        │
│  ├── DIO1  = GPIO 14             ├── SCL  = GPIO 18        │
│  └── BUSY  = GPIO 13             └── RST  = GPIO 21        │
│                                                             │
│  Ultraschall:                    System:                   │
│  ├── TRIG  = GPIO 5              ├── LED     = GPIO 35     │
│  └── ECHO  = GPIO 4              ├── Button  = GPIO 0      │
│                                  └── Battery = GPIO 1 (ADC)│
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Sensor-Verkabelung

#### BME280 (I2C)

| BME280 | Heltec |
|--------|--------|
| VCC | 3.3V |
| GND | GND |
| SDA | GPIO 41 |
| SCL | GPIO 42 |

#### HC-SR04 / JSN-SR04T (Ultraschall)

| Sensor | Heltec |
|--------|--------|
| VCC | 5V (3.3V für JSN-SR04T) |
| GND | GND |
| TRIG | GPIO 5 |
| ECHO | GPIO 4 |

---

## Installation

### Voraussetzungen

- [PlatformIO](https://platformio.org/) (VS Code Extension oder CLI)
- USB-Kabel (Typ-C für Heltec V3)
- LoRaWAN Gateway mit ChirpStack

### Build & Flash

```bash
# Repository klonen
git clone https://github.com/myiotgrid/myiotgrid.git
cd myiotgrid/myIoTGrid.NodeLoraWan

# Build für Heltec LoRa32 V3
pio run -e heltec_lora32_v3

# Flash
pio run -e heltec_lora32_v3 --target upload

# Serial Monitor
pio device monitor
```

### Build-Environments

| Environment | Beschreibung |
|-------------|--------------|
| `heltec_lora32_v3` | Produktion mit echten Sensoren |
| `heltec_lora32_v3_simulate` | Produktion mit simulierten Sensoren |
| `native` | Linux-Simulation für Entwicklung |
| `native_test` | Unit Tests |

---

## LoRaWAN Konfiguration

### Device in ChirpStack anlegen

1. Application erstellen oder auswählen
2. Device Profile für OTAA/EU868 erstellen
3. Device hinzufügen:
   - **DevEUI**: Wird aus ESP32 MAC generiert (siehe Serial Monitor)
   - **AppEUI**: Von ChirpStack Application
   - **AppKey**: Von ChirpStack Device

### Credentials konfigurieren

Via Serial Monitor (115200 Baud):

```
# DevEUI anzeigen
SHOW

# AppEUI setzen (16 Hex-Zeichen)
APPEUI=0011223344556677

# AppKey setzen (32 Hex-Zeichen)
APPKEY=00112233445566778899AABBCCDDEEFF

# Speichern
SAVE

# Neustart
# (Taste 5 Sekunden halten oder Device resetten)
```

### Beispiel-Ausgabe

```
=======================================
  myIoTGrid NodeLoraWan v1.0.0
  Heltec LoRa32 V3 - LoRaWAN Sensor
=======================================
[INFO]  Initializing hardware...
[INFO]  Display initialized
[INFO]  Hardware initialized
[INFO]  Initializing sensors...
[INFO]  BME280 initialized
[INFO]    Temperature: 21.5 °C
[INFO]    Humidity: 55 %
[INFO]    Pressure: 1013 hPa
[INFO]  Initializing LoRaWAN...
[INFO]  Generated DevEUI: AABBCCDDFFEEDD11
=== LoRaWAN Credentials ===
DevEUI: AABBCCDDFFEEDD11
AppEUI: 0011223344556677
AppKey: 0011...EEFF (masked)
OTAA Ready: Yes
Frame Counter: 0
===========================
[INFO]  OTAA join attempt 1...
[INFO]  Joined network successfully!
```

---

## Payload-Format

### Sensor-Daten Encoding

Jeder Sensorwert wird in 3 Bytes kodiert:

```
[TypeID: 1 Byte][Value: 2 Bytes (int16, MSB first)]
```

| Sensor | TypeID | Wert-Skalierung |
|--------|--------|-----------------|
| Temperature | 0x01 | × 100 (2 Dezimalstellen) |
| Humidity | 0x02 | × 100 |
| Pressure | 0x03 | × 10 (1 Dezimalstelle) |
| Water Level | 0x04 | × 100 |
| Battery | 0x05 | × 100 |
| CO2 | 0x06 | × 100 |
| PM2.5 | 0x07 | × 100 |
| PM10 | 0x08 | × 100 |

### Beispiel-Payload

```
Hex: 01 07 3A 02 1A 2C 03 27 46 05 21 34

Dekodiert:
- 0x01 0x07 0x3A = Temperature: 18.50°C  (1850 / 100)
- 0x02 0x1A 0x2C = Humidity: 67.00%      (6700 / 100)
- 0x03 0x27 0x46 = Pressure: 1005.4 hPa  (10054 / 10)
- 0x05 0x21 0x34 = Battery: 85%          (8500 / 100)

Total: 12 bytes
```

### ChirpStack Codec (JavaScript)

```javascript
function decodeUplink(input) {
  var data = {};
  var bytes = input.bytes;

  for (var i = 0; i < bytes.length; i += 3) {
    var typeId = bytes[i];
    var value = (bytes[i+1] << 8) | bytes[i+2];

    // Handle signed int16
    if (value > 32767) value -= 65536;

    switch (typeId) {
      case 0x01: data.temperature = value / 100.0; break;
      case 0x02: data.humidity = value / 100.0; break;
      case 0x03: data.pressure = value / 10.0; break;
      case 0x04: data.water_level = value / 100.0; break;
      case 0x05: data.battery = value / 100.0; break;
      case 0x06: data.co2 = value / 100.0; break;
      case 0x07: data.pm25 = value / 100.0; break;
      case 0x08: data.pm10 = value / 100.0; break;
    }
  }

  return { data: data };
}
```

---

## Konfiguration via Downlink

### Port 10: Konfiguration

| Byte | Beschreibung |
|------|--------------|
| 0 | Intervall in Minuten (1-255) |
| 1 | Flags (optional) |

**Flags:**
- Bit 0: ADR Enable (1) / Disable (0)
- Bit 1-7: Reserviert

### Beispiel

```
# Intervall auf 10 Minuten setzen
Payload: 0A

# Intervall 5 Minuten + ADR deaktivieren
Payload: 05 00
```

---

## Button-Bedienung

| Aktion | Dauer | Funktion |
|--------|-------|----------|
| Kurz drücken | < 1s | Display-Screen wechseln |
| Mittel drücken | 1-5s | Sofortige Übertragung |
| Lang drücken | > 5s | Neustart |

---

## Display-Screens

1. **Status**: Join-Status, RSSI, SNR, Frame Counter, Batterie
2. **Readings**: Aktuelle Sensorwerte
3. **Config**: DevEUI, Intervall, Data Rate

Das Display schaltet nach 30 Sekunden automatisch ab (Stromsparen).

---

## Deep Sleep & Batteriebetrieb

### Stromverbrauch

| Modus | Verbrauch |
|-------|-----------|
| Aktiv (Transmission) | ~120 mA |
| Aktiv (Idle) | ~80 mA |
| Deep Sleep | ~10 µA |

### Batterielaufzeit (3000 mAh LiPo)

| Intervall | Laufzeit (geschätzt) |
|-----------|---------------------|
| 5 Minuten | ~6 Monate |
| 15 Minuten | ~12 Monate |
| 60 Minuten | ~18 Monate |

### Adaptive Sleep

Bei niedrigem Batteriestand wird das Schlaf-Intervall automatisch verlängert:
- < 20%: Intervall × 2
- < 10%: Intervall × 4

---

## Architektur

```
┌─────────────────────────────────────────────────────────────┐
│                         main.cpp                            │
│                    (State Machine)                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐      │
│   │LoRaConnec-  │   │   OLED      │   │   Power     │      │
│   │   tion      │   │  Display    │   │  Manager    │      │
│   └──────┬──────┘   └──────┬──────┘   └──────┬──────┘      │
│          │                 │                 │              │
│   ┌──────┴──────┐   ┌──────┴──────┐   ┌──────┴──────┐      │
│   │ Credential  │   │  BME280     │   │   Water     │      │
│   │  Manager    │   │  Sensor     │   │   Level     │      │
│   └──────┬──────┘   └──────┬──────┘   └──────┬──────┘      │
│          │                 │                 │              │
├──────────┴─────────────────┴─────────────────┴──────────────┤
│                      HAL Layer                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │  hal_lora   │  │  hal_lora32 │  │  hal_native │         │
│  │  (SX1262)   │  │   (ESP32)   │  │   (Linux)   │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
└─────────────────────────────────────────────────────────────┘
```

---

## Unit Tests

```bash
# Tests ausführen
pio test -e native_test

# Erwartete Ausgabe:
# test/test_payload_encoding.cpp:XX: PASSED
# ...
# 16 Tests 0 Failures 0 Ignored
```

---

## Troubleshooting

### "LoRa radio init failed"

- Überprüfe SPI-Verbindung (Kabel, Lötungen)
- Versuche Reset des Boards

### "OTAA join failed"

- DevEUI in ChirpStack korrekt?
- AppEUI/AppKey stimmen überein?
- Gateway in Reichweite?
- Antennen angeschlossen?

### "BME280 not found"

- I2C-Verkabelung prüfen
- I2C-Adresse: 0x76 oder 0x77?
- 3.3V Stromversorgung?

### Kein Display

- OLED_RST Pin korrekt?
- I2C-Adresse: 0x3C?

---

## Entwicklung

### Code-Struktur

```
myIoTGrid.NodeLoraWan/
├── src/
│   ├── main.cpp                 # Entry Point, State Machine
│   ├── lora_credentials.cpp     # Credential Management
│   ├── display/                 # OLED Display
│   └── power/                   # Power Management
├── include/
│   ├── config.h                 # Konfigurationskonstanten
│   ├── hal/                     # HAL Interfaces
│   └── *.h                      # Header Files
├── lib/
│   ├── connection/              # LoRaConnection
│   ├── sensor/                  # BME280, WaterLevel
│   ├── hal_lora32/              # ESP32 HAL Implementation
│   └── hal_native/              # Linux Simulation
├── test/                        # Unit Tests
└── platformio.ini               # Build Configuration
```

### Neuen Sensor hinzufügen

1. Sensor-Klasse in `lib/sensor/src/` erstellen
2. `ISensor` Interface implementieren
3. TypeID in `config.h` definieren
4. In `main.cpp` initialisieren
5. ChirpStack Codec erweitern

---

## Lizenz

MIT License - siehe [LICENSE](../LICENSE)

---

## Links

- [myIoTGrid Dokumentation](https://mysocialcare-doku.atlassian.net/wiki/spaces/myIoTGrid)
- [Heltec LoRa32 V3](https://heltec.org/project/wifi-lora-32-v3/)
- [ChirpStack](https://www.chirpstack.io/)
- [LoRa Alliance](https://lora-alliance.org/)

---

**myIoTGrid** - Open Source · Privacy First · Cloud-KI

*Made with ❤️ in Germany*
