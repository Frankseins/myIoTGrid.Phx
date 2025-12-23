# myIoTGrid Bluetooth-System

## Dokumentation für alle Stakeholder

**Version:** 1.0
**Stand:** Dezember 2025
**Sprint:** BT-01 (Bluetooth Infrastructure)

---

# Teil 1: Für Nicht-Techniker

## Was macht das Bluetooth-System?

Stell dir vor, du hast einen Temperatursensor (ESP32) und möchtest die Messwerte auf deinem Smartphone oder Computer sehen. Dafür gibt es zwei Wege:

### Weg 1: Sensor mit WLAN verbinden (empfohlen)

```
┌─────────────┐      WiFi      ┌─────────────┐      Internet     ┌─────────────┐
│   Sensor    │ ─────────────▶ │   Router    │ ◀───────────────▶ │   Cloud     │
│   (ESP32)   │                │   (FritzBox)│                   │   App       │
└─────────────┘                └─────────────┘                   └─────────────┘
```

**Problem:** Der Sensor kennt dein WLAN-Passwort noch nicht!

**Lösung:** Du gibst dem Sensor per Bluetooth die WLAN-Zugangsdaten:

```
┌─────────────┐   Bluetooth    ┌─────────────┐
│   Dein      │ ─────────────▶ │   Sensor    │
│   Handy     │   "Hier ist    │   (ESP32)   │
│             │   das WLAN-    │             │
│             │   Passwort"    │             │
└─────────────┘                └─────────────┘
       │                              │
       │                              ▼
       │                       Sensor verbindet
       │                       sich mit WLAN
       │                              │
       ▼                              ▼
    ┌─────────────────────────────────────┐
    │         Beide im gleichen Netzwerk  │
    │         Sensor sendet Daten an Hub  │
    └─────────────────────────────────────┘
```

### Weg 2: Reines Bluetooth (ohne WLAN)

Für Orte ohne WLAN (Garten, Keller, Gewächshaus):

```
┌─────────────┐   Bluetooth    ┌─────────────┐
│   Sensor    │ ─────────────▶ │  Raspberry  │
│   (ESP32)   │   "21.5°C"     │     Pi      │
│   funkt     │   "65% Luft"   │   (Hub)     │
│   ständig   │                │             │
└─────────────┘                └─────────────┘
                                      │
                                      │ WLAN/LAN
                                      ▼
                               ┌─────────────┐
                               │   Dein      │
                               │   Computer  │
                               └─────────────┘
```

Der Sensor sendet seine Daten einfach per Bluetooth in die Luft. Der Raspberry Pi (Hub) empfängt diese und zeigt sie im Browser an.

---

## So funktioniert die Einrichtung (Schritt für Schritt)

### Szenario A: Sensor mit WLAN verbinden

1. **Sensor anschließen** - Strom anschließen, LED blinkt
2. **App öffnen** - myIoTGrid im Browser aufrufen
3. **"Neues Gerät hinzufügen"** - Button klicken
4. **Sensor wählen** - "myIoTGrid-92CC" erscheint in der Liste
5. **Verbinden** - Auf Sensor klicken
6. **WLAN eingeben** - Name und Passwort eingeben
7. **Fertig!** - Sensor verbindet sich mit WLAN und sendet Daten

### Szenario B: Reines Bluetooth (Beacon-Modus)

1. **Sensor anschließen** - Strom anschließen
2. **Sensor ist automatisch aktiv** - Sendet sofort Daten per Bluetooth
3. **Hub empfängt** - Raspberry Pi zeigt Daten automatisch an
4. **Fertig!** - Keine weitere Konfiguration nötig

---

## Häufige Fragen

**F: Wie weit reicht Bluetooth?**
A: Ca. 10-30 Meter, je nach Hindernissen (Wände).

**F: Brauche ich Internet?**
A: Nein, alles funktioniert auch offline im lokalen Netzwerk.

