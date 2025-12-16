# 🤖 Claude's Projekt-Übersicht - myIoTGrid.Phx

> **Erstellt:** 15. Dezember 2024  
> **Letzte Aktualisierung:** 15. Dezember 2024 - Wiki-Strategie definiert

---

## ⚠️ WICHTIG: Wiki-Erstellung Regeln

### 🚫 Was NICHT tun:
- ❌ Nichts erfinden oder spekulieren
- ❌ Keine vorgefertigten Seiten ohne Absprache
- ❌ Kein "Bla Bla" - nur Fakten
- ❌ Keine Beispiel-Inhalte ohne echte Basis

### ✅ Was TUN:
- ✅ Nur nach expliziter Absprache Seiten erstellen
- ✅ Alles im Quellcode kontrollieren
- ✅ Fakten aus vorhandenen Quellen verwenden:
  - GitHub Repository (Code, README, Docs)
  - Confluence myIoTGrid (Konzepte, Anleitungen)
  - Lokale Dateien im Projekt
- ✅ Seite für Seite erstellen
- ✅ Vor jeder Seite: Quellen prüfen und bestätigen

### 🎯 Wiki-Zweck:
**Dokumentation & Bedienungsanleitung für:**
1. Hardware (Aufbau, Spezifikationen)
2. Software (Installation, Konfiguration, API)
3. Hilfestellung (Troubleshooting)
4. Konzepte (aus Confluence übernehmen)
5. Anleitungen (aus Confluence übernehmen)

**Ziel:** Confluence ersetzen + praktische Dokumentation

---

## 📍 Datenquellen-Übersicht

### 1. 🌐 GitHub Repository
**URL:** https://github.com/Frankseins/myIoTGrid.Phx  
**Status:** ✅ Vollzugriff über web_fetch

**Wichtige Dateien:**
- `README.md` - Hauptdokumentation (Vision, Features, Architektur)
- `CLAUDE.md` - KI-Dokumentation
- `DOCKER.md` - Container-Setup
- `docs/raspberry-pi-deployment.md` - Deployment-Guide

**Verzeichnisse:**
```
├── myIoTGrid.Hub/              # .NET 8 Backend
├── myIoTGrid.Sensor/           # ESP32 Firmware (C++)
├── myIoTGrid.Gateway.LoRaWAN/  # LoRaWAN Bridge
├── myIoTGrid.MatterBridge/     # Smart Home Integration
├── myIoTGrid.Shared/           # Shared Libraries
├── myIoTGrid.Apps/             # Angular Frontend
├── docker/                     # Docker Configs
└── config/                     # Konfigurationsdateien
```

---

### 2. ☁️ Atlassian Confluence
**Space:** myIoTGrid + HackAThon  
**Cloud ID:** 6d463b70-8e34-4c5a-b49e-9787770c180c  
**Status:** ✅ Vollzugriff über Atlassian Tools

**Wichtige Spaces:**
- **myIoTGrid** - Hauptprojekt-Dokumentation
- **HackAThon** - Pascal Gymnasium SmartCity Hackathon (ABGESCHLOSSEN 12.-13.12.2024)

**Zentrale Seiten:**
- Sprint-Planung (Sprint 0 bis Sprint 17)
- Technische Dokumentation (LoRaWAN, GPS, Sensoren)
- PhX1-Projektbeschreibung (Erft-Monitoring)
- Hardware-Setup-Anleitungen
- Konzepte und Anleitungen → **Diese ins Wiki übertragen!**

**Zugriff:**
```
Atlassian:search - Volltextsuche
Atlassian:fetch - Seiten abrufen per ARI
Atlassian:getConfluencePage - Seiten mit Content
```

---

### 3. 💻 Lokales Dateisystem
**Pfad:** `/Users/frankbersch/RiderProjects/myIoTGrid.Phx`  
**Status:** ✅ Vollzugriff über Filesystem Tools

