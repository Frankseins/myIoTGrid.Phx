# myIoTGrid - Projektstruktur

**Version:** 3.0
**Stand:** 9. Dezember 2025

---

## Übersicht

Das myIoTGrid-Projekt folgt einer **Two-Tier Architektur** mit klarer Trennung zwischen:

1. **Shared Libraries** (`myIoTGrid.Shared/`) - Projekt-übergreifende Komponenten
2. **Komponenten-Projekte** (`myIoTGrid.Hub/`, `myIoTGrid.Apps/`, etc.) - Spezifische Implementierungen

---

## Verzeichnisstruktur

```
myIoTGrid/
│
├── 📁 myIoTGrid.Shared/              → Shared Libraries (Backend)
│   ├── myIoTGrid.Shared.Common/      → Entities, DTOs, Enums, Constants
│   ├── myIoTGrid.Shared.Contracts/   → Interfaces, Service Contracts
│   ├── myIoTGrid.Shared.Utilities/   → Helper-Klassen, Extensions
│   └── Tests/
│       └── myIoTGrid.Shared.Common.Tests/
│
├── 📁 myIoTGrid.Hub/                 → Hub Backend (.NET 10)
│   ├── src/
│   │   ├── myIoTGrid.Hub.Api/        → Startup, Program.cs, Composition Root
│   │   ├── myIoTGrid.Hub.Domain/     → Hub-spezifische Domain-Logik
│   │   ├── myIoTGrid.Hub.Shared/     → Hub-spezifische DTOs (re-exportiert Shared)
│   │   ├── myIoTGrid.Hub.Service/    → Business Logic, Service Implementierungen
│   │   ├── myIoTGrid.Hub.Infrastructure/ → EF Core, DbContext, Repositories
│   │   └── myIoTGrid.Hub.Interface/  → Controllers, SignalR Hubs, Middleware
│   ├── tests/
│   ├── docs/
│   └── myIoTGrid.Hub.sln
│
├── 📁 myIoTGrid.Apps/                → Frontend (Angular 21, Nx Monorepo)
│   ├── apps/
│   │   └── hub-frontend/             → Hub Frontend App
│   ├── libs/                         → Shared Angular Libraries
│   ├── docker/
│   │   └── Dockerfile
│   ├── docs/
│   └── package.json
│
├── 📁 myIoTGrid.Sensor/              → ESP32 Firmware (PlatformIO)
│   ├── src/
│   ├── docker/
│   │   └── Dockerfile                → Sensor Simulator
│   ├── docs/
│   └── platformio.ini
│
├── 📁 myIoTGrid.Cloud/               → Cloud Backend (zukünftig)
│
├── 📁 myIoTGrid.Docs/                → Projekt-Dokumentation
│
├── 📁 docker/                        → Docker-Konfigurationen
│   └── mosquitto/
│       └── mosquitto.conf
│
├── 📁 .github/
│   └── workflows/
│       └── ci-cd.yml                 → CI/CD Pipeline
│
├── docker-compose.yml                → Lokaler Stack
├── CLAUDE.md                         → AI Development Guide
├── LICENSE
└── README.md
```

---

## Shared Libraries Detail

### myIoTGrid.Shared.Common

**Enthält:** Entities, DTOs, Enums, Constants, Value Objects

```
myIoTGrid.Shared.Common/
├── Constants/
│   ├── SensorTypeConstants.cs        → Vordefinierte Sensor-Typ-Codes
│   └── AlertTypeConstants.cs         → Vordefinierte Alert-Typ-Codes
├── DTOs/
│   ├── SensorDataDto.cs
│   ├── HubDto.cs
│   ├── AlertDto.cs
│   ├── LocationDto.cs
│   └── PaginatedResultDto.cs
├── Entities/
│   ├── Tenant.cs
│   ├── Hub.cs
│   ├── SensorData.cs
│   ├── SensorType.cs
│   ├── Alert.cs
│   └── AlertType.cs
├── Enums/
│   ├── Protocol.cs
│   ├── AlertLevel.cs
│   └── AlertSource.cs
└── ValueObjects/
    └── Location.cs
```

### myIoTGrid.Shared.Contracts

**Enthält:** Service Interfaces, Repository Interfaces

```
myIoTGrid.Shared.Contracts/
├── Services/
│   ├── ISensorDataService.cs
│   ├── IHubService.cs
│   ├── IAlertService.cs
│   ├── ISensorTypeService.cs
│   ├── IAlertTypeService.cs
│   ├── ITenantService.cs
│   ├── ICloudSyncService.cs
│   └── IMatterBridgeService.cs
└── Repositories/
    ├── ISensorDataRepository.cs
    ├── IHubRepository.cs
    └── IAlertRepository.cs
```

### myIoTGrid.Shared.Utilities

**Enthält:** Extensions, Helpers, Mapping

```
myIoTGrid.Shared.Utilities/
├── Extensions/
│   ├── EntityExtensions.cs           → ToDto() Mappings
│   ├── DateTimeExtensions.cs
│   └── StringExtensions.cs
└── Helpers/
    ├── JsonHelper.cs
    └── ValidationHelper.cs
```

---

## Hub Backend Detail

### Layer-Abhängigkeiten