**F: Was passiert bei Stromausfall?**
A: Der Sensor merkt sich die Einstellungen und startet automatisch neu.

**F: Ist das sicher?**
A: Ja, die Bluetooth-Verbindung ist verschlüsselt und erfordert Authentifizierung.

---

# Teil 2: Für Product Owner

## Feature-Übersicht

### Epic: Bluetooth-basierte Gerätekommunikation

| Feature | Status | Priorität | Sprint |
|---------|--------|-----------|--------|
| BLE-Scanning im Frontend | Done | P1 | BT-01 |
| BLE-Scanning im Backend | Done | P1 | BT-02 |
| WLAN-Provisioning via BLE | Done | P1 | BT-01 |
| Beacon-Modus (passiv) | Done | P2 | BT-01 |
| GATT-basierte Konfiguration | Done | P1 | BT-01 |
| App-Level-Authentifizierung | Done | P1 | BT-01 |

---

## User Stories

### US-BT-001: Sensor einrichten (WLAN-Modus)

**Als** Endnutzer
**möchte ich** einen neuen Sensor über die Web-App einrichten
**damit** er Daten an meinen Hub senden kann.

**Akzeptanzkriterien:**
- [ ] Sensor erscheint in "Verfügbare Geräte"-Liste
- [ ] WLAN-Credentials können eingegeben werden
- [ ] Sensor verbindet sich nach Konfiguration mit WLAN
- [ ] Sensor erscheint als "Online" im Dashboard
- [ ] Erste Messwerte werden innerhalb von 60 Sekunden angezeigt

**Ablauf:**
```
┌────────────────────────────────────────────────────────────────────┐
│                        WLAN-PROVISIONING                           │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│  1. Nutzer         2. Frontend        3. Hub/Backend    4. ESP32  │
│     │                  │                   │               │      │
│     │─ Klick Scan ────▶│                   │               │      │
│     │                  │── BLE Scan ──────▶│               │      │
│     │                  │                   │◀── Advertising│      │
│     │◀─ Geräteliste ───│◀── Gefundene ─────│               │      │
│     │                  │     Geräte        │               │      │
│     │─ Wählt Gerät ───▶│                   │               │      │
│     │                  │── GATT Connect ──▶│── Connect ───▶│      │
│     │                  │                   │◀── DeviceInfo │      │
│     │◀─ Gerät-Info ────│◀── Device Info ───│               │      │
│     │                  │                   │               │      │
│     │─ WLAN eingeben ─▶│                   │               │      │
│     │                  │── Auth + Config ─▶│── CMD_AUTH ──▶│      │
│     │                  │                   │◀── RESP_OK ───│      │
│     │                  │                   │── CMD_WIFI ──▶│      │
│     │                  │                   │◀── RESP_OK ───│      │
│     │                  │                   │── CMD_REBOOT ▶│      │
│     │                  │                   │               │      │
│     │                  │                   │     ESP32 startet    │
│     │                  │                   │     mit WLAN neu     │
│     │                  │                   │               │      │
│     │◀─ "Erfolgreich" ─│◀── Registriert ───│◀── Readings ──│      │
│     │                  │                   │               │      │
└────────────────────────────────────────────────────────────────────┘
```

---

### US-BT-002: Sensor im Beacon-Modus betreiben

**Als** Nutzer mit Sensoren außerhalb der WLAN-Reichweite
**möchte ich** Messwerte per Bluetooth empfangen
**damit** ich auch in Keller/Garten/Gewächshaus messen kann.

**Akzeptanzkriterien:**
- [ ] Sensor sendet automatisch im Beacon-Modus
- [ ] Hub empfängt Daten ohne aktive Verbindung
- [ ] Messwerte erscheinen im Dashboard
- [ ] Reichweite: mindestens 10 Meter