**Struktur:**
```
myIoTGrid.Phx/
├── Wiki/                          # GitHub Wiki (Git Submodul)
│   ├── .git/                     # Remote: github.com/Frankseins/myIoTGrid.Phx.wiki.git
│   └── [Wiki-Seiten werden hier erstellt]
│
├── myIoTGrid.Hub/                # Backend (.NET 8)
├── myIoTGrid.Sensor/             # Firmware (ESP32, C++)
├── myIoTGrid.Gateway.LoRaWAN/    # LoRaWAN Bridge
├── myIoTGrid.MatterBridge/       # Smart Home
├── myIoTGrid.Shared/             # Shared Code
├── myIoTGrid.Apps/               # Frontend (Angular)
├── docs/                         # Projekt-Dokumentation
├── docker/                       # Docker Configs
├── config/                       # Konfiguration
│
├── README.md                     # Hauptdokumentation
├── CLAUDE.md                     # KI-Dokumentation
├── DOCKER.md                     # Container-Guide
├── LICENSE                       # MIT License
├── docker-compose.yml            # Standard Setup
├── docker-compose.rpi.yml        # Raspberry Pi
└── docker-compose.lorawan.yml    # LoRaWAN Gateway
```

**Verfügbare Filesystem-Tools:**
```
Filesystem:list_directory         - Verzeichnisse auflisten
Filesystem:read_text_file         - Dateien lesen
Filesystem:write_file             - Dateien schreiben
Filesystem:directory_tree         - Baum-Ansicht
Filesystem:search_files           - Dateien suchen
```

---

## 🎯 Hackathon-Projekt: PhX1

### 📅 Event-Details (ABGESCHLOSSEN)
**Name:** "Pascal smartens up the city"  
**Datum:** 12.-13. Dezember 2024 (24h)  
**Ort:** Pascal-Gymnasium Grevenbroich  
**Partner:** Stadt Grevenbroich, NEW Energie, dataMatters GmbH

### 🚤 PhX1 - Pascal Hack Xplorer 1
**Konzept:** Schwimmendes IoT-Labor für die Erft

**Hardware:**
- 🍾 PET-Flaschen als Schwimmkörper
- 📡 QIQIAZI Meshtastic LoRa V3 ESP32 (Dual-Core, OLED)
- 🌡️ DS18B20 Wassertemperatur (wasserdicht)
- 📏 JSN-SR04T Ultraschall (Tiefe)
- 💧 Turbidity Sensor (Trübung)
- 🛰️ NEO-6M GPS
- 🌤️ GY-BME280 (Temp, Humidity, Pressure)
- 💡 BH1750 Lichtsensor
- 💾 SD-Karten-Modul

**Infrastruktur:**
- LoRaWAN-Netz (Schule)
- Waveshare SX1302 Gateway (Raspberry Pi 5)
- ChirpStack Network Server
- myIoTGrid.Hub für Datenverarbeitung
- Live-Dashboard mit Google Maps

---

## 🏗️ Architektur-Übersicht

### System-Komponenten

```
┌─────────────────┐
│  ESP32 Sensor   │  ESP32 + Sensoren
│  (WiFi/LoRa)    │
└────────┬────────┘
         │ WiFi: MQTT / LoRa: LoRaWAN
         ↓
┌─────────────────┐
│  Gateway        │  Raspberry Pi (nur LoRaWAN)
│  (optional)     │  ChirpStack
└────────┬────────┘
         │ MQTT
         ↓
┌─────────────────┐
│  Grid.Hub       │  .NET 8 Backend
│  (Backend)      │  SQLite DB
└────────┬────────┘
         │ SignalR/HTTP
         ↓
┌─────────────────┐
│  Dashboard      │  Angular Frontend
│  (Frontend)     │
└─────────────────┘
```