```
                    ┌─────────────────────┐
                    │        Api          │  ← Composition Root
                    └──────────┬──────────┘
                               │
              ┌────────────────┼────────────────┐
              ▼                ▼                ▼
    ┌─────────────────┐ ┌─────────────┐ ┌─────────────────┐
    │   Interface     │ │   Service   │ │ Infrastructure  │
    └────────┬────────┘ └──────┬──────┘ └────────┬────────┘
             │                 │                 │
             └─────────────────┼─────────────────┘
                               ▼
                    ┌─────────────────────┐
                    │       Domain        │
                    └──────────┬──────────┘
                               ▼
                    ┌─────────────────────┐
                    │   Shared.Common     │  ← Entities, DTOs
                    │   Shared.Contracts  │  ← Interfaces
                    │   Shared.Utilities  │  ← Extensions
                    └─────────────────────┘
```

### Projekt-Referenzen

| Projekt | Referenziert |
|---------|--------------|
| **Hub.Api** | Interface, Service, Infrastructure, Hub.Shared |
| **Hub.Interface** | Service, Domain, Hub.Shared |
| **Hub.Service** | Domain, Hub.Shared, Shared.Contracts |
| **Hub.Infrastructure** | Domain, Hub.Shared |
| **Hub.Domain** | Hub.Shared |
| **Hub.Shared** | Shared.Common, Shared.Contracts, Shared.Utilities |

---

## Was liegt wo?

### Entities (Daten-Modelle)

| Typ | Ort | Beispiel |
|-----|-----|----------|
| **Shared Entities** | `Shared.Common/Entities/` | Tenant, Hub, SensorData, Alert |
| **Hub-spezifische** | `Hub.Domain/Entities/` | (falls benötigt) |

### DTOs (Data Transfer Objects)

| Typ | Ort | Beispiel |
|-----|-----|----------|
| **Shared DTOs** | `Shared.Common/DTOs/` | SensorDataDto, HubDto, AlertDto |
| **Hub-spezifische** | `Hub.Shared/DTOs/` | (falls benötigt) |

### Interfaces

| Typ | Ort | Beispiel |
|-----|-----|----------|
| **Service Interfaces** | `Shared.Contracts/Services/` | ISensorDataService |
| **Repository Interfaces** | `Shared.Contracts/Repositories/` | ISensorDataRepository |

### Implementierungen

| Typ | Ort | Beispiel |
|-----|-----|----------|
| **Services** | `Hub.Service/Services/` | SensorDataService |
| **Repositories** | `Hub.Infrastructure/Repositories/` | SensorDataRepository |
| **Controllers** | `Hub.Interface/Controllers/` | SensorDataController |
| **SignalR Hubs** | `Hub.Interface/Hubs/` | SensorHub |

### Enums & Constants

| Typ | Ort |
|-----|-----|
| **Enums** | `Shared.Common/Enums/` |
| **Constants** | `Shared.Common/Constants/` |

---

## Docker Images

| Image | Dockerfile | Plattformen |
|-------|------------|-------------|
| `myiotgrid-hub-api` | `myIoTGrid.Hub/src/myIoTGrid.Hub.Api/Dockerfile` | amd64, arm64 |
| `myiotgrid-hub-frontend` | `myIoTGrid.Apps/docker/Dockerfile` | amd64, arm64 |
| `myiotgrid-sensor-sim` | `myIoTGrid.Sensor/docker/Dockerfile` | amd64, arm64 |

---

## Build-Befehle

### Backend (Shared + Hub)

```bash
# Shared Libraries bauen
cd myIoTGrid.Shared
dotnet build

# Hub Backend bauen
cd myIoTGrid.Hub
dotnet build

# Tests ausführen
dotnet test
```

### Frontend

```bash
cd myIoTGrid.Apps
npm ci
npm run build
```

### Docker Stack

```bash
# Alle Images bauen und starten
docker-compose up -d --build

# Logs anzeigen
docker-compose logs -f

# Stack stoppen
docker-compose down
```

---

## CI/CD Pipeline

Die GitHub Actions Pipeline (`.github/workflows/ci-cd.yml`) führt folgende Jobs aus:

1. **build-and-test-backend** - .NET Build & Tests
2. **build-and-test-frontend** - Angular Build
3. **docker-hub-api** - API Docker Image (amd64 + arm64)
4. **docker-hub-frontend** - Frontend Docker Image (amd64 + arm64)
5. **docker-sensor-sim** - Sensor Simulator Image (amd64 + arm64)
6. **summary** - Build-Zusammenfassung

### Trigger

- Push auf `main`, `test*`, `beta*` Branches
- Pull Requests auf `main`

---

## Wichtige Dateien

| Datei | Beschreibung |
|-------|--------------|
| `CLAUDE.md` | AI Development Guide (Version 3.0) |
| `docker-compose.yml` | Lokaler Docker Stack |
| `.github/workflows/ci-cd.yml` | CI/CD Pipeline |
| `myIoTGrid.Hub/myIoTGrid.Hub.sln` | Hub Solution |

---

## Weitere Dokumentation

- **Hub Docker Guide:** `myIoTGrid.Hub/docs/confluence-docker-guide.md`
- **CI/CD Details:** `myIoTGrid.Hub/docs/CI-CD-Pipeline.md`
- **Frontend Pattern Guide:** `myIoTGrid.Apps/docs/FEATURE_PATTERN_GUIDE.md`
- **Firmware Docs:** `myIoTGrid.Sensor/docs/FIRMWARE_DOCUMENTATION.md`

---

*myIoTGrid - Open Source · Privacy First · Cloud-KI*