**Ablauf:**
```
┌────────────────────────────────────────────────────────────────────┐
│                          BEACON-MODUS                              │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│  ESP32 Sensor              Bluetooth (Luft)           Raspberry Pi│
│      │                          │                          │      │
│      │                          │                          │      │
│      │── Advertising ──────────▶│◀─────── Passive Scan ────│      │
│      │   [Temp: 21.5°C]         │                          │      │
│      │   [Humidity: 65%]        │                          │      │
│      │   [NodeID-Hash]          │                          │      │
│      │                          │                          │      │
│      │                          │────── Parse Beacon ─────▶│      │
│      │                          │                          │      │
│      │                          │                    ┌─────┴─────┐│
│      │                          │                    │  Speichern││
│      │                          │                    │  in DB    ││
│      │                          │                    └─────┬─────┘│
│      │                          │                          │      │
│      │                          │                    ┌─────┴─────┐│
│      │                          │                    │  Dashboard││
│      │                          │                    │  Update   ││
│      │                          │                    └───────────┘│
│      │                          │                          │      │
│  (alle 60 Sekunden wiederholen)                                   │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
```

---

### US-BT-003: Sensor remote konfigurieren

**Als** Administrator
**möchte ich** einen bereits installierten Sensor per Bluetooth umkonfigurieren
**damit** ich nicht physisch zum Gerät gehen muss.

**Akzeptanzkriterien:**
- [ ] Verbindung zu bestehendem Sensor möglich
- [ ] Authentifizierung mit Node-ID erforderlich
- [ ] WLAN-Credentials können geändert werden
- [ ] Messintervall kann angepasst werden
- [ ] Factory-Reset möglich

---

## Verfügbare Befehle

| Befehl | Code | Beschreibung | Erfordert Auth |
|--------|------|--------------|----------------|
| Authentifizieren | `0x00` | Hub authentifiziert sich mit Node-ID-Hash | Nein |
| WLAN setzen | `0x01` | WLAN-Credentials übertragen | Ja |
| Hub-URL setzen | `0x02` | API-Endpunkt konfigurieren | Ja |
| Node-ID setzen | `0x03` | Geräte-ID ändern | Ja |
| Intervall setzen | `0x04` | Messintervall in Sekunden | Ja |
| Factory Reset | `0xFE` | Alle Einstellungen löschen | Ja |
| Neustart | `0xFF` | Gerät neu starten | Ja |

---

## Metriken & KPIs

| Metrik | Ziel | Aktuell |
|--------|------|---------|
| Erfolgreiche Provisioning-Rate | >95% | - |
| Durchschnittliche Einrichtungszeit | <2 min | - |
| Bluetooth-Reichweite | >10m | ~15m |
| Beacon-Update-Intervall | 60s | 60s |
| Batterielebensdauer (Beacon) | >1 Jahr | - |

---

# Teil 3: Für Entwickler

## Architektur-Übersicht

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              SYSTEM-ARCHITEKTUR                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────┐                          ┌─────────────────────────┐  │
│  │    ESP32        │                          │      Raspberry Pi       │  │
│  │    Sensor       │                          │         (Hub)           │  │
│  │                 │                          │                         │  │
│  │ ┌─────────────┐ │                          │ ┌─────────────────────┐ │  │
│  │ │ BLE Beacon  │─┼──── Advertising ────────▶│ │ Beacon Scanner     │ │  │
│  │ │ Mode        │ │     (passive)            │ │ (BluetoothHub)     │ │  │
│  │ └─────────────┘ │                          │ └─────────────────────┘ │  │
│  │                 │                          │                         │  │
│  │ ┌─────────────┐ │                          │ ┌─────────────────────┐ │  │
│  │ │ BLE GATT    │◀┼──── Connect/Config ─────▶│ │ BleGattClientService│ │  │
│  │ │ Server      │ │     (bidirektional)      │ │ (InTheHand.BLE)    │ │  │
│  │ └─────────────┘ │                          │ └─────────────────────┘ │  │
│  │                 │                          │                         │  │
│  │ ┌─────────────┐ │                          │ ┌─────────────────────┐ │  │
│  │ │ WiFi Client │─┼──── HTTP/REST ──────────▶│ │ .NET API           │ │  │
│  │ │ (optional)  │ │     (nach Config)        │ │ (myIoTGrid.Hub)    │ │  │
│  │ └─────────────┘ │                          │ └─────────────────────┘ │  │
│  │                 │                          │                         │  │
│  │  NimBLE Stack   │                          │  BlueZ + D-Bus          │  │
│  └─────────────────┘                          └─────────────────────────┘  │
│                                                                             │
│  Firmware: ble_beacon_mode.cpp                Service: BleGattClientService│
│  Version: 1.21.0                              Package: InTheHand.BLE 4.0.37│
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Komponenten-Übersicht

