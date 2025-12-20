# myIoTGrid Bluetooth Hub Service

Bluetooth-Gateway-Service für ESP32-Sensoren. Empfängt Sensordaten via BLE und leitet sie an die myIoTGrid API weiter.

## Features

- BLE Device Discovery (sucht nach `myIoTGrid-*` und `ESP32-*` Geräten)
- Automatische Verbindung zu registrierten Geräten
- JSON-Parsing von Sensordaten
- API-Forwarding mit Retry-Logik
- Offline-Queue für fehlgeschlagene Sends
- Systemd Integration für Linux
- Docker Support

## Voraussetzungen

- .NET 8.0 SDK oder höher
- Bluetooth-fähiges System
- Für Linux: BlueZ (wird automatisch installiert im Docker-Image)

## Entwicklung

```bash
# Projekt bauen
cd myIoTGrid.BluetoothHub
dotnet build

# Projekt starten
dotnet run --project src/myIoTGrid.BluetoothHub

# Mit Development-Konfiguration
DOTNET_ENVIRONMENT=Development dotnet run --project src/myIoTGrid.BluetoothHub
```

## Konfiguration

Die Konfiguration erfolgt über `appsettings.json`:

```json
{
  "BluetoothHub": {
    "HubId": "hub-rpi5-01",
    "HubName": "Raspberry Pi 5 Bluetooth Hub",
    "ApiBaseUrl": "http://localhost:5000",
    "ScanInterval": 30000,
    "ReconnectDelay": 5000,
    "ServiceUUID": "4fafc201-1fb5-459e-8fcc-c5c9c331914b",
    "SensorDataUUID": "beb5483e-36e1-4688-b7f5-ea07361b26ac",
    "RegisteredDevices": [
      {
        "Name": "myIoTGrid-Sensor1",
        "NodeId": "ESP32-AABBCCDDEEFF",
        "ExpectedSensors": ["temperature", "humidity"]
      }
    ]
  }
}
```

## Deployment auf Raspberry Pi

### Build für ARM64

```bash
dotnet publish -c Release -r linux-arm64 --self-contained -o publish
```

### Transfer und Installation

```bash
# Dateien übertragen
scp -r publish/* pi@phx1.local:/opt/myiotgrid/bluetooth-hub/

# Auf dem Pi: Systemd Service erstellen
sudo nano /etc/systemd/system/bluetooth-hub.service
```

### Systemd Service

```ini
[Unit]
Description=myIoTGrid Bluetooth Hub Service
After=network.target bluetooth.target

[Service]
Type=notify
User=root
WorkingDirectory=/opt/myiotgrid/bluetooth-hub
ExecStart=/opt/myiotgrid/bluetooth-hub/myIoTGrid.BluetoothHub
Restart=always
RestartSec=10
Environment=DOTNET_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

### Service aktivieren

```bash
sudo systemctl daemon-reload
sudo systemctl enable bluetooth-hub
sudo systemctl start bluetooth-hub
sudo systemctl status bluetooth-hub

# Logs anzeigen
sudo journalctl -u bluetooth-hub -f
```

## Docker

### Image bauen

```bash
docker build -t myiotgrid-bluetooth-hub .
```

### Container starten

```bash
docker run -d \
  --name bluetooth-hub \
  --privileged \
  --net=host \
  -v /var/run/dbus:/var/run/dbus \
  -v $(pwd)/logs:/app/logs \
  -e BluetoothHub__ApiBaseUrl=http://host.docker.internal:5000 \
  myiotgrid-bluetooth-hub
```

**Hinweis:** `--privileged` und `--net=host` sind für Bluetooth-Zugriff erforderlich.

## BLE UUIDs

| Characteristic | UUID | Beschreibung |
|----------------|------|--------------|
| Service | `4fafc201-1fb5-459e-8fcc-c5c9c331914b` | Haupt-Service |
| SENSOR_DATA | `beb5483e-36e1-4688-b7f5-ea07361b26ac` | Sensordaten (Notifications) |
| STATUS | `beb5483e-36e1-4688-b7f5-ea07361b26ab` | Gerätestatus |

## Sensor-Datenformat

```json
{
  "nodeId": "ESP32-AABBCCDDEEFF",
  "timestamp": "2024-12-18T12:30:45Z",
  "sensors": {
    "temperature": 22.5,
    "humidity": 65.2,
    "pressure": 1013.25,
    "uv": 3.5,
    "waterLevel": null,
    "gps": {
      "latitude": 50.9375,
      "longitude": 6.9603,
      "altitude": 55.0
    }
  },
  "battery": 87,
  "rssi": -45
}
```

## Architektur

```
ESP32 Sensoren ──BLE──> Bluetooth Hub ──HTTP──> myIoTGrid API
                              │
                              └──> Offline Queue (bei API-Ausfall)
```

## Bekannte Einschränkungen

- **InTheHand.BluetoothLE**: Für interaktive Szenarien gedacht. Für echten Background-Scanning-Support auf Linux wird BlueZ D-Bus API benötigt.
- **macOS**: Eingeschränkter Background-Bluetooth-Support
- **Windows**: Voller Support über Windows.Devices.Bluetooth.Advertisement

## Lizenz

MIT License
