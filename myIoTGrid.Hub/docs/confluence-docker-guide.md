# myIoTGrid Hub - Docker Guide

> **Confluence Space:** myIoTGrid
> **Seite:** Docker Entwicklungsumgebung
> **Version:** 1.0
> **Stand:** November 2025

---

## Übersicht

Die myIoTGrid Hub Entwicklungsumgebung läuft vollständig in Docker-Containern. Dies ermöglicht eine konsistente Entwicklungs- und Testumgebung auf allen Plattformen.

### Container-Stack

| Service | Container Name | Image | Beschreibung |
|---------|----------------|-------|--------------|
| **Hub API** | `myiotgrid-hub-api` | .NET 10 | REST API, SignalR Hub |
| **Mosquitto** | `myiotgrid-mosquitto` | eclipse-mosquitto:2 | MQTT Broker |

### Ports & Adressen

| Service | Port | URL | Beschreibung |
|---------|------|-----|--------------|
| **API** | 5001 | http://localhost:5001 | REST API Basis-URL |
| **Swagger** | 5001 | http://localhost:5001/swagger | API Dokumentation |
| **Health Check** | 5001 | http://localhost:5001/health | Health Endpoint |
| **SignalR** | 5001 | http://localhost:5001/hubs/sensors | WebSocket Hub |
| **MQTT** | 1883 | localhost:1883 | MQTT Standard Port |
| **MQTT WebSocket** | 9001 | localhost:9001 | MQTT über WebSocket |

---

## Schnellstart

### Voraussetzungen

- Docker Desktop installiert
- Terminal/Command Line Zugriff

### Stack starten

```bash
# In das Backend-Verzeichnis wechseln
cd myIoTGrid.Hub/myIoTGrid.Hub.Backend

# Alle Container starten (im Hintergrund)
docker compose up -d

# Status prüfen
docker compose ps
```

### Stack stoppen

```bash
# Alle Container stoppen
docker compose down

# Container stoppen UND Volumes löschen (Daten werden gelöscht!)
docker compose down -v
```

---

## Wichtige Befehle

### Container-Management

| Befehl | Beschreibung |
|--------|--------------|
| `docker compose up -d` | Stack im Hintergrund starten |
| `docker compose down` | Stack stoppen |
| `docker compose restart` | Stack neu starten |
| `docker compose ps` | Status aller Container |
| `docker compose build` | Images neu bauen |
| `docker compose build --no-cache` | Images komplett neu bauen |

### Logs anzeigen

| Befehl | Beschreibung |
|--------|--------------|
| `docker compose logs` | Alle Logs anzeigen |
| `docker compose logs -f` | Logs live verfolgen |
| `docker compose logs -f hub-api` | Nur API Logs live |
| `docker compose logs -f mosquitto` | Nur MQTT Logs live |
| `docker compose logs --tail=100 hub-api` | Letzte 100 Zeilen |

### Container-Shell (Console)

```bash
# Shell im API-Container öffnen
docker exec -it myiotgrid-hub-api /bin/bash

# Falls bash nicht verfügbar (Alpine):
docker exec -it myiotgrid-hub-api /bin/sh

# Shell im Mosquitto-Container
docker exec -it myiotgrid-mosquitto /bin/sh
```

### Debugging im Container

```bash
# Laufende Prozesse anzeigen
docker exec myiotgrid-hub-api ps aux

# Umgebungsvariablen anzeigen
docker exec myiotgrid-hub-api env

# Dateien im Datenverzeichnis
docker exec myiotgrid-hub-api ls -la /app/data

# SQLite Datenbank prüfen
docker exec myiotgrid-hub-api ls -la /app/data/hub.db

# Netzwerk-Verbindungen
docker exec myiotgrid-hub-api netstat -tlnp
```

---

## Nützliche URLs

### API Endpunkte

| Endpunkt | Methode | Beschreibung |
|----------|---------|--------------|
| `/` | GET | API Info & Status |
| `/swagger` | GET | Swagger UI |
| `/health` | GET | Health Check |
| `/health/ready` | GET | Readiness Check |
| `/api/hubs` | GET | Alle Hubs |
| `/api/nodes` | GET | Alle Nodes |
| `/api/sensors` | GET | Alle Sensoren |
| `/api/readings` | GET | Messwerte |
| `/api/alerts` | GET | Aktive Alerts |
| `/api/sensortypes` | GET | Sensor-Typen |
| `/api/alerttypes` | GET | Alert-Typen |

### Quick Links

- **Swagger UI:** http://localhost:5001/swagger
- **API Status:** http://localhost:5001/
- **Health Check:** http://localhost:5001/health

---

## Volumes & Persistenz

### Docker Volumes