### ESP32 Firmware (myIoTGrid.Sensor)

| Datei | Beschreibung |
|-------|--------------|
| `include/ble_beacon_mode.h` | Header für BLE Hybrid Mode |
| `src/ble_beacon_mode.cpp` | Implementierung Beacon + GATT |
| `include/config.h` | UUIDs, Konstanten, Timing |

### Hub Backend (.NET)

| Datei | Beschreibung |
|-------|--------------|
| `Shared.Common/DTOs/BluetoothHubDto.cs` | BLE DTOs |
| `Shared.Contracts/Services/IBleGattClientService.cs` | GATT Client Interface |
| `Hub.Service/Services/BleGattClientService.cs` | GATT Client Implementierung |
| `Hub.Service/Services/BluetoothPairingService.cs` | BLE Scanning (hcitool) |
| `Hub.Interface/Controllers/BluetoothHubsController.cs` | REST API Endpunkte |

---

## GATT Service-Definition

### Service UUID
```
CONFIG_SERVICE_UUID = "4d494f54-4752-4944-434f-4e4649470000"
                       (ASCII: "MIOTGRIDCONFIG" + padding)
```

### Characteristics

| Characteristic | UUID | Properties | Beschreibung |
|----------------|------|------------|--------------|
| Config Write | `...0001` | WRITE, WRITE_NR | Hub schreibt Befehle |
| Config Read | `...0002` | READ, NOTIFY | Hub liest Geräte-Info |
| Sensor Data | `...0003` | READ, NOTIFY | Hub liest Sensor-Werte |

---

## Datenformate

### Device Info (Config Read Characteristic)

ESP32 liefert JSON:
```json
{
  "nodeId": "sensor-wohnzimmer-01",
  "deviceName": "myIoTGrid-92CC",
  "firmware": "1.21.0",
  "hash": "A1B2C3D4"
}
```

### Sensor Data (Sensor Data Characteristic)

ESP32 liefert JSON:
```json
{
  "t": 21.50,    // Temperature in °C
  "h": 65.0,     // Humidity in %
  "p": 1013.25,  // Pressure in hPa
  "b": 3300,     // Battery in mV
  "f": 0         // Flags (bit field)
}
```

### Beacon Advertising Data

Manufacturer Data (Company ID 0xFFFF):
```
Offset  Size  Beschreibung
0       2     Temperature (int16, *100) → 2150 = 21.50°C
2       2     Humidity (uint16, *100) → 6500 = 65.00%
4       2     Pressure (uint16, -50000) → 51325 = 101325 hPa
6       2     Battery (uint16, mV) → 3300 = 3.3V
```

---

## Authentifizierung

### Ablauf

```
Hub                                         ESP32
 │                                            │
 │  1. Berechne Hash aus Node-ID              │
 │     hash = 0                               │
 │     for char in nodeId:                    │
 │         hash = hash * 31 + char            │
 │                                            │
 │  2. Sende CMD_AUTH + 4-Byte-Hash           │
 │ ─────────────────────────────────────────▶ │
 │     [0x00][B0][B1][B2][B3]                 │
 │                                            │
 │                     3. ESP32 vergleicht    │
 │                        mit eigenem Hash    │
 │                                            │
 │  4. Antwort                                │
 │ ◀───────────────────────────────────────── │
 │     [0x00] = OK                            │
 │     [0x01] = Error (Hash stimmt nicht)     │
 │                                            │
```

