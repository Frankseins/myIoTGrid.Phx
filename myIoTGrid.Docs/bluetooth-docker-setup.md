# Bluetooth/BLE in Docker auf Raspberry Pi

## Übersicht

Der myIoTGrid Hub unterstützt Bluetooth Low Energy (BLE) für die direkte Kommunikation mit ESP32-Sensoren. Diese Anleitung beschreibt die notwendige Konfiguration für den Docker-Betrieb auf einem Raspberry Pi.

## Problem

Docker-Container sind standardmäßig von der Host-Hardware isoliert. Für Bluetooth-Zugriff benötigt der Container:
- Zugriff auf den Bluetooth-Adapter (`/dev/hci0`)
- D-Bus Kommunikation mit dem BlueZ Stack des Hosts
- Entsprechende Berechtigungen

## Voraussetzungen auf dem Raspberry Pi Host

### 1. BlueZ (Bluetooth Stack) muss laufen

```bash
# Status prüfen
sudo systemctl status bluetooth

# Falls nicht aktiv
sudo systemctl enable bluetooth
sudo systemctl start bluetooth
```

### 2. Bluetooth-Adapter vorhanden

```bash
# Adapter prüfen
hciconfig -a

# Erwartete Ausgabe:
# hci0: Type: Primary  Bus: UART
#       BD Address: XX:XX:XX:XX:XX:XX
#       UP RUNNING
```

### 3. D-Bus Socket vorhanden

```bash
ls -la /var/run/dbus/
# Muss system_bus_socket enthalten
```

## Docker-Konfiguration

### Dockerfile Änderungen

Im Runtime-Stage wurden folgende Pakete hinzugefügt:

```dockerfile
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        curl \
        openssl \
        dbus \           # D-Bus Daemon
        libdbus-1-3 \    # D-Bus Client Library
        bluez \          # Linux Bluetooth Stack
    && rm -rf /var/lib/apt/lists/*
```

Umgebungsvariable für D-Bus:

```dockerfile
ENV DBUS_SYSTEM_BUS_ADDRESS="unix:path=/var/run/dbus/system_bus_socket"
```

### docker-compose.yml Änderungen

```yaml
hub-api:
  # Bluetooth requirements
  privileged: true
  network_mode: host      # Erforderlich für D-Bus Zugriff
  user: root              # D-Bus benötigt root-Rechte

  # Bluetooth-Adapter durchreichen
  devices:
    - /dev:/dev

  # D-Bus Socket vom Host mounten (OHNE :ro!)
  volumes:
    - /var/run/dbus:/var/run/dbus

  # BLE Scanner aktivieren
  environment:
    - Ble__Enabled=true
    - DBUS_SYSTEM_BUS_ADDRESS=unix:path=/var/run/dbus/system_bus_socket

# Frontend braucht extra_hosts weil hub-api im host network ist
hub-frontend:
  extra_hosts:
    - "hub-api:host-gateway"
```

**Wichtig:** Bei `network_mode: host` werden keine `ports:` und `networks:` Einträge benötigt.

## Konfiguration (appsettings.json)

```json
{
  "Ble": {
    "Enabled": true,
    "ScanIntervalMs": 30000,
    "ServiceUuid": "12345678-1234-5678-1234-56789abcdef0",
    "SensorDataUuid": "12345678-1234-5678-1234-56789abcdef1",
    "DeviceInfoUuid": "12345678-1234-5678-1234-56789abcdef2"
  }
}
```

Die UUIDs müssen mit der ESP32-Firmware übereinstimmen.

## Verifizierung

### Container-Logs prüfen

```bash
docker logs myIoTGrid.Hub 2>&1 | grep -i ble
```

**Erfolgreiche Ausgabe:**
```
BLE Scanner starting...
Service UUID: 12345678-1234-5678-1234-56789abcdef0
Scan interval: 30000ms
Bluetooth is available. Starting scan loop...
```

**Fehlerhafte Ausgabe:**
```
Bluetooth is not available on this system. BLE Scanner will not run.
```

### Im Container testen

