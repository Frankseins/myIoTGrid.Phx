# myIoTGrid Sensor Firmware - Vollständige Dokumentation

**Version:** 1.9.1 (Sprint OS-01: Offline Storage)
**Stand:** Dezember 2024
**Plattform:** ESP32 (Espressif)
**Framework:** Arduino / PlatformIO

---

## Inhaltsverzeichnis

1. [Schnellstart für Benutzer](#1-schnellstart-für-benutzer)
2. [Anmeldeverfahren / Provisioning](#2-anmeldeverfahren--provisioning)
3. [Reset-Optionen](#3-reset-optionen)
4. [Debug-Optionen](#4-debug-optionen)
5. [Unterstützte Sensoren](#5-unterstützte-sensoren)
6. [Capabilities & Features](#6-capabilities--features)
7. [Konfiguration](#7-konfiguration)
8. [API-Dokumentation](#8-api-dokumentation)
9. [Architektur (für Entwickler)](#9-architektur-für-entwickler)
10. [Build & Deployment](#10-build--deployment)
11. [Troubleshooting](#11-troubleshooting)
12. [Glossar](#12-glossar)

---

# 1. Schnellstart für Benutzer

## 1.1 Was ist der myIoTGrid Sensor?

Der myIoTGrid Sensor ist ein ESP32-basiertes Gerät, das verschiedene Umweltdaten erfasst (Temperatur, Luftfeuchtigkeit, CO₂, etc.) und diese an einen lokalen Hub sendet. Der Hub kann die Daten dann zur Analyse in die Cloud übertragen.

## 1.2 Erste Inbetriebnahme (5 Minuten)

### Schritt 1: Sensor einschalten
- Verbinden Sie den Sensor mit einer 5V USB-Stromversorgung
- Die Status-LED blinkt blau → Sensor ist im Pairing-Modus

### Schritt 2: Mit der App verbinden
1. Öffnen Sie die myIoTGrid App auf Ihrem Smartphone
2. Tippen Sie auf "Neuen Sensor hinzufügen"
3. Die App findet den Sensor automatisch per Bluetooth

### Schritt 3: WiFi konfigurieren
1. Wählen Sie Ihr WiFi-Netzwerk aus der Liste
2. Geben Sie das WiFi-Passwort ein
3. Tippen Sie auf "Verbinden"

### Schritt 4: Fertig!
- Die LED leuchtet grün → Sensor ist verbunden
- Messwerte erscheinen in der App innerhalb von 60 Sekunden

## 1.3 Alternative: WPS-Verbindung (ohne App)

Falls Ihr Router WPS unterstützt:

1. **Am Sensor:** Halten Sie den Boot-Button **3 Sekunden** gedrückt
2. **Am Router:** Drücken Sie den WPS-Knopf innerhalb von 2 Minuten
3. Die LED leuchtet grün → Verbindung hergestellt

> **Hinweis:** Nach WPS-Verbindung muss der Sensor noch über die App oder das Web-Interface einem Hub zugewiesen werden.

## 1.4 LED-Status-Anzeige

| LED-Farbe/Muster | Bedeutung |
|------------------|-----------|
| Blau blinkend | Warte auf Bluetooth-Pairing |
| Blau pulsierend | Bluetooth verbunden, warte auf Konfiguration |
| Gelb blinkend | Verbinde mit WiFi... |
| Grün leuchtend | Verbunden und sendet Daten |
| Rot blinkend | Fehler (siehe Troubleshooting) |
| Orange pulsierend | Synchronisiere Offline-Daten |

---

# 2. Anmeldeverfahren / Provisioning

## 2.1 Übersicht der Verbindungsmethoden

| Methode | Voraussetzung | Schwierigkeit | Empfohlen für |
|---------|---------------|---------------|---------------|
| **BLE-Provisioning** | Smartphone mit App | Einfach | Endbenutzer |
| **WPS** | Router mit WPS-Taste | Einfach | Schnelle Einrichtung |
| **Manuell** | Serial-Zugang | Fortgeschritten | Entwickler |

## 2.2 BLE-Provisioning (Bluetooth Low Energy)

### Technische Details

**BLE-Service:**
- **Service UUID:** `4fafc201-1fb5-459e-8fcc-c5c9c331914b`
- **Stack:** NimBLE (energieeffizient)

**Characteristics:**

| UUID | Name | Rechte | Beschreibung |
|------|------|--------|--------------|
| `beb5483e-36e1-4688-b7f5-ea07361b26a8` | Registration | Read/Notify | Node-ID (z.B. `ESP32-0070078492CC`) |
| `beb5483e-36e1-4688-b7f5-ea07361b26a9` | WiFi Config | Write | SSID + Passwort (JSON) |
| `beb5483e-36e1-4688-b7f5-ea07361b26aa` | API Config | Write | Hub-URL + API-Key (JSON) |
| `beb5483e-36e1-4688-b7f5-ea07361b26ab` | Status | Read | Aktueller Status |

### Ablauf

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Sensor    │     │    App      │     │    Hub      │
└──────┬──────┘     └──────┬──────┘     └──────┬──────┘
       │                   │                   │
       │◀── BLE Scan ──────│                   │
       │                   │                   │
       │── Advertise ─────▶│                   │
       │   "ESP32-Sensor"  │                   │
       │                   │                   │
       │◀── Connect ───────│                   │
       │                   │                   │
       │── Node-ID ───────▶│                   │
       │   ESP32-007007... │                   │
       │                   │                   │
       │◀── WiFi-Config ───│                   │
       │   {ssid,password} │                   │
       │                   │                   │
       │──── WiFi Connect ─┼──────────────────▶│
       │                   │                   │
       │◀── API-Config ────│                   │
       │   {hubUrl,apiKey} │                   │
       │                   │                   │
       │─── Register ──────┼──────────────────▶│
       │                   │                   │
       │◀── Config ────────┼───────────────────│
       │                   │                   │
       ▼                   ▼                   ▼
   OPERATIONAL         Fertig!            Sensor aktiv
```

### WiFi-Config Payload

```json
{
  "ssid": "MeinWiFi",
  "password": "GeheimesPasswort123"
}
```

### API-Config Payload

```json
{
  "nodeId": "ESP32-0070078492CC",
  "apiKey": "abc123...",
  "hubUrl": "https://192.168.1.100:5001"
}
```

## 2.3 WPS (Wi-Fi Protected Setup)

### Aktivierung

1. **Button drücken:** Boot-Button (GPIO0) für **3 Sekunden** halten
2. **LED:** Blinkt schnell gelb
3. **Router:** WPS-Taste innerhalb von 2 Minuten drücken
4. **Erfolg:** LED leuchtet grün

### Technische Details

- **Typ:** PBC (Push Button Configuration)
- **Timeout:** 2 Minuten (ESP-IDF Standard)
- **Hersteller-Info:**
  - Manufacturer: `myIoTGrid`
  - Model: `Sensor-v1`
  - Device Name: `ESP32-Sensor`

### Nach WPS-Verbindung

Nach erfolgreicher WPS-Verbindung startet automatisch die **Hub-Discovery**:

```
┌─────────────┐                    ┌─────────────┐
│   Sensor    │                    │    Hub      │
└──────┬──────┘                    └──────┬──────┘
       │                                  │
       │── UDP Broadcast ────────────────▶│
       │   Port 5001                      │
       │   "MYIOTGRID_DISCOVER"           │
       │                                  │
       │◀─────────────────────────────────│
       │   "MYIOTGRID_HUB"                │
       │   {hubUrl, hubName}              │
       │                                  │
       │── HTTP POST /register ──────────▶│
       │                                  │
       │◀── {nodeId, apiKey} ─────────────│
       │                                  │
       ▼                                  ▼
   OPERATIONAL                      Sensor registriert
```

## 2.4 Node-ID Format

Die Node-ID wird automatisch aus der WiFi-MAC-Adresse generiert:

```
Format: ESP32-{MAC-Adresse ohne Doppelpunkte}
Beispiel: ESP32-0070078492CC
```

> **Warum WiFi-MAC statt BLE-MAC?**
> Die WiFi-MAC ist konsistent und ändert sich nicht, während die BLE-MAC bei manchen ESP32-Versionen variieren kann.

## 2.5 Re-Provisioning Mode

Falls die WiFi-Verbindung **3 Mal** fehlschlägt, wechselt der Sensor automatisch in den **Re-Provisioning-Mode**:

- **BLE-Name:** `ESP32-Sensor-SETUP` (mit "-SETUP" Suffix)
- **Parallel:** WiFi-Retry alle 30 Sekunden
- **Ziel:** Neue WiFi-Credentials empfangen

```
┌──────────────────────────────────────────────────────────┐
│                    RE-PROVISIONING MODE                  │
├──────────────────────────────────────────────────────────┤
│                                                          │
│   BLE Advertising aktiv     WiFi Retry alle 30s         │
│   "ESP32-Sensor-SETUP"      (mit alten Credentials)      │
│         │                           │                    │
│         ▼                           ▼                    │
│   ┌─────────────┐           ┌─────────────┐             │
│   │ Neue Config │           │ WiFi OK?    │             │
│   │ empfangen   │           │             │             │
│   └──────┬──────┘           └──────┬──────┘             │
│          │                         │                     │
│          └────────┬────────────────┘                     │
│                   ▼                                      │
│            CONFIGURED → OPERATIONAL                      │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

---

# 3. Reset-Optionen

## 3.1 Übersicht

| Reset-Typ | Auslöser | Was wird gelöscht | Dauer |
|-----------|----------|-------------------|-------|
| **Soft Reset** | Stromunterbrechung | Nichts | Sofort |
| **WiFi Reset** | Programmatisch | Nur WiFi-Daten | Sofort |
| **Factory Reset** | Button 10s | Alles (NVS) | 3s |

## 3.2 Factory Reset (Werkseinstellungen)

### Durchführung

1. **Button halten:** Boot-Button (GPIO0) für **10 Sekunden** gedrückt halten
2. **Feedback:** Serial-Monitor zeigt Countdown
3. **LED:** Blinkt rot während des Resets
4. **Neustart:** Automatisch nach dem Reset

### Was wird gelöscht

| Daten | Gelöscht? |
|-------|-----------|
| WiFi-Konfiguration (SSID, Passwort) | ✅ Ja |
| Node-ID und API-Key | ✅ Ja |
| Hub-URL | ✅ Ja |
| Debug-Einstellungen | ✅ Ja |
| Offline-Daten auf SD-Karte | ❌ Nein |
| Firmware | ❌ Nein |

### Serial-Output beim Factory Reset

```
[Button] Boot button pressed - hold 10s for Factory Reset
[Button] Factory Reset in 9...
[Button] Factory Reset in 8...
[Button] Factory Reset in 7...
...
[Button] Factory Reset in 1...
[Config] Clearing all NVS data...
[Config] NVS cleared successfully
[System] Restarting...
```

## 3.3 WiFi Reset (nur WiFi-Daten)

Nur programmatisch möglich (nicht über Button):

```cpp
ConfigManager::clearWiFiConfig();
```

Löscht nur:
- WiFi-SSID
- WiFi-Passwort

Behält:
- Node-ID
- API-Key
- Hub-URL

## 3.4 Button-Kombinationen Übersicht

| Aktion | Button-Dauer | Beschreibung |
|--------|--------------|--------------|
| Keine Aktion | < 1 Sekunde | Nichts passiert |
| **WPS-Modus** | **3 Sekunden** | WPS-Pairing starten |
| **Factory Reset** | **10 Sekunden** | Alle Daten löschen |

### Timing-Diagramm

```
Button-Druck:
├─────────────────────────────────────────────────────────────▶ Zeit
│
0s          1s          3s                    10s
│           │           │                     │
│  Nichts   │   Warte   │      WPS-Mode       │  Factory Reset
│           │           │      aktiviert      │  ausgeführt
└───────────┴───────────┴─────────────────────┴───────────────
```

## 3.5 Sync-Button (GPIO4)

Zusätzlich zum Boot-Button gibt es einen **Sync-Button** für Offline-Daten:

| Aktion | Button-Dauer | Beschreibung |
|--------|--------------|--------------|
| **Normaler Sync** | < 1 Sekunde | Pending-Daten synchronisieren |
| **Force Sync** | ≥ 3 Sekunden | Alle Daten neu synchronisieren |

---

# 4. Debug-Optionen

## 4.1 Debug-Level

Der Sensor unterstützt 3 Debug-Level, die persistent in NVS gespeichert werden:

| Level | Name | Beschreibung | Empfohlen für |
|-------|------|--------------|---------------|
| 0 | **PRODUCTION** | Minimal, nur kritische Fehler | Endbenutzer |
| 1 | **NORMAL** | Standard-Logging (Default) | Normaler Betrieb |
| 2 | **DEBUG** | Verbose, alle Details | Entwicklung/Troubleshooting |

## 4.2 Log-Kategorien

Logs können nach Kategorien gefiltert werden (Bitmask):

| ID | Kategorie | Bit | Beschreibung |
|----|-----------|-----|--------------|
| 0 | **SYSTEM** | 0x01 | Boot, State Machine, Allgemein |
| 1 | **HARDWARE** | 0x02 | I2C, UART, GPIO, Hardware-Scanning |
| 2 | **NETWORK** | 0x04 | WiFi, BLE, Konnektivität |
| 3 | **SENSOR** | 0x08 | Sensor-Readings, Messungen |
| 4 | **GPS** | 0x10 | GPS/GNSS-spezifisch |
| 5 | **API** | 0x20 | HTTP API, Hub-Kommunikation |
| 6 | **STORAGE** | 0x40 | SD-Karte, NVS, Persistierung |
| 7 | **ERROR** | 0x80 | Fehler (immer geloggt) |

### Beispiel: Kategorien kombinieren

```cpp
// Nur SENSOR und API aktivieren
uint8_t categories = LOG_SENSOR | LOG_API;  // 0x08 | 0x20 = 0x28

// Alle Kategorien aktivieren
uint8_t categories = 0xFF;
```

## 4.3 Serial Monitor

### Verbindung herstellen

1. **USB-Kabel** an Computer anschließen
2. **Seriellen Port** öffnen (z.B. `/dev/ttyUSB0` oder `COM3`)
3. **Baud Rate:** `115200`

### Tools

- **PlatformIO:** `pio device monitor`
- **Arduino IDE:** Serial Monitor
- **Terminal:** `screen /dev/ttyUSB0 115200`

### Log-Format

```
[Kategorie] Nachricht
```

### Beispiel-Output

```
[System] ===============================================
[System] myIoTGrid Sensor Firmware v1.9.1
[System] Hardware: ESP32
[System] Serial: ESP32-0070078492CC
[System] ===============================================

[Config] Loading configuration from NVS...
[Config] WiFi SSID: MeinWiFi
[Config] Hub URL: https://192.168.1.100:5001
[Config] Configuration loaded successfully

[Network] Connecting to WiFi...
[Network] WiFi connected! IP: 192.168.1.42

[Hardware] Scanning I2C bus...
[Hardware] Found device at 0x76: BME280
[Hardware] Found device at 0x23: BH1750

[Sensor] Reading BME280...
[Sensor] Temperature: 21.5°C
[Sensor] Humidity: 45.2%
[Sensor] Pressure: 1013.25 hPa

[API] Sending readings to hub...
[API] Response: 200 OK
```

## 4.4 Remote Logging

Logs können an den Hub gesendet werden:

### Aktivierung

1. Im Hub-Dashboard: Sensor auswählen
2. "Remote Logging" aktivieren
3. Debug-Level wählen
4. Kategorien auswählen

### Log-Upload

- **Intervall:** Konfigurierbar (Standard: 60 Sekunden)
- **Batch-Size:** Bis zu 100 Logs pro Upload
- **Fallback:** Bei Netzwerk-Fehler: SD-Karte

### Log-Entry-Format (JSON)

```json
{
  "timestamp": 1733150400,
  "level": 2,
  "category": 3,
  "message": "Temperature reading: 21.5°C",
  "stackTrace": null
}
```

## 4.5 SD-Karten-Logging

Bei verfügbarer SD-Karte werden Logs automatisch gespeichert:

### Verzeichnis-Struktur

```
/iotgrid/
├── logs/
│   ├── 2024-12-01.log
│   ├── 2024-12-02.log
│   └── ...
├── readings/
│   └── pending/
└── config.json
```

### Log-Datei-Format

```
2024-12-09T15:30:00 [SENSOR] Temperature: 21.5°C
2024-12-09T15:30:00 [SENSOR] Humidity: 45.2%
2024-12-09T15:30:01 [API] Sending readings...
2024-12-09T15:30:01 [API] Response: 200 OK
```

## 4.6 Hardware Validator

Nach dem Boot sendet der Sensor einen **Hardware-Status-Report** an den Hub:

```json
{
  "serialNumber": "ESP32-0070078492CC",
  "firmwareVersion": "1.9.1",
  "hardwareType": "ESP32",
  "detectedDevices": [
    {"type": "I2C", "address": "0x76", "name": "BME280"},
    {"type": "I2C", "address": "0x23", "name": "BH1750"},
    {"type": "OneWire", "address": "28-00000ABCDEF", "name": "DS18B20"}
  ],
  "storage": {
    "sdCardAvailable": true,
    "sdCardSize": 32000000000,
    "sdCardFree": 30000000000
  },
  "busStatus": {
    "i2c": "OK",
    "spi": "OK",
    "uart": "OK"
  }
}
```

## 4.7 Debug-Makros für Entwickler

```cpp
// System-Log
DBG_SYSTEM("Boot completed in %d ms", bootTime);

// Hardware-Log
DBG_HARDWARE("Found I2C device at 0x%02X", address);

// Network-Log
DBG_NETWORK("WiFi RSSI: %d dBm", rssi);

// Sensor-Log
DBG_SENSOR("Temperature: %.1f°C", temp);

// GPS-Log
DBG_GPS("Position: %.6f, %.6f", lat, lon);

// API-Log
DBG_API("HTTP Response: %d", statusCode);

// Storage-Log
DBG_STORAGE("SD Card: %d MB free", freeSpace);

// Error-Log (immer geloggt)
DBG_ERROR("Failed to read sensor: %s", errorMsg);
```

---

# 5. Unterstützte Sensoren

## 5.1 Sensor-Übersicht nach Kategorie

### Temperatur & Luftfeuchte

| Sensor | Interface | Adressen | Messgrößen | Library |
|--------|-----------|----------|------------|---------|
| **BME280** | I2C | 0x76, 0x77 | Temp, Feuchte, Druck | Adafruit BME280 |
| **BME680** | I2C | 0x76, 0x77 | Temp, Feuchte, Druck, VOC | Adafruit BME680 |
| **SHT31** | I2C | 0x44, 0x45 | Temp, Feuchte | ClosedCube SHT31D |
| **DHT22** | Digital | GPIO | Temp, Feuchte | Adafruit DHT |
| **DHT11** | Digital | GPIO | Temp, Feuchte | Adafruit DHT |
| **DS18B20** | 1-Wire | GPIO | Temperatur | DallasTemperature |

### CO₂ & Luftqualität

| Sensor | Interface | Adressen | Messgrößen | Library |
|--------|-----------|----------|------------|---------|
| **SCD30** | I2C | 0x61 | CO₂, Temp, Feuchte | SparkFun SCD30 |
| **SCD40/41** | I2C | 0x62 | CO₂, Temp, Feuchte | Sensirion I2C SCD4x |
| **CCS811** | I2C | 0x5A, 0x5B | eCO₂, TVOC | Adafruit CCS811 |
| **SGP30** | I2C | 0x58 | eCO₂, TVOC | Adafruit SGP30 |

### Licht & UV

| Sensor | Interface | Adressen | Messgrößen | Library |
|--------|-----------|----------|------------|---------|
| **BH1750** | I2C | 0x23, 0x5C | Helligkeit (lux) | claws/BH1750 |
| **TSL2561** | I2C | 0x29, 0x39, 0x49 | Helligkeit (lux) | Adafruit TSL2561 |

### Distanz

| Sensor | Interface | Adressen | Messgrößen | Library |
|--------|-----------|----------|------------|---------|
| **VL53L0X** | I2C | 0x29 | Distanz (mm) | Pololu VL53L0X |
| **HC-SR04** | Digital | GPIO | Distanz (cm) | Integriert |

### Analog-Sensoren

| Sensor | Interface | Messgrößen |
|--------|-----------|------------|
| **Kapazitive Bodenfeuchtigkeit** | Analog | Feuchte (%) |
| **Batteriespannung** | Analog | Spannung (V) |

### GPS/GNSS

| Sensor | Interface | Messgrößen | Library |
|--------|-----------|------------|---------|
| **GPS-Module (diverse)** | UART | Position, Geschwindigkeit | TinyGPS+ |

## 5.2 I2C-Adressen-Tabelle

```
┌─────────────────────────────────────────────────────────────┐
│                    I2C-Adressbereich                        │
├──────────┬──────────────────────────────────────────────────┤
│ 0x10     │ VEML6075 (UV)                                    │
│ 0x23     │ BH1750 (Licht)                                   │
│ 0x29     │ TSL2561 (Licht), VL53L0X (Distanz)              │
│ 0x38     │ VEML6070 (UV)                                    │
│ 0x39     │ TSL2561 (Licht)                                  │
│ 0x3C-3D  │ SSD1306 OLED (Display)                          │
│ 0x40     │ HDC1080 (Temp/Feuchte)                          │
│ 0x44-45  │ SHT31 (Temp/Feuchte)                            │
│ 0x48-4B  │ ADS1115/ADS1015 (ADC)                           │
│ 0x49     │ TSL2561 (Licht)                                  │
│ 0x50-51  │ AT24C32 EEPROM                                   │
│ 0x52     │ VL53L0X (Distanz)                                │
│ 0x58     │ SGP30 (CO₂/TVOC)                                 │
│ 0x5A-5B  │ CCS811 (CO₂/TVOC)                                │
│ 0x5C     │ BH1750 (Licht, alternative Adresse)              │
│ 0x60     │ MPL3115A2 (Druck)                                │
│ 0x61     │ SCD30 (CO₂)                                      │
│ 0x62     │ SCD40/SCD41 (CO₂)                                │
│ 0x68     │ DS3231/DS1307 RTC                                │
│ 0x76-77  │ BME280/BME680/BMP280 (Umwelt)                   │
└──────────┴──────────────────────────────────────────────────┘
```

## 5.3 One-Wire Konfiguration

### Unterstützte Pins für DS18B20

```
GPIO 4, 5, 13, 14, 15, 16, 17, 18, 19, 23, 25, 26, 27, 32, 33
```

### Verkabelung

```
┌─────────────┐
│   DS18B20   │
├─────────────┤
│ VCC (rot)   ├──── 3.3V
│ DATA (gelb) ├──── GPIO + 4.7kΩ Pull-up zu 3.3V
│ GND (schwarz)├──── GND
└─────────────┘
```

## 5.4 UART-Sensoren

### GPS-Module

```
┌─────────────┐     ┌─────────────┐
│   ESP32     │     │  GPS-Modul  │
├─────────────┤     ├─────────────┤
│ TX (GPIO17) ├────▶│ RX          │
│ RX (GPIO16) │◀────│ TX          │
│ 3.3V        ├────▶│ VCC         │
│ GND         ├────▶│ GND         │
└─────────────┘     └─────────────┘
```

### Ultraschall SR04M2

```
Baud Rate: 9600 (Standard)
Protokoll: ASCII
```

## 5.5 Sensor-Kalibrierung

Jeder Sensor kann individuell kalibriert werden:

| Parameter | Beschreibung | Formel |
|-----------|--------------|--------|
| `offsetCorrection` | Offset addieren | `Wert + Offset` |
| `gainCorrection` | Multiplikator | `Wert × Gain` |

### Beispiel

```json
{
  "sensorCode": "bme280_temp",
  "offsetCorrection": -0.5,  // Sensor zeigt 0.5°C zu viel
  "gainCorrection": 1.0      // Keine Skalierung
}
```

### Kalibrierte Messung

```
Roher Wert:       22.0°C
Nach Kalibrierung: 22.0 × 1.0 + (-0.5) = 21.5°C
```

## 5.6 Automatische Hardware-Erkennung

Beim Boot scannt der Sensor automatisch:

1. **I2C-Bus** (0x00 - 0x7F)
2. **One-Wire-Pins** (konfigurierte GPIOs)
3. **UART-Ports** (wenn konfiguriert)

```
[Hardware] Scanning I2C bus...
[Hardware] Found device at 0x76 - Identified as: BME280
[Hardware] Found device at 0x23 - Identified as: BH1750
[Hardware] Found device at 0x61 - Identified as: SCD30
[Hardware] I2C scan complete: 3 devices found

[Hardware] Scanning OneWire pins...
[Hardware] Found DS18B20 on GPIO4: 28-00000ABCDEF
[Hardware] OneWire scan complete: 1 device found
```

---

# 6. Capabilities & Features

## 6.1 Feature-Übersicht

| Feature | Status | Beschreibung |
|---------|--------|--------------|
| BLE-Provisioning | ✅ Aktiv | Bluetooth-basierte Einrichtung |
| WPS | ✅ Aktiv | WiFi Protected Setup |
| Hub-Discovery | ✅ Aktiv | Automatische Hub-Suche per UDP |
| Offline-Storage | ✅ Aktiv | Lokale Datenspeicherung |
| OTA-Updates | ✅ Aktiv | Over-the-Air Firmware-Updates |
| Remote-Debug | ✅ Aktiv | Ferndiagnose |
| Deep Sleep | 🔄 Geplant | Energiesparmodus |
| MQTT | 🔄 Geplant | Alternative zu HTTP |

## 6.2 Offline-Storage (Sprint OS-01)

### Übersicht

Der Sensor kann Messwerte lokal speichern, wenn keine Netzwerkverbindung besteht.

### Speicher-Modi

| Modus | ID | Beschreibung |
|-------|----|--------------|
| **RemoteOnly** | 0 | Nur HTTP, keine lokale Speicherung |
| **LocalAndRemote** | 1 | Lokal + Cloud-Sync (Standard) |
| **LocalOnly** | 2 | Nur lokale Speicherung |
| **LocalAutoSync** | 3 | Lokal mit automatischem Sync |

### SD-Karten-Konfiguration

| Pin | Funktion | GPIO |
|-----|----------|------|
| MISO | Data Out | 19 |
| MOSI | Data In | 23 |
| SCK | Clock | 18 |
| CS | Chip Select | 5 |

### Verzeichnis-Struktur

```
/iotgrid/
├── readings/
│   ├── 2024-12-09.csv         # Tägliche Readings
│   └── pending/
│       ├── batch_001.json     # Nicht synchronisierte Daten
│       └── batch_002.json
├── logs/
│   └── 2024-12-09.log
├── config.json                 # Lokale Konfiguration
└── sync_status.json            # Synchronisierungs-Status
```

### Reading-CSV-Format

```csv
timestamp,sensorType,value,unit,endpointId,synced
1733150400,temperature,21.5,°C,1,1
1733150460,humidity,45.2,%,2,0
1733150520,co2,450,ppm,3,0
```

### Sync-Manager

```
┌─────────────────────────────────────────────────────────────┐
│                      SYNC MANAGER                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   Strategien:                                               │
│   ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│   │  Scheduled  │  │   Manual    │  │  Automatic  │        │
│   │ (Intervall) │  │  (Button)   │  │(WiFi-Event) │        │
│   └──────┬──────┘  └──────┬──────┘  └──────┬──────┘        │
│          │                │                │                │
│          └────────────────┼────────────────┘                │
│                           ▼                                 │
│                    ┌─────────────┐                          │
│                    │ Sync Start  │                          │
│                    └──────┬──────┘                          │
│                           │                                 │
│                    ┌──────▼──────┐                          │
│                    │ Load Batch  │                          │
│                    └──────┬──────┘                          │
│                           │                                 │
│                    ┌──────▼──────┐                          │
│                    │ HTTP POST   │──▶ Hub                   │
│                    └──────┬──────┘                          │
│                           │                                 │
│            ┌──────────────┼──────────────┐                  │
│            │              │              │                  │
│      ┌─────▼─────┐  ┌─────▼─────┐  ┌─────▼─────┐           │
│      │  Success  │  │   Retry   │  │   Error   │           │
│      │  Delete   │  │  Backoff  │  │   Keep    │           │
│      └───────────┘  └───────────┘  └───────────┘           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Sync-Button (GPIO4)

| Aktion | Dauer | LED-Feedback |
|--------|-------|--------------|
| Normal Sync | < 1s | Pulsing orange |
| Force Sync | ≥ 3s | Schnell blinkend |

### Sync-Status-LED (GPIO2)

| Zustand | LED-Muster |
|---------|------------|
| Idle | Aus |
| Syncing | Pulsierend |
| Erfolgreich | Kurz grün |
| Fehler | Rot blinkend |

## 6.3 State Machine

### Zustände

```
┌─────────────────────────────────────────────────────────────┐
│                     STATE MACHINE                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌───────────────┐                                          │
│  │ UNCONFIGURED  │◀───── Boot ohne Config                   │
│  └───────┬───────┘                                          │
│          │ BLE_PAIR_START                                   │
│          ▼                                                  │
│  ┌───────────────┐                                          │
│  │    PAIRING    │◀───── BLE aktiv, warte auf Config        │
│  └───────┬───────┘                                          │
│          │ BLE_CONFIG_RECEIVED                              │
│          ▼                                                  │
│  ┌───────────────┐                                          │
│  │  CONFIGURED   │◀───── WiFi konfiguriert                  │
│  └───────┬───────┘                                          │
│          │                                                  │
│    ┌─────┴─────┐                                            │
│    │           │                                            │
│    ▼           ▼                                            │
│ WIFI_OK    WIFI_FAIL ───▶ 3x ───▶ RE_PAIRING               │
│    │                              │                         │
│    ▼                              │ NEW_WIFI_RECEIVED       │
│  ┌───────────────┐                │                         │
│  │  OPERATIONAL  │◀───────────────┘                         │
│  └───────┬───────┘                                          │
│          │                                                  │
│          │ ERROR                                            │
│          ▼                                                  │
│  ┌───────────────┐                                          │
│  │     ERROR     │───── Recovery ───▶ CONFIGURED           │
│  └───────────────┘                                          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### State-Beschreibungen

| State | Beschreibung | Aktionen |
|-------|--------------|----------|
| `UNCONFIGURED` | Keine Konfiguration | BLE starten, auf Config warten |
| `PAIRING` | BLE aktiv | WiFi-Credentials empfangen |
| `CONFIGURED` | WiFi konfiguriert | Mit WiFi verbinden |
| `OPERATIONAL` | Voll funktional | Sensoren lesen, Daten senden |
| `ERROR` | Fehler aufgetreten | Recovery versuchen |
| `RE_PAIRING` | WiFi fehlgeschlagen | BLE + WiFi parallel |

## 6.4 Protokolle

### HTTP/HTTPS

| Parameter | Wert |
|-----------|------|
| Standard-Port | 5001 (HTTPS) |
| Fallback-Port | 5000 (HTTP) |
| Timeout | 10 Sekunden |
| Retry-Versuche | 3 |
| JSON-Library | ArduinoJson 7.2.1 |

### Reading-Payload

```json
{
  "serialNumber": "ESP32-0070078492CC",
  "readings": [
    {
      "sensorType": "temperature",
      "value": 21.5,
      "unit": "°C",
      "endpointId": 1,
      "timestamp": 1733150400
    },
    {
      "sensorType": "humidity",
      "value": 45.2,
      "unit": "%",
      "endpointId": 2,
      "timestamp": 1733150400
    }
  ]
}
```

## 6.5 Sensor-Simulation

Für Entwicklung und Testing können Sensoren simuliert werden:

### Aktivierung

```ini
# platformio.ini
build_flags = -DSIMULATE_SENSORS=1
```

### Simulierte Werte

| Sensor | Base | Amplitude | Variation |
|--------|------|-----------|-----------|
| Temperatur | 20.0°C | ±5.0°C | Tageszeit-Sinus |
| Luftfeuchte | 50% | ±20% | Tageszeit-Sinus |
| CO₂ | 450 ppm | ±200 ppm | Zufällig |
| Druck | 1013 hPa | ±10 hPa | Zufällig |

### Simulations-Profile

```cpp
enum SimulationProfile {
    NORMAL,      // Normale Wohnraum-Bedingungen
    COLD,        // Kalte Umgebung (Winter)
    HOT,         // Warme Umgebung (Sommer)
    HUMID,       // Hohe Luftfeuchtigkeit
    DRY,         // Niedrige Luftfeuchtigkeit
    HIGH_CO2     // Schlechte Belüftung
};
```

---

# 7. Konfiguration

## 7.1 config.h - Compile-Time Konfiguration

```cpp
// ============================================================
// FIRMWARE-INFORMATIONEN
// ============================================================
#define FIRMWARE_VERSION "1.9.1"

// ============================================================
// HUB-VERBINDUNG
// ============================================================
#define DEFAULT_HUB_HOST "localhost"
#define DEFAULT_HUB_PORT 5001
#define DEFAULT_HUB_PROTOCOL "https"

// ============================================================
// TIMING
// ============================================================
#define DEFAULT_INTERVAL_SECONDS 60      // Sensor-Reading-Intervall
#define HTTP_TIMEOUT_MS 10000            // HTTP-Request-Timeout
#define HTTP_RETRY_COUNT 3               // HTTP-Wiederholungen
#define HEARTBEAT_INTERVAL_MS 60000      // Heartbeat an Hub

// ============================================================
// DISCOVERY
// ============================================================
#define DISCOVERY_PORT 5001
#define DISCOVERY_TIMEOUT_MS 5000
#define DISCOVERY_RETRY_COUNT 3

// ============================================================
// BUTTON-TIMING
// ============================================================
#define BOOT_BUTTON_PIN 0                // GPIO0
#define WPS_BUTTON_HOLD_MS 3000          // 3 Sekunden für WPS
#define FACTORY_RESET_HOLD_MS 10000      // 10 Sekunden für Reset

// ============================================================
// SD-KARTE (Offline-Storage)
// ============================================================
#define SD_MISO_PIN 19
#define SD_MOSI_PIN 23
#define SD_SCK_PIN 18
#define SD_CS_PIN 5
#define SD_MIN_FREE_SPACE 1048576        // 1 MB

// ============================================================
// SYNC-BUTTON & LED
// ============================================================
#define SYNC_BUTTON_GPIO 4
#define SYNC_LED_GPIO 2
#define SYNC_BUTTON_DEBOUNCE_MS 50
#define SYNC_BUTTON_LONG_PRESS_MS 3000

// ============================================================
// I2C (Standard-Pins)
// ============================================================
#define I2C_SDA_PIN 21
#define I2C_SCL_PIN 22
#define I2C_FREQUENCY 100000             // 100 kHz
```

## 7.2 Environment Variables

Für Docker/Native-Builds:

| Variable | Typ | Standard | Beschreibung |
|----------|-----|----------|--------------|
| `HUB_HOST` | String | `localhost` | Hub-Hostname |
| `HUB_PORT` | Int | `5001` | Hub-Port |
| `HUB_PROTOCOL` | String | `https` | `http` oder `https` |
| `HUB_INSECURE` | Bool | `false` | SSL-Zertifikat ignorieren |
| `WIFI_SSID` | String | - | WiFi-Netzwerk |
| `WIFI_PASSWORD` | String | - | WiFi-Passwort |
| `DISCOVERY_ENABLED` | Bool | `true` | UDP-Discovery aktiv |

## 7.3 NVS-Speicher (Non-Volatile Storage)

Persistente Einstellungen im ESP32-Flash:

### Namespace: "config"

| Schlüssel | Typ | Beschreibung |
|-----------|-----|--------------|
| `node_id` | String | Node-ID (GUID vom Hub) |
| `api_key` | String | API-Key für Authentifizierung |
| `wifi_ssid` | String | WiFi-Netzwerkname |
| `wifi_pass` | String | WiFi-Passwort |
| `hub_url` | String | Hub-API-URL |
| `configured` | Bool | Ist konfiguriert? |

### Namespace: "debug"

| Schlüssel | Typ | Standard | Beschreibung |
|-----------|-----|----------|--------------|
| `level` | UInt8 | 1 | Debug-Level (0-2) |
| `remote` | Bool | false | Remote Logging aktiv |
| `cats` | UInt8 | 0xFF | Aktive Log-Kategorien |

## 7.4 Hub-Konfiguration (Runtime)

Der Sensor lädt seine Konfiguration vom Hub:

### Endpoint

```
GET /api/Nodes/{serialNumber}/configuration
```

### Response

```json
{
  "success": true,
  "nodeId": "00000000-0000-0000-0000-000000000001",
  "serialNumber": "ESP32-0070078492CC",
  "name": "Wohnzimmer Sensor",
  "isSimulation": false,
  "defaultIntervalSeconds": 60,
  "storageMode": 1,
  "sensors": [
    {
      "endpointId": 1,
      "sensorCode": "bme280_temp",
      "sensorName": "BME280 Temperature",
      "icon": "thermostat",
      "color": "#FF5722",
      "isActive": true,
      "intervalSeconds": 60,
      "i2cAddress": "0x76",
      "sdaPin": 21,
      "sclPin": 22,
      "offsetCorrection": 0.0,
      "gainCorrection": 1.0,
      "capabilities": [
        {
          "measurementType": "temperature",
          "displayName": "Temperatur",
          "unit": "°C"
        }
      ]
    }
  ],
  "configurationTimestamp": 1733150400
}
```

### Sensor-Assignment-Felder

| Feld | Typ | Beschreibung |
|------|-----|--------------|
| `endpointId` | Int | Eindeutige ID für diesen Sensor |
| `sensorCode` | String | Sensor-Typ-Code |
| `i2cAddress` | String | I2C-Adresse (hex, z.B. "0x76") |
| `sdaPin` | Int | SDA-Pin (für alternative I2C-Busse) |
| `sclPin` | Int | SCL-Pin (für alternative I2C-Busse) |
| `oneWirePin` | Int | OneWire-Daten-Pin |
| `analogPin` | Int | Analog-Eingang |
| `digitalPin` | Int | Digital-Eingang |
| `triggerPin` | Int | Ultraschall-Trigger |
| `echoPin` | Int | Ultraschall-Echo |
| `baudRate` | Int | UART Baud Rate |
| `offsetCorrection` | Float | Offset-Kalibrierung |
| `gainCorrection` | Float | Gain-Kalibrierung |

---

# 8. API-Dokumentation

## 8.1 Endpunkt-Übersicht

| Methode | Endpoint | Beschreibung |
|---------|----------|--------------|
| POST | `/api/Nodes/register` | Sensor registrieren |
| GET | `/api/Nodes/{serial}/configuration` | Konfiguration abrufen |
| POST | `/api/readings` | Messwerte senden |
| POST | `/api/readings/heartbeat` | Heartbeat senden |
| POST | `/api/Nodes/{serial}/hardware-status` | Hardware-Status melden |
| GET | `/api/Nodes/{serial}/debug-configuration` | Debug-Config abrufen |

## 8.2 Sensor-Registrierung

### Request

```http
POST /api/Nodes/register
Content-Type: application/json

{
  "serialNumber": "ESP32-0070078492CC",
  "firmwareVersion": "1.9.1",
  "hardwareType": "ESP32",
  "capabilities": [
    "temperature",
    "humidity",
    "pressure",
    "co2"
  ]
}
```

### Response (201 Created)

```json
{
  "nodeId": "00000000-0000-0000-0000-000000000001",
  "serialNumber": "ESP32-0070078492CC",
  "name": "Neuer Sensor",
  "location": null,
  "intervalSeconds": 60,
  "isNewNode": true,
  "message": "Node registered successfully",
  "connection": {
    "endpoint": "wss://hub.local:5001/signalr"
  }
}
```

## 8.3 Messwerte senden

### Request

```http
POST /api/readings
Content-Type: application/json

{
  "serialNumber": "ESP32-0070078492CC",
  "readings": [
    {
      "sensorType": "temperature",
      "value": 21.5,
      "unit": "°C",
      "endpointId": 1,
      "timestamp": 1733150400
    },
    {
      "sensorType": "humidity",
      "value": 45.2,
      "unit": "%",
      "endpointId": 2,
      "timestamp": 1733150400
    }
  ]
}
```

### Response (200 OK)

```json
{
  "success": true,
  "processed": 2,
  "serverTime": 1733150401
}
```

## 8.4 Heartbeat

### Request

```http
POST /api/readings/heartbeat
Content-Type: application/json

{
  "serialNumber": "ESP32-0070078492CC",
  "firmwareVersion": "1.9.1",
  "batteryLevel": 95,
  "wifiRssi": -45,
  "freeHeap": 150000
}
```

### Response

```json
{
  "success": true,
  "serverTime": 1733150400,
  "nextHeartbeatSeconds": 60,
  "configurationChanged": false
}
```

## 8.5 Hardware-Status

### Request

```http
POST /api/Nodes/ESP32-0070078492CC/hardware-status
Content-Type: application/json

{
  "serialNumber": "ESP32-0070078492CC",
  "firmwareVersion": "1.9.1",
  "hardwareType": "ESP32",
  "detectedDevices": [
    {
      "type": "I2C",
      "address": "0x76",
      "name": "BME280"
    },
    {
      "type": "I2C",
      "address": "0x23",
      "name": "BH1750"
    },
    {
      "type": "OneWire",
      "address": "28-00000ABCDEF",
      "name": "DS18B20"
    }
  ],
  "storage": {
    "sdCardAvailable": true,
    "sdCardSize": 32000000000,
    "sdCardFree": 30000000000,
    "nvsAvailable": true
  },
  "busStatus": {
    "i2c": "OK",
    "spi": "OK",
    "uart": "OK"
  }
}
```

## 8.6 Discovery-Protokoll (UDP)

### Discovery Request

```
Port: UDP 5001
Broadcast: 255.255.255.255

Payload:
{
  "type": "MYIOTGRID_DISCOVER",
  "serial": "ESP32-0070078492CC",
  "firmwareVersion": "1.9.1",
  "hardwareType": "ESP32"
}
```

### Discovery Response

```json
{
  "type": "MYIOTGRID_HUB",
  "hubId": "hub-001",
  "hubName": "Mein Hub",
  "hubUrl": "https://192.168.1.100:5001",
  "apiVersion": "1.0"
}
```

---

# 9. Architektur (für Entwickler)

## 9.1 Projektstruktur

```
myIoTGrid.Sensor/
├── src/
│   └── main.cpp                 # Hauptprogramm
├── include/
│   └── config.h                 # Compile-Time-Konfiguration
├── lib/
│   ├── controller/
│   │   ├── config_manager.h     # NVS-Speicherverwaltung
│   │   └── node_controller.h    # State Machine
│   ├── connection/
│   │   └── http_connection.h    # HTTP-Client
│   ├── sensor/
│   │   ├── sensor_factory.h     # Sensor-Erstellung
│   │   ├── sensor_interface.h   # Sensor-Basisklasse
│   │   └── simulated_sensor.h   # Simulation
│   ├── data/
│   │   ├── data_types.h         # Datenstrukturen
│   │   └── json_serializer.h    # JSON-Serialisierung
│   ├── hal_esp32/
│   │   └── hal_esp32.cpp        # ESP32-Hardware-Abstraktion
│   └── hal_native/
│       ├── Arduino.h            # Arduino-API für Native
│       └── hal_native.cpp       # Native-Hardware-Abstraktion
├── test/
│   └── ...                      # Unit-Tests
├── docker/
│   └── Dockerfile               # Docker-Build für Simulator
└── platformio.ini               # PlatformIO-Konfiguration
```

## 9.2 Architektur-Diagramm

```
┌─────────────────────────────────────────────────────────────────┐
│                         MAIN.CPP                                │
│  • Boot-Sequenz         • Button-Handler                        │
│  • State Machine        • Sensor Reading Loop                   │
└──────────────────────────────┬──────────────────────────────────┘
                               │
       ┌───────────────────────┼───────────────────────┐
       │                       │                       │
       ▼                       ▼                       ▼
┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐
│  STATE MACHINE  │   │   API CLIENT    │   │ SENSOR READER   │
│  • States       │   │  • HTTP/HTTPS   │   │  • I2C Scan     │
│  • Transitions  │   │  • JSON Parse   │   │  • Read Values  │
│  • Callbacks    │   │  • Error Handle │   │  • Calibration  │
└────────┬────────┘   └─────────────────┘   └─────────────────┘
         │
         ├──▶ BLE SERVICE          CONFIG MANAGER
         │    • NimBLE Server       • NVS Storage
         │    • Characteristics     • Load/Save
         │
         ├──▶ WPS MANAGER          DISCOVERY CLIENT
         │    • Push Button         • UDP Broadcast
         │    • ESP-IDF             • Hub Finding
         │
         └──▶ WIFI MANAGER         DEBUG MANAGER
              • Connect/Reconnect   • Log Levels
              • RSSI Reading        • Categories

┌─────────────────────────────────────────────────────────────────┐
│                    STORAGE SYSTEM                               │
├─────────────────────────────────────────────────────────────────┤
│  SD MANAGER        │  READING STORAGE  │  SYNC MANAGER         │
│  • SPI Init        │  • CSV Write      │  • Batch Upload       │
│  • File System     │  • Pending Queue  │  • Retry Logic        │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                 HARDWARE ABSTRACTION LAYER                      │
├─────────────────────────────────────────────────────────────────┤
│  hal_esp32.cpp (ESP32)    │    hal_native.cpp (Linux/Native)   │
└─────────────────────────────────────────────────────────────────┘
```

## 9.3 State Machine Details

### States

```cpp
enum class NodeState {
    UNCONFIGURED,   // Keine Konfiguration vorhanden
    PAIRING,        // BLE-Pairing aktiv
    CONFIGURED,     // WiFi konfiguriert, verbinde...
    OPERATIONAL,    // Voll funktional
    ERROR,          // Fehler, Recovery läuft
    RE_PAIRING      // WiFi fehlgeschlagen, BLE neu aktiv
};
```

### Events

```cpp
enum class NodeEvent {
    BOOT,
    CONFIG_FOUND,
    NO_CONFIG,
    BLE_PAIR_START,
    BLE_CONFIG_RECEIVED,
    WIFI_CONNECTED,
    WIFI_FAILED,
    API_VALIDATED,
    API_FAILED,
    RESET_REQUESTED,
    MAX_RETRIES_REACHED,
    NEW_WIFI_RECEIVED,
    WIFI_RETRY_TIMER
};
```

### Transition-Tabelle

| From | Event | To | Action |
|------|-------|----|---------|
| UNCONFIGURED | BLE_PAIR_START | PAIRING | BLE starten |
| PAIRING | BLE_CONFIG_RECEIVED | CONFIGURED | WiFi verbinden |
| CONFIGURED | WIFI_CONNECTED | OPERATIONAL | API validieren |
| CONFIGURED | WIFI_FAILED (3x) | RE_PAIRING | BLE + WiFi parallel |
| RE_PAIRING | NEW_WIFI_RECEIVED | CONFIGURED | Neue Credentials |
| OPERATIONAL | ERROR | ERROR | Recovery starten |
| ERROR | WIFI_CONNECTED | OPERATIONAL | Recovery erfolgreich |

## 9.4 Wichtige Konstanten

```cpp
// Retry/Timeout
const int MAX_RECONNECT_ATTEMPTS = 5;
const int RECONNECT_INTERVAL_MS = 30000;
const int REGISTRATION_RETRY_DELAY_MS = 5000;
const int MAX_WIFI_RETRIES = 3;

// Button
const int BOOT_BUTTON_PIN = 0;
const int WPS_BUTTON_HOLD_MS = 3000;
const int FACTORY_RESET_HOLD_MS = 10000;

// Storage
const int SD_MIN_FREE_SPACE = 1048576;  // 1 MB
const int FLUSH_INTERVAL_MS = 10000;

// Sync
const int SYNC_BUTTON_DEBOUNCE_MS = 50;
const int SYNC_BUTTON_SHORT_PRESS_MS = 1000;
const int SYNC_BUTTON_LONG_PRESS_MS = 3000;
```

---

# 10. Build & Deployment

## 10.1 PlatformIO Environments

| Environment | Plattform | Sensoren | Verwendung |
|-------------|-----------|----------|------------|
| `esp32` | ESP32 Dev | Echt | **Production** |
| `esp32_simulate` | ESP32 Dev | Simuliert | Hardware-Testing |
| `native` | Linux/macOS | Simuliert | Entwicklung |
| `native_test` | Linux/macOS | Simuliert | Unit-Tests |

## 10.2 Build-Befehle

### Production Build (ESP32)

```bash
cd myIoTGrid.Sensor

# Build
pio run -e esp32

# Upload
pio run -e esp32 --target upload

# Monitor
pio device monitor
```

### Simulation Build (ESP32)

```bash
# Build mit simulierten Sensoren
pio run -e esp32_simulate
```

### Native Build (Entwicklung)

```bash
# Build für lokale Entwicklung
pio run -e native

# Ausführen
.pio/build/native/program
```

### Unit-Tests

```bash
# Tests ausführen
pio test -e native_test
```

## 10.3 Docker (Sensor-Simulator)

### Build

```bash
cd myIoTGrid.Sensor
docker build -t myiotgrid-sensor-sim -f docker/Dockerfile .
```

### Run

```bash
docker run -d \
  --name sensor-sim \
  -e HUB_HOST=hub-api \
  -e HUB_PORT=5001 \
  -e HUB_PROTOCOL=https \
  -e HUB_INSECURE=true \
  myiotgrid-sensor-sim
```

### Docker-Compose

```yaml
sensor-sim:
  image: ghcr.io/myiotgrid/myiotgrid-sensor-sim:latest
  environment:
    - HUB_HOST=hub-api
    - HUB_PORT=5001
    - HUB_PROTOCOL=https
    - HUB_INSECURE=true
  volumes:
    - ./data:/app/data
  depends_on:
    - hub-api
```

## 10.4 OTA-Updates

### Über PlatformIO

```bash
# OTA Upload
pio run -e esp32 --target upload --upload-port <IP-Adresse>
```

### Über Hub (geplant)

```
Hub Dashboard → Sensor → Firmware Update → Upload
```

## 10.5 Dependencies

### ESP32

```ini
lib_deps =
    bblanchon/ArduinoJson@^7.2.1
    h2zero/NimBLE-Arduino@^1.4.0
    paulstoffregen/OneWire@^2.3.8
    milesburton/DallasTemperature@^3.11.0
    adafruit/Adafruit BME280 Library@^2.2.4
    adafruit/Adafruit Unified Sensor@^1.1.14
    adafruit/Adafruit BME680 Library@^2.0.4
    closedcube/ClosedCube SHT31D@^1.5.1
    claws/BH1750@^1.3.0
    sparkfun/SparkFun SCD30 Arduino Library@^1.0.20
    sensirion/Sensirion I2C SCD4x@^0.4.0
    adafruit/Adafruit CCS811 Library@^1.1.3
    adafruit/Adafruit SGP30 Sensor@^2.0.3
    adafruit/Adafruit TSL2561@^1.1.2
    pololu/VL53L0X@^1.3.1
    adafruit/Adafruit ADS1X15@^2.5.0
    adafruit/DHT sensor library@^1.4.6
    mikalhart/TinyGPSPlus@^1.0.3
```

---

# 11. Troubleshooting

## 11.1 Häufige Probleme

### Sensor findet kein WiFi

**Symptome:**
- LED blinkt gelb, dann rot
- Serial: `[Network] WiFi connection failed`

**Lösungen:**
1. WiFi-Passwort prüfen
2. Sensor näher am Router platzieren
3. 2.4 GHz Netzwerk verwenden (ESP32 unterstützt kein 5 GHz)
4. Factory Reset durchführen

### Sensor wird nicht in App gefunden

**Symptome:**
- App zeigt "Keine Sensoren gefunden"

**Lösungen:**
1. Bluetooth am Smartphone aktivieren
2. Standort-Berechtigung für App erteilen
3. Sensor aus-/einschalten
4. Factory Reset durchführen

### Keine Messwerte im Hub

**Symptome:**
- Sensor zeigt grüne LED
- Keine Daten im Dashboard

**Lösungen:**
1. Serial-Monitor prüfen (API-Fehler?)
2. Hub-URL in Sensor-Konfiguration prüfen
3. Firewall-Regeln prüfen (Port 5001)
4. Hub-Logs prüfen

### I2C-Sensor nicht erkannt

**Symptome:**
- Serial: `[Hardware] No device found at 0xXX`

**Lösungen:**
1. Verkabelung prüfen (SDA, SCL, VCC, GND)
2. Pull-up-Widerstände prüfen (4.7kΩ)
3. I2C-Adresse verifizieren (manche Sensoren haben Jumper)
4. Anderen I2C-Bus verwenden

### SD-Karte nicht erkannt

**Symptome:**
- Serial: `[Storage] SD card initialization failed`

**Lösungen:**
1. SD-Karte als FAT32 formatieren
2. Verkabelung prüfen (MISO, MOSI, SCK, CS)
3. Andere SD-Karte testen
4. Maximal 32 GB SD-Karte verwenden

## 11.2 LED-Fehlercodes

| Muster | Bedeutung | Lösung |
|--------|-----------|--------|
| Rot, 1x blinken | WiFi-Fehler | WiFi-Credentials prüfen |
| Rot, 2x blinken | Hub nicht erreichbar | Hub-Status prüfen |
| Rot, 3x blinken | API-Fehler | Hub-Logs prüfen |
| Rot, schnell blinkend | Hardware-Fehler | Serial-Monitor prüfen |
| Rot, dauerhaft | Kritischer Fehler | Factory Reset |

## 11.3 Debug-Befehle (Serial)

```
[System] Type 'help' for available commands

Available commands:
  help          - Show this help
  status        - Show current status
  config        - Show configuration
  sensors       - List detected sensors
  wifi          - Show WiFi status
  reset         - Factory reset
  reboot        - Reboot sensor
```

## 11.4 Recovery-Verfahren

### 1. Soft Recovery

```
1. Sensor aus-/einschalten
2. Warten auf grüne LED
3. Fertig
```

### 2. WiFi Reset

```
1. Boot-Button 3 Sekunden halten → WPS-Modus
2. Abbrechen (Button kurz drücken)
3. Sensor ist im Re-Provisioning-Modus
4. Mit App neu konfigurieren
```

### 3. Factory Reset

```
1. Boot-Button 10 Sekunden halten
2. LED blinkt rot
3. Loslassen
4. Sensor startet neu
5. Komplett neu einrichten
```

### 4. Firmware Recovery

```bash
# Falls OTA fehlschlägt, per USB:
pio run -e esp32 --target upload
```

---

# 12. Glossar

| Begriff | Bedeutung |
|---------|-----------|
| **BLE** | Bluetooth Low Energy - energiesparendes Bluetooth |
| **NVS** | Non-Volatile Storage - persistenter Speicher im ESP32 |
| **WPS** | WiFi Protected Setup - einfache WiFi-Konfiguration |
| **OTA** | Over-The-Air - drahtlose Updates |
| **Hub** | Grid.Hub - lokaler Server auf Raspberry Pi |
| **Node** | Ein Sensor-Gerät im myIoTGrid-Netzwerk |
| **Capability** | Fähigkeit eines Sensors (z.B. Temperatur messen) |
| **Endpoint** | Eindeutige ID für einen Sensor-Kanal |
| **Serial Number** | Eindeutige Geräte-ID (z.B. ESP32-0070078492CC) |
| **I2C** | Inter-Integrated Circuit - Bus für Sensoren |
| **One-Wire** | Einadrige Schnittstelle (z.B. DS18B20) |
| **UART** | Serielle Schnittstelle für GPS, etc. |
| **GPIO** | General Purpose Input/Output - digitale Pins |
| **ADC** | Analog-Digital-Converter - analoge Eingänge |

---

# Anhang

## A. Pinout ESP32 DevKit

```
                    ┌─────────────────┐
                    │     ESP32       │
                    │     DevKit      │
                    ├─────────────────┤
            EN ─────┤ EN          D23 ├───── MOSI (SD)
           VP ─────┤ VP          D22 ├───── SCL (I2C)
           VN ─────┤ VN          TX0 ├─────
          D34 ─────┤ D34         RX0 ├─────
          D35 ─────┤ D35         D21 ├───── SDA (I2C)
          D32 ─────┤ D32         D19 ├───── MISO (SD)
          D33 ─────┤ D33         D18 ├───── SCK (SD)
          D25 ─────┤ D25          D5 ├───── CS (SD)
          D26 ─────┤ D26         TX2 ├───── GPS TX
          D27 ─────┤ D27         RX2 ├───── GPS RX
          D14 ─────┤ D14          D4 ├───── Sync Button
          D12 ─────┤ D12          D2 ├───── Sync LED
          D13 ─────┤ D13         D15 ├─────
          GND ─────┤ GND         GND ├───── GND
          VIN ─────┤ VIN         3V3 ├───── 3.3V
                    │      [USB]      │
                    │       D0        │───── Boot Button
                    └─────────────────┘
```

## B. Changelog

### Version 1.9.1 (Sprint OS-01)
- Offline-Storage mit SD-Karte
- Sync-Manager mit Button-Trigger
- Sync-Status-LED
- Storage-Modi (Remote, Local, Hybrid)

### Version 1.8.0 (Sprint 8)
- Remote-Debug-System
- Debug-Level und Kategorien
- Hardware-Validator

### Version 1.7.0 (Sprint 7)
- Re-Provisioning-Mode
- Verbessertes Error-Recovery
- WPS-Support

### Version 1.6.0 (Sprint 6)
- Hub-Discovery per UDP
- Automatische Registrierung
- Heartbeat-System

---

**myIoTGrid Sensor Firmware**
*Open Source - Privacy First - Local First*

*Made with ❤️ in Germany*