### Hash-Algorithmus (C# & C++)

```csharp
// C# (.NET)
public byte[] ComputeNodeIdHash(string nodeId)
{
    uint hash = 0;
    foreach (var c in nodeId)
    {
        hash = hash * 31 + c;
    }
    return new byte[]
    {
        (byte)((hash >> 24) & 0xFF),
        (byte)((hash >> 16) & 0xFF),
        (byte)((hash >> 8) & 0xFF),
        (byte)(hash & 0xFF)
    };
}
```

```cpp
// C++ (ESP32)
void computeNodeIdHash(const String& nodeId) {
    uint32_t hash = 0;
    for (size_t i = 0; i < nodeId.length(); i++) {
        hash = hash * 31 + nodeId.charAt(i);
    }
    _nodeIdHash[0] = (hash >> 24) & 0xFF;
    _nodeIdHash[1] = (hash >> 16) & 0xFF;
    _nodeIdHash[2] = (hash >> 8) & 0xFF;
    _nodeIdHash[3] = hash & 0xFF;
}
```

---

## API-Endpunkte

### BLE Scanning & Pairing

| Methode | Endpoint | Beschreibung |
|---------|----------|--------------|
| GET | `/api/bluetoothhubs/scan` | BLE-Scan starten |
| POST | `/api/bluetoothhubs/pair` | Gerät pairen |
| GET | `/api/bluetoothhubs/paired-devices` | Gepaarte Geräte |

### GATT Client

| Methode | Endpoint | Beschreibung |
|---------|----------|--------------|
| POST | `/api/bluetoothhubs/gatt/connect/{mac}` | GATT-Verbindung |
| POST | `/api/bluetoothhubs/gatt/disconnect` | Trennen |
| GET | `/api/bluetoothhubs/gatt/status` | Verbindungsstatus |
| POST | `/api/bluetoothhubs/gatt/authenticate` | Auth mit Node-ID |
| GET | `/api/bluetoothhubs/gatt/sensor-data` | Sensordaten lesen |
| GET | `/api/bluetoothhubs/gatt/device-info` | Geräte-Info lesen |
| POST | `/api/bluetoothhubs/gatt/config/wifi` | WLAN setzen |
| POST | `/api/bluetoothhubs/gatt/config/hub-url` | Hub-URL setzen |
| POST | `/api/bluetoothhubs/gatt/reboot` | Neustart |
| POST | `/api/bluetoothhubs/gatt/provision` | Komplett-Provisioning |

### Beispiel: Provisioning-Request

```bash
curl -X POST https://hub.local:5001/api/bluetoothhubs/gatt/provision \
  -H "Content-Type: application/json" \
  -d '{
    "macAddress": "00:70:07:84:92:CE",
    "nodeId": "sensor-wohnzimmer-01",
    "wifiConfig": {
      "ssid": "MeinWLAN",
      "password": "GeheimesPasswort"
    },
    "hubUrlConfig": {
      "hubUrl": "https://192.168.1.100",
      "port": 5001
    },
    "intervalSeconds": 60
  }'
```

---

## Befehls-Protokoll (Config Write)

### Befehlsstruktur

```
Byte 0:     Command Code
Bytes 1-n:  Payload (command-specific)
```

### CMD_AUTH (0x00)
```
[0x00][Hash0][Hash1][Hash2][Hash3]
  │      └────────┬────────────┘
  │               └── 4-Byte Node-ID Hash
  └── Command
```