### Tech Stack
**Backend:**
- .NET 8 (C#)
- SQLite (lokal)
- MQTT (Mosquitto)
- SignalR (Realtime)

**Frontend:**
- Angular 18+
- TypeScript
- Material Design

**Firmware:**
- C++ (PlatformIO)
- Arduino Framework
- ESP32
- LoRaWAN (LMIC)

**Infrastructure:**
- Docker + Docker Compose
- ChirpStack (LoRaWAN Server)
- Ubuntu Server 24.04

---

## 📋 Aktuelle Sprint-Übersicht

### Sprint Status
**Letzter Sprint vor Hackathon:** Sprint 17 - Node-Detail UI Bugfixes  
**Hackathon:** 12.-13. Dezember 2024 - ✅ ABGESCHLOSSEN

---

## 🔍 Wichtige Such-Befehle

### Confluence durchsuchen
```javascript
Atlassian:search({ query: "suchbegriff" })
Atlassian:getConfluencePage({ cloudId: "...", pageId: "..." })
```

### Lokale Dateien finden
```javascript
Filesystem:search_files({ 
  path: "/Users/frankbersch/RiderProjects/myIoTGrid.Phx",
  pattern: "suchbegriff"
})

Filesystem:read_text_file({
  path: "/Users/frankbersch/RiderProjects/myIoTGrid.Phx/..."
})
```

### Code-Verzeichnisse
```javascript
Filesystem:directory_tree({ 
  path: "/Users/frankbersch/RiderProjects/myIoTGrid.Phx/myIoTGrid.Hub"
})
```

---

## 📝 Wiki-Erstellung Workflow

### Vor jeder neuen Seite:

1. **Mit User absprechen**
   - Was soll auf die Seite?
   - Welche Quellen nutzen?

2. **Quellen prüfen**
   - GitHub Code checken
   - Confluence durchsuchen
   - Lokale Dateien lesen
   - Nichts erfinden!

3. **Inhalte sammeln**
   - Fakten aus Code extrahieren
   - Confluence-Inhalte übernehmen
   - Screenshots/Diagramme identifizieren

4. **Seite erstellen**
   - Nur verifizierte Informationen
   - Technisch korrekt
   - Keine Spekulation

5. **User-Review**
   - Seite zeigen
   - Feedback einarbeiten
   - Erst dann finalisieren

---

## 📚 Geplante Wiki-Struktur

### Bereiche (nach Absprache zu füllen):
1. **Konzept** - Aus Confluence übernehmen
2. **Dokumentation** - Aus Code/Confluence
3. **Anleitungen** - Aus Confluence übernehmen
4. **FAQ** - Aus Erfahrung/Issues

**Wichtig:** Jede Seite nur nach Absprache und mit verifizierten Inhalten!

---

## 🔗 Wichtige Links

### Repositories
- **Hauptprojekt:** https://github.com/Frankseins/myIoTGrid.Phx
- **Wiki:** https://github.com/Frankseins/myIoTGrid.Phx.wiki

### Confluence
- **myIoTGrid Space:** https://myiotgrid.atlassian.net/wiki/spaces/myIoTGrid/overview
- **Hackathon Space:** https://myiotgrid.atlassian.net/wiki/spaces/HackAThon/overview

---

## 🤖 KI-Integration

### Claude's Rolle im Projekt
- **Code-Analyse:** Quellcode verstehen und dokumentieren
- **Dokumentation:** Wiki-Seiten aus verifizierten Quellen erstellen
- **Confluence-Migration:** Inhalte strukturiert übertragen
- **Keine Spekulation:** Nur Fakten, keine Erfindungen

### Workflow-Prinzip
```
1. User fragt nach Seite
   ↓
2. Claude prüft Quellen (Code/Confluence/Docs)
   ↓
3. Claude zeigt gefundene Inhalte
   ↓
4. User bestätigt/korrigiert
   ↓
5. Claude erstellt Seite mit verifizierten Fakten
```

---

**💡 Erinnerung für Claude:**  
Immer ERST Quellen prüfen, DANN User fragen, DANN (und nur dann) erstellen!

**📝 Updates:**  
Diese Datei bei jeder strukturellen Änderung aktualisieren.

---

*Erstellt von Claude für Claude* 🤖  
*Letzte Aktualisierung: 15. Dezember 2024 - Wiki-Regeln hinzugefügt*