```bash
# In Container einloggen
docker exec -it myIoTGrid.Hub bash

# D-Bus Umgebung prüfen
echo $DBUS_SYSTEM_BUS_ADDRESS
# Erwartete Ausgabe: unix:path=/var/run/dbus/system_bus_socket

# D-Bus Socket prüfen
ls -la /var/run/dbus/
```

## Troubleshooting

### Problem: "Bluetooth is not available"

1. **BlueZ auf Host prüfen:**
   ```bash
   sudo systemctl status bluetooth
   hciconfig -a
   ```

2. **D-Bus Mount prüfen:**
   ```bash
   docker exec myIoTGrid.Hub ls -la /var/run/dbus/
   ```

3. **Privileged Mode aktiv?**
   ```bash
   docker inspect myIoTGrid.Hub | grep -i privileged
   ```

### Problem: D-Bus Verbindungsfehler

```bash
# Container mit root-Rechten betreten
docker exec -u root -it myIoTGrid.Hub bash

# D-Bus Tools nachinstallieren (temporär)
apt-get update && apt-get install -y dbus

# D-Bus Verbindung testen
dbus-send --system --dest=org.bluez --print-reply /org/bluez org.freedesktop.DBus.Introspectable.Introspect
```

## Architektur

```
┌─────────────────────────────────────────────────────────┐
│                    Raspberry Pi Host                     │
├─────────────────────────────────────────────────────────┤
│                                                          │
│   ┌──────────────┐     ┌──────────────────────────┐     │
│   │  Bluetooth   │     │      BlueZ Daemon        │     │
│   │   Adapter    │◄───►│    (bluetoothd)          │     │
│   │   (hci0)     │     │                          │     │
│   └──────────────┘     └───────────┬──────────────┘     │
│                                    │                     │
│                        ┌───────────▼──────────────┐     │
│                        │   D-Bus System Bus       │     │
│                        │ /var/run/dbus/socket     │     │
│                        └───────────┬──────────────┘     │
│                                    │                     │
│   ┌────────────────────────────────┼────────────────┐   │
│   │            Docker Container    │                │   │
│   │                                ▼                │   │
│   │   ┌──────────────────────────────────────┐     │   │
│   │   │         .NET Hub API                 │     │   │
│   │   │                                      │     │   │
│   │   │  ┌────────────────────────────────┐ │     │   │
│   │   │  │   BleScannerHostedService      │ │     │   │
│   │   │  │                                │ │     │   │
│   │   │  │  InTheHand.Bluetooth (NuGet)   │ │     │   │
│   │   │  │         │                      │ │     │   │
│   │   │  │  Tmds.DBus (.NET D-Bus Client) │ │     │   │
│   │   │  └────────────────────────────────┘ │     │   │
│   │   └──────────────────────────────────────┘     │   │
│   │                                                │   │
│   │  Mounts:                                       │   │
│   │  - /var/run/dbus:/var/run/dbus:ro             │   │
│   │  - /dev:/dev (privileged)                      │   │
│   └────────────────────────────────────────────────┘   │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

## Verwendete Bibliotheken

| Bibliothek | Version | Zweck |
|------------|---------|-------|
| InTheHand.Bluetooth | NuGet | Cross-Platform BLE API für .NET |
| Tmds.DBus | (Dependency) | D-Bus Client für Linux |
| BlueZ | 5.x | Linux Bluetooth Stack (auf Host) |
| libdbus-1-3 | apt | D-Bus Client Library (im Container) |

## Sicherheitshinweise

- `privileged: true` gibt dem Container vollen Zugriff auf Host-Hardware
- Alternative: Spezifische Capabilities statt privileged:
  ```yaml
  cap_add:
    - NET_ADMIN
    - SYS_ADMIN
  ```
- D-Bus Mount ist read-only (`:ro`) um Schreibzugriffe zu verhindern

## Referenzen

- [InTheHand.Bluetooth](https://github.com/inthehand/32feet)
- [BlueZ Documentation](http://www.bluez.org/)
- [Docker Device Access](https://docs.docker.com/engine/reference/run/#runtime-privilege-and-linux-capabilities)