### CMD_SET_WIFI (0x01)
```
[0x01][SSID_LEN][SSID...][PWD_LEN][PWD...]
  │       │         │        │       │
  │       │         │        │       └── Password bytes
  │       │         │        └── Password length
  │       │         └── SSID bytes
  │       └── SSID length
  └── Command
```

### CMD_SET_HUB_URL (0x02)
```
[0x02][URL_LEN][URL...][PORT_LO][PORT_HI]
  │       │       │        │        │
  │       │       │        └────────┴── Port (little-endian)
  │       │       └── URL bytes
  │       └── URL length
  └── Command
```

### Antwort-Codes

| Code | Name | Beschreibung |
|------|------|--------------|
| 0x00 | RESP_OK | Befehl erfolgreich |
| 0x01 | RESP_ERROR | Allgemeiner Fehler |
| 0x02 | RESP_INVALID_CMD | Unbekannter Befehl |
| 0x03 | RESP_INVALID_DATA | Ungültige Daten |
| 0x04 | RESP_NOT_AUTHENTICATED | Authentifizierung erforderlich |

---

## Sicherheit

### Warum keine BLE-Level-Security?

- BlueZ auf Raspberry Pi + NimBLE auf ESP32 haben Kompatibilitätsprobleme beim Bonding
- Connection Timeouts bei aktivierter BLE-Security
- **Workaround:** Application-Level Authentication mit Node-ID Hash

### Sicherheitsmaßnahmen

1. **Hash-basierte Auth:** Hub muss Node-ID-Hash kennen
2. **Lokales Netzwerk:** Hub ist nicht aus dem Internet erreichbar
3. **HTTPS:** API-Kommunikation ist verschlüsselt
4. **Keine Secrets im Beacon:** Nur Sensordaten, keine Credentials

### Zukünftige Verbesserungen

- [ ] BLE Bonding wenn BlueZ/NimBLE-Kompatibilität verbessert
- [ ] Challenge-Response statt statischem Hash
- [ ] Verschlüsselung der Beacon-Daten

---

## Fehlerbehebung

### Problem: Gerät wird nicht gefunden

```bash
# Auf Raspberry Pi:
sudo hcitool lescan

# Erwartete Ausgabe:
# 00:70:07:84:92:CE myIoTGrid-92CC
```

### Problem: GATT-Verbindung schlägt fehl

```bash
# BlueZ-Status prüfen:
systemctl status bluetooth

# Adapter-Status:
hciconfig hci0

# Falls down:
sudo hciconfig hci0 up
```

### Problem: Authentifizierung fehlgeschlagen

1. Node-ID prüfen (muss exakt übereinstimmen)
2. Hash-Berechnung verifizieren
3. ESP32 Serial-Monitor prüfen für Debug-Output

### ESP32 Debug-Ausgabe aktivieren

```cpp
// In ble_beacon_mode.cpp wird automatisch geloggt:
// [BLE-Auth] Received hash: A1B2C3D4
// [BLE-Auth] Expected hash: A1B2C3D4
// [BLE-Auth] Hash matches - authenticated!
```

---

## Sequenzdiagramme

### Vollständiger WLAN-Provisioning-Flow