| Volume | Mount Point | Beschreibung |
|--------|-------------|--------------|
| `myiotgrid-hub-data` | `/app/data` | SQLite DB, Matter Credentials |
| `myiotgrid-hub-logs` | `/app/logs` | Application Logs |
| `myiotgrid-mosquitto-data` | `/mosquitto/data` | MQTT Persistenz |
| `myiotgrid-mosquitto-logs` | `/mosquitto/log` | MQTT Logs |

### Volumes verwalten

```bash
# Volumes auflisten
docker volume ls | grep myiotgrid

# Volume Details anzeigen
docker volume inspect myiotgrid-hub-data

# ACHTUNG: Volume löschen (Datenverlust!)
docker volume rm myiotgrid-hub-data
```

### Datenbank-Backup

```bash
# SQLite DB aus Container kopieren
docker cp myiotgrid-hub-api:/app/data/hub.db ./backup-hub.db

# Backup zurückspielen
docker cp ./backup-hub.db myiotgrid-hub-api:/app/data/hub.db
```

---

## MQTT Broker (Mosquitto)

### MQTT testen

```bash
# MQTT Client installieren (macOS)
brew install mosquitto

# Topic subscriben (in Terminal 1)
mosquitto_sub -h localhost -p 1883 -t "myiotgrid/#" -v

# Nachricht publishen (in Terminal 2)
mosquitto_pub -h localhost -p 1883 -t "myiotgrid/test" -m "Hello MQTT"
```

### MQTT Topics

| Topic | Richtung | Beschreibung |
|-------|----------|--------------|
| `myiotgrid/{tenantId}/readings` | Sensor → Hub | Neue Messwerte |
| `myiotgrid/{tenantId}/hubs/+/status` | Sensor → Hub | Hub Online/Offline |
| `myiotgrid/{tenantId}/nodes/+/status` | Sensor → Hub | Node Status |

---

## Troubleshooting

### Container startet nicht

```bash
# Logs prüfen
docker compose logs hub-api

# Container-Status prüfen
docker compose ps

# Container manuell starten für mehr Output
docker compose up hub-api
```

### Port bereits belegt

```bash
# Prüfen welcher Prozess den Port nutzt
lsof -i :5001
lsof -i :1883

# Prozess beenden (PID aus lsof)
kill -9 <PID>
```

### Datenbank-Probleme

```bash
# Datenbank löschen und neu erstellen
docker compose down
docker volume rm myiotgrid-hub-data
docker compose up -d
```

### Image neu bauen

```bash
# Nach Code-Änderungen
docker compose build hub-api
docker compose up -d hub-api

# Komplett neu bauen (ohne Cache)
docker compose build --no-cache
docker compose up -d
```

### Container-Ressourcen prüfen

```bash
# CPU/Memory Nutzung
docker stats

# Nur myIoTGrid Container
docker stats myiotgrid-hub-api myiotgrid-mosquitto
```

---

## Entwicklungs-Workflow

### Typischer Workflow

1. **Stack starten:**
   ```bash
   docker compose up -d
   ```

2. **Swagger öffnen:** http://localhost:5001/swagger

3. **Logs beobachten:**
   ```bash
   docker compose logs -f hub-api
   ```

4. **Nach Code-Änderungen neu bauen:**
   ```bash
   docker compose build hub-api && docker compose up -d hub-api
   ```

5. **Am Ende stoppen:**
   ```bash
   docker compose down
   ```

### Hot Reload (Alternative)

Für schnellere Entwicklung kann die API auch lokal gestartet werden:

```bash
# Nur Mosquitto in Docker
docker compose up -d mosquitto

# API lokal starten
cd src/myIoTGrid.Hub.Api
dotnet run
```

---

## Umgebungsvariablen

Die API kann über Umgebungsvariablen konfiguriert werden:

| Variable | Default | Beschreibung |
|----------|---------|--------------|
| `ASPNETCORE_ENVIRONMENT` | Development | Umgebung (Development/Production) |
| `ConnectionStrings__HubDb` | Data Source=/app/data/hub.db | SQLite Connection String |
| `Mqtt__Host` | mosquitto | MQTT Broker Hostname |
| `Mqtt__Port` | 1883 | MQTT Broker Port |

### Umgebungsvariablen überschreiben

```bash
# Via docker-compose.override.yml oder direkt:
docker compose up -d -e "ASPNETCORE_ENVIRONMENT=Production"
```

---

## Siehe auch

- [Hub Architektur](./hub-architektur.md)
- [API Dokumentation](http://localhost:5001/swagger)
- [MQTT Integration](./mqtt-integration.md)
- [Matter Bridge](./matter-bridge.md)

---

**myIoTGrid** - Open Source IoT Platform
*Made with ❤️ in Germany*