```
┌────────┐     ┌─────────┐     ┌──────────┐     ┌─────────┐
│Frontend│     │Hub API  │     │GATT Svc  │     │ ESP32   │
└───┬────┘     └────┬────┘     └────┬─────┘     └────┬────┘
    │               │               │                │
    │ POST /scan    │               │                │
    │──────────────▶│               │                │
    │               │ hcitool lescan│                │
    │               │──────────────▶│                │
    │               │               │◀── Advertising │
    │               │◀──────────────│                │
    │◀──────────────│ Geräte-Liste  │                │
    │               │               │                │
    │ POST /gatt/   │               │                │
    │ connect/MAC   │               │                │
    │──────────────▶│               │                │
    │               │ ConnectAsync  │                │
    │               │──────────────▶│                │
    │               │               │── BLE Connect─▶│
    │               │               │◀── Connected ──│
    │               │               │── Read Info ──▶│
    │               │               │◀── JSON ───────│
    │               │◀──────────────│                │
    │◀──────────────│ DeviceInfo    │                │
    │               │               │                │
    │ POST /gatt/   │               │                │
    │ authenticate  │               │                │
    │ {nodeId}      │               │                │
    │──────────────▶│               │                │
    │               │ Compute Hash  │                │
    │               │ AuthAsync     │                │
    │               │──────────────▶│                │
    │               │               │── CMD_AUTH ───▶│
    │               │               │◀── RESP_OK ────│
    │               │◀──────────────│                │
    │◀──────────────│ AuthResult    │                │
    │               │               │                │
    │ POST /gatt/   │               │                │
    │ config/wifi   │               │                │
    │ {ssid,pwd}    │               │                │
    │──────────────▶│               │                │
    │               │ SetWifiAsync  │                │
    │               │──────────────▶│                │
    │               │               │── CMD_WIFI ───▶│
    │               │               │◀── RESP_OK ────│
    │               │◀──────────────│                │
    │◀──────────────│ ConfigResult  │                │
    │               │               │                │
    │ POST /gatt/   │               │                │
    │ reboot        │               │                │
    │──────────────▶│               │                │
    │               │ RebootAsync   │                │
    │               │──────────────▶│                │
    │               │               │── CMD_REBOOT ─▶│
    │               │               │   (disconnect) │
    │               │◀──────────────│                │
    │◀──────────────│ Success       │                │
    │               │               │                │
    │               │               │      ESP32 startet mit WLAN
    │               │               │                │
    │               │               │                │── HTTP POST
    │               │◀──────────────│────────────────│   /api/readings
    │               │  Readings     │                │
    │               │               │                │
```

---

## Projektstruktur

```
myIoTGrid/
├── myIoTGrid.Sensor/                    # ESP32 Firmware
│   ├── include/
│   │   ├── ble_beacon_mode.h           # BLE Hybrid Mode Header
│   │   └── config.h                     # UUIDs, Konstanten
│   └── src/
│       └── ble_beacon_mode.cpp          # BLE Implementierung
│
├── myIoTGrid.Shared/                    # Shared Libraries
│   ├── myIoTGrid.Shared.Common/
│   │   └── DTOs/
│   │       └── BluetoothHubDto.cs       # BLE DTOs
│   └── myIoTGrid.Shared.Contracts/
│       └── Services/
│           └── IBleGattClientService.cs # GATT Client Interface
│
└── myIoTGrid.Hub/                       # Hub Backend
    └── src/
        ├── myIoTGrid.Hub.Service/
        │   └── Services/
        │       ├── BleGattClientService.cs      # GATT Client
        │       └── BluetoothPairingService.cs   # BLE Scanning
        └── myIoTGrid.Hub.Interface/
            └── Controllers/
                └── BluetoothHubsController.cs   # REST API
```

---

## Changelog

### Version 1.21.0 (Dezember 2025)

**Neue Features:**
- App-Level-Authentifizierung mit Node-ID-Hash
- GATT-basierte Konfiguration (WiFi, Hub-URL, Interval)
- High-Level Provisioning-Endpoint
- JSON-basierte Datenformate

**Behobene Probleme:**
- BLE-Level-Security Timeout mit BlueZ (Workaround)
- Bleak API-Änderungen (rssi Attribut)

**Breaking Changes:**
- Keine

---

## Nächste Schritte

1. **Frontend-Integration:** Angular-Komponenten für BLE-Wizard
2. **Beacon-Scanner-Service:** Hintergrund-Service für Beacon-Empfang
3. **Multi-Sensor-Support:** Gleichzeitige Verbindung zu mehreren ESP32
4. **OTA-Updates:** Firmware-Update über BLE

---

*Dokumentation erstellt von Claude Code*
*myIoTGrid - Open Source IoT Platform*
