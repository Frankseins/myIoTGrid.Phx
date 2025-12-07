# myIoTGrid - Claude Code Memory & Style Guide

**Version:** 2.0
**Letzte Aktualisierung:** 27. November 2025
**Projekt:** myIoTGrid - Open-Source IoT-Plattform für Sensordaten
**Technologie:** .NET 10 Backend + Angular 21 Frontend + ESP32 Firmware

---

## 🎯 PROJEKT-ÜBERSICHT

myIoTGrid ist eine Open-Source IoT-Plattform für Sensordaten mit Cloud-KI:

- **Grid.Hub** - Raspberry Pi als lokales Gateway (Datensammlung, lokale Speicherung)
- **Grid.Cloud** - Cloud für KI-Analyse und Community Intelligence
- **Grid.Sensor** - ESP32-basierte Sensoren (Temperatur, CO₂, Feinstaub, etc.)
- **Smart Home** - Integration via Matter (Apple, Google, Alexa)

### Kernprinzipien
- **Local First** - Volle Funktionalität auch offline (mit lokalen Fallback-Regeln)
- **Privacy by Design** - Sensoren sind standardmäßig privat
- **Cloud-KI** - KI-Analyse in der Cloud, Alerts werden an Hub gesendet
- **Open Source** - MIT License, für immer frei

### Architektur-Übersicht

```
┌─────────────────────────────────────────────────────────────────┐
│                         GRID.HUB                                │
│                    "Das lokale Gateway"                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────┐     ┌─────────────────────────────────────┐  │
│   │   SENSOR    │────▶│         GRID.HUB                    │  │
│   │   (ESP32)   │MQTT │                                     │  │
│   │   📶 WLAN   │ or  │  ┌─────────┐  ┌─────────┐          │  │
│   └─────────────┘REST │  │  .NET   │  │ Angular │          │  │
│                       │  │   API   │  │   Web   │          │  │
│   ┌─────────────┐     │  └────┬────┘  └────┬────┘          │  │
│   │   SENSOR    │────▶│       │            │               │  │
│   │  (LoRa32)   │     │  ┌────┴────────────┴────┐          │  │
│   │   📡 LoRa   │     │  │       SQLite         │          │  │
│   └─────────────┘     │  │    (lokale Daten)    │          │  │
│                       │  └──────────────────────┘          │  │
│                       │              │                      │  │
│                       └──────────────┼──────────────────────┘  │
│                                      │                         │
│                                      ▼                         │
│                       ┌──────────────────────────┐             │
│                       │       GRID.CLOUD         │             │
│                       │    🤖 KI-Analyse         │             │
│                       │    📊 Langzeitspeicher   │             │
│                       └──────────────────────────┘             │
│                                      │                         │
│                                      ▼                         │
│                       ┌──────────────────────────┐             │
│                       │      SMART HOME          │             │
│                       │  🏠 Apple · Google · Alexa│             │
│                       └──────────────────────────┘             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### KI-Architektur (WICHTIG!)

**Die KI läuft in der Cloud, NICHT lokal auf dem Hub!**

```
Hub ────SensorData────▶ Grid.Cloud
                            │
                            ▼
                       🤖 KI Analyse
                       (ML.NET, ONNX)
                            │
Hub ◀────KI-Alert──── Grid.Cloud
 │
 └──▶ Smart Home (Apple/Google/Alexa)
```

---

## 🏗️ PROJEKTSTRUKTUR

```
myIoTGrid/
├── myIoTGrid.Cloud/              → Cloud-Backend (.NET 10, PostgreSQL)
├── myIoTGrid.Docs/               → Dokumentation
├── myIoTGrid.Hub/
│   └── myIoTGrid.Hub.Backend/
│       ├── src/
│       │   ├── myIoTGrid.Hub.Api/
│       │   ├── myIoTGrid.Hub.Domain/
│       │   ├── myIoTGrid.Hub.Shared/
│       │   ├── myIoTGrid.Hub.Service/
│       │   ├── myIoTGrid.Hub.Infrastructure/
│       │   └── myIoTGrid.Hub.Interface/
│       ├── tests/
│       └── docker/
│   └── myIoTGrid.Hub.Frontend/   → Hub-Frontend (Angular 21)
├── myIoTGrid.Sensor/             → ESP32 Firmware
├── .gitignore
├── LICENSE
└── README.md
```

---

## 🧠 HUB BACKEND ARCHITEKTUR (.NET 10)

### Clean Architecture Layers

```
myIoTGrid.Hub.Backend/src/
├── myIoTGrid.Hub.Api/              → Startup, Program.cs, Composition Root
├── myIoTGrid.Hub.Domain/           → Entities, Enums, Interfaces
├── myIoTGrid.Hub.Shared/           → DTOs, Constants, Extensions
├── myIoTGrid.Hub.Service/          → Business Logic, Service Interfaces
├── myIoTGrid.Hub.Infrastructure/   → EF Core, DbContext, Repositories, MQTT
└── myIoTGrid.Hub.Interface/        → Controllers, SignalR Hubs, Middleware
```

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
                    │       Shared        │
                    └─────────────────────┘
```

### Projekt-Referenzen

| Projekt | Referenziert |
|---------|--------------|
| Api | Interface, Service, Infrastructure, Shared |
| Interface | Service, Domain, Shared |
| Service | Domain, Shared |
| Infrastructure | Domain, Shared |
| Domain | Shared |
| Shared | (keine) |

---

## 🚨 KRITISCHE ARCHITEKTUR-REGELN (HÖCHSTE PRIORITÄT!)

### 1. Entities gehören IMMER ins Domain-Projekt

```csharp
// ✅ RICHTIG: myIoTGrid.Hub.Domain/Entities/Hub.cs
namespace myIoTGrid.Hub.Domain.Entities;

public class Hub
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string HubId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Protocol Protocol { get; set; }
    public Location? DefaultLocation { get; set; }
    public DateTime? LastSeen { get; set; }
    public bool IsOnline { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public Tenant? Tenant { get; set; }
    public ICollection<SensorData> SensorData { get; set; } = new List<SensorData>();
}

// ❌ FALSCH: Entity im Service-Projekt definieren
// myIoTGrid.Hub.Service/Entities/Hub.cs ← VERBOTEN!
```

### 2. DTOs gehören IMMER ins Shared-Projekt

```csharp
// ✅ RICHTIG: myIoTGrid.Hub.Shared/DTOs/SensorDataDto.cs
namespace myIoTGrid.Hub.Shared.DTOs;

public record SensorDataDto(
    Guid Id,
    Guid TenantId,
    Guid HubId,
    Guid SensorTypeId,
    string SensorTypeCode,
    string SensorTypeName,
    string Unit,
    double Value,
    DateTime Timestamp,
    LocationDto? Location,
    bool IsSyncedToCloud
);

public record CreateSensorDataDto(
    string HubId,
    string SensorType,
    double Value,
    LocationDto? Location = null
);

// ❌ FALSCH: DTO im Service-Projekt definieren
// myIoTGrid.Hub.Service/DTOs/SensorDataDto.cs ← VERBOTEN!
```

### 3. Services konsumieren DTOs aus Shared (NIEMALS selbst definieren)

```csharp
// ✅ RICHTIG: myIoTGrid.Hub.Service/Services/SensorDataService.cs
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Domain.Entities;

namespace myIoTGrid.Hub.Service.Services;

public class SensorDataService : ISensorDataService
{
    public async Task<SensorDataDto> CreateAsync(CreateSensorDataDto dto, CancellationToken ct = default)
    {
        // DTO aus Shared verwenden
    }
}

// ❌ FALSCH: DTO im Service definieren
public class SensorDataService
{
    public class SensorDataDto { } // ← VERBOTEN!
}
```

### 4. Controllers und Hubs gehören ins Interface-Projekt

```csharp
// ✅ RICHTIG: myIoTGrid.Hub.Interface/Controllers/SensorDataController.cs
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Interface.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorDataController : ControllerBase
{
    private readonly ISensorDataService _sensorDataService;
    private readonly IHubContext<SensorHub> _hubContext;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSensorDataDto dto, CancellationToken ct)
    {
        var sensorData = await _sensorDataService.CreateAsync(dto, ct);

        // SignalR Broadcast
        await _hubContext.Clients.All.SendAsync("NewSensorData", sensorData, ct);

        return CreatedAtAction(nameof(GetById), new { id = sensorData.Id }, sensorData);
    }
}
```

### 5. SignalR Hub für Echtzeit-Updates

```csharp
// ✅ RICHTIG: myIoTGrid.Hub.Interface/Hubs/SensorHub.cs
using Microsoft.AspNetCore.SignalR;

namespace myIoTGrid.Hub.Interface.Hubs;

public class SensorHub : Hub
{
    public async Task JoinHubGroup(string hubId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, hubId);
    }

    public async Task LeaveHubGroup(string hubId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, hubId);
    }

    public async Task JoinAlertGroup(string alertLevel)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"alerts:{alertLevel}");
    }
}
```

### 6. Async/Await ist PFLICHT

```csharp
// ✅ RICHTIG: Alles asynchron mit CancellationToken
public async Task<SensorDataDto> CreateAsync(CreateSensorDataDto dto, CancellationToken ct = default)
{
    var hub = await _hubService.GetOrCreateByHubIdAsync(dto.HubId, ct);
    var sensorType = await _sensorTypeService.GetByCodeAsync(dto.SensorType, ct);

    var sensorData = new SensorData
    {
        TenantId = _tenantService.GetCurrentTenantId(),
        HubId = hub.Id,
        SensorTypeId = sensorType.Id,
        Value = dto.Value,
        Timestamp = DateTime.UtcNow,
        Location = dto.Location?.ToEntity()
    };

    _context.SensorData.Add(sensorData);
    await _context.SaveChangesAsync(ct);

    return sensorData.ToDto(sensorType);
}

// ❌ FALSCH: Synchrone Operationen
public SensorDataDto Create(CreateSensorDataDto dto)
{
    // VERBOTEN!
}
```

### 7. VOR dem Erstellen IMMER prüfen: Existiert die Klasse bereits?

```bash
# Suche in allen Projekten
grep -r "class SensorDataDto" myIoTGrid*/

# Oder in Rider: Ctrl+Shift+F (Find in Files)
# Suche nach: "class [ClassName]"
```

**NIEMALS Duplikate erstellen!** → Führt zu Build-Errors und Architektur-Chaos

---

## 📊 DATENMODELL (Multi-Tenant)

### Entity-Beziehungen

```
TENANT (1)
    │
    ├──▶ HUB (n)
    │       │
    │       └──▶ SENSOR_DATA (n)
    │               │
    │               └──▶ SENSOR_TYPE (1)
    │
    └──▶ ALERT (n)
            │
            └──▶ ALERT_TYPE (1)

SENSOR_TYPE ◀──── Cloud Sync ────▶ Grid.Cloud
ALERT_TYPE  ◀──── Cloud Sync ────▶ Grid.Cloud
```

### Entities

#### Tenant
```csharp
// myIoTGrid.Hub.Domain/Entities/Tenant.cs
public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CloudApiKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Hub> Hubs { get; set; } = new List<Hub>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
```

#### Hub (Sensor-Gerät)
```csharp
// myIoTGrid.Hub.Domain/Entities/Hub.cs
public class Hub
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string HubId { get; set; } = string.Empty;     // z.B. "sensor-wohnzimmer-01"
    public string Name { get; set; } = string.Empty;
    public Protocol Protocol { get; set; }                 // WLAN, LoRaWAN
    public Location? DefaultLocation { get; set; }
    public DateTime? LastSeen { get; set; }
    public bool IsOnline { get; set; }
    public string? Metadata { get; set; }                  // JSON für zusätzliche Infos
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Tenant? Tenant { get; set; }
    public ICollection<SensorData> SensorData { get; set; } = new List<SensorData>();
}
```

#### SensorData (Messwert)
```csharp
// myIoTGrid.Hub.Domain/Entities/SensorData.cs
public class SensorData
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid HubId { get; set; }
    public Guid SensorTypeId { get; set; }
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public Location? Location { get; set; }                // Kann vom Hub abweichen!
    public bool IsSyncedToCloud { get; set; }

    // Navigation
    public Hub? Hub { get; set; }
    public SensorType? SensorType { get; set; }
}
```

#### SensorType (Cloud-synced)
```csharp
// myIoTGrid.Hub.Domain/Entities/SensorType.cs
public class SensorType
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;       // z.B. "temperature"
    public string Name { get; set; } = string.Empty;       // z.B. "Temperatur"
    public string Unit { get; set; } = string.Empty;       // z.B. "°C"
    public string? Description { get; set; }
    public string? IconName { get; set; }                  // Material Icon
    public bool IsGlobal { get; set; }                     // Von Cloud definiert
    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<SensorData> SensorData { get; set; } = new List<SensorData>();
}
```

#### Alert (KI-Warnung von Cloud)
```csharp
// myIoTGrid.Hub.Domain/Entities/Alert.cs
public class Alert
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? HubId { get; set; }
    public Guid AlertTypeId { get; set; }
    public AlertLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
    public AlertSource Source { get; set; }                // Local oder Cloud
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Tenant? Tenant { get; set; }
    public Hub? Hub { get; set; }
    public AlertType? AlertType { get; set; }
}
```

#### AlertType (Cloud-synced)
```csharp
// myIoTGrid.Hub.Domain/Entities/AlertType.cs
public class AlertType
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;       // z.B. "mold_risk"
    public string Name { get; set; } = string.Empty;       // z.B. "Schimmelrisiko"
    public string? Description { get; set; }
    public AlertLevel DefaultLevel { get; set; }
    public string? IconName { get; set; }
    public bool IsGlobal { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
```

#### Location (Value Object)
```csharp
// myIoTGrid.Hub.Domain/ValueObjects/Location.cs
public class Location
{
    public string? Name { get; set; }                      // z.B. "Wohnzimmer"
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
```

### Enums

```csharp
// myIoTGrid.Hub.Domain/Enums/Protocol.cs
public enum Protocol
{
    Unknown = 0,
    WLAN = 1,
    LoRaWAN = 2
}

// myIoTGrid.Hub.Domain/Enums/AlertLevel.cs
public enum AlertLevel
{
    Ok = 0,       // 🟢 Alles optimal
    Info = 1,     // 🔵 Hinweis/Tipp
    Warning = 2,  // 🟡 Warnung
    Critical = 3  // 🔴 Kritisch
}

// myIoTGrid.Hub.Domain/Enums/AlertSource.cs
public enum AlertSource
{
    Local = 0,    // Lokale Regel (z.B. Hub offline)
    Cloud = 1     // KI-Analyse aus Cloud
}
```

### Warum Location in SensorData?

| Szenario | Hub Location | SensorData Location |
|----------|--------------|---------------------|
| Fester Sensor | "Wohnzimmer" | "Wohnzimmer" (geerbt) |
| Mobiler Sensor | "Garten" (Default) | "Gewächshaus" (aktuell) |
| GPS-Tracker | "Auto" (Default) | 50.9375, 6.9603 (GPS) |

---

## 🔧 DIE 9 MODULE

```
┌─────────────────────────────────────────────────────────────────┐
│                      GRID.HUB MODULE                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐          │
│   │ 1. REST API │   │ 2. MQTT     │   │ 3. SignalR  │          │
│   │   Handler   │   │   Handler   │   │    Hub      │          │
│   └──────┬──────┘   └──────┬──────┘   └──────┬──────┘          │
│          │                 │                 │                  │
│          └─────────────────┼─────────────────┘                  │
│                            ▼                                    │
│   ┌─────────────────────────────────────────────────────────┐  │
│   │              4. SENSOR DATA SERVICE                      │  │
│   │         (Validation, Storage, Broadcast)                 │  │
│   └─────────────────────────┬───────────────────────────────┘  │
│                             │                                   │
│          ┌──────────────────┼──────────────────┐               │
│          ▼                  ▼                  ▼               │
│   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐          │
│   │ 5. Hub      │   │ 6. Sensor   │   │ 7. Alert    │          │
│   │   Service   │   │Type Service │   │   Service   │          │
│   └─────────────┘   └─────────────┘   └─────────────┘          │
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐  │
│   │                  8. CLOUD SYNC SERVICE                   │  │
│   │     (Upload SensorData, Download Alerts & Types)         │  │
│   └─────────────────────────────────────────────────────────┘  │
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐  │
│   │                  9. MATTER BRIDGE SERVICE                │  │
│   │          (Smart Home: Apple, Google, Alexa)              │  │
│   └─────────────────────────────────────────────────────────┘  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Service Interfaces

```csharp
// myIoTGrid.Hub.Service/Interfaces/

public interface ISensorDataService
{
    Task<SensorDataDto> CreateAsync(CreateSensorDataDto dto, CancellationToken ct = default);
    Task<SensorDataDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PaginatedResultDto<SensorDataDto>> GetFilteredAsync(SensorDataFilterDto filter, CancellationToken ct = default);
    Task<IEnumerable<SensorDataDto>> GetLatestByHubAsync(Guid hubId, CancellationToken ct = default);
}

public interface IHubService
{
    Task<HubDto> GetOrCreateByHubIdAsync(string hubId, CancellationToken ct = default);
    Task<IEnumerable<HubDto>> GetAllAsync(CancellationToken ct = default);
    Task<HubDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<HubDto> UpdateAsync(Guid id, UpdateHubDto dto, CancellationToken ct = default);
    Task UpdateLastSeenAsync(Guid id, CancellationToken ct = default);
}

public interface IAlertService
{
    Task<AlertDto> CreateFromCloudAsync(CreateAlertDto dto, CancellationToken ct = default);
    Task<IEnumerable<AlertDto>> GetActiveAsync(CancellationToken ct = default);
    Task<AlertDto> AcknowledgeAsync(Guid id, CancellationToken ct = default);
    Task CreateHubOfflineAlertAsync(Guid hubId, CancellationToken ct = default);
}

public interface ISensorTypeService
{
    Task<IEnumerable<SensorTypeDto>> GetAllAsync(CancellationToken ct = default);
    Task<SensorTypeDto?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task SyncFromCloudAsync(CancellationToken ct = default);
}

public interface IAlertTypeService
{
    Task<IEnumerable<AlertTypeDto>> GetAllAsync(CancellationToken ct = default);
    Task<AlertTypeDto?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task SyncFromCloudAsync(CancellationToken ct = default);
}

public interface ITenantService
{
    Guid GetCurrentTenantId();
    void SetCurrentTenantId(Guid tenantId);
    Task EnsureDefaultTenantAsync(CancellationToken ct = default);
}

public interface ICloudSyncService
{
    Task UploadSensorDataAsync(SensorDataDto data, CancellationToken ct = default);
    Task UploadBatchAsync(IEnumerable<SensorDataDto> data, CancellationToken ct = default);
    Task ConnectAsync(CancellationToken ct = default);
    bool IsConnected { get; }
    event Action<AlertDto> OnAlertReceived;
}

public interface IMatterBridgeService
{
    Task RegisterSensorAsync(HubDto hub, CancellationToken ct = default);
    Task PublishValueAsync(SensorDataDto data, CancellationToken ct = default);
    Task PublishAlertAsync(AlertDto alert, CancellationToken ct = default);
}
```

---

## 🌐 API-ENDPUNKTE

### SensorData API

| Methode | Endpoint | Beschreibung |
|---------|----------|--------------|
| `POST` | `/api/sensordata` | Neuen Messwert speichern |
| `GET` | `/api/sensordata` | Messwerte filtern |
| `GET` | `/api/sensordata/latest` | Letzte Werte pro Hub |
| `GET` | `/api/sensordata/{id}` | Einzelner Messwert |

### Hubs API

| Methode | Endpoint | Beschreibung |
|---------|----------|--------------|
| `GET` | `/api/hubs` | Alle registrierten Hubs |
| `GET` | `/api/hubs/{id}` | Hub-Details |
| `PUT` | `/api/hubs/{id}` | Hub aktualisieren |

### Alerts API

| Methode | Endpoint | Beschreibung |
|---------|----------|--------------|
| `GET` | `/api/alerts` | Aktive Alerts |
| `POST` | `/api/alerts/{id}/acknowledge` | Alert quittieren |
| `POST` | `/api/alerts/receive` | Alert von Cloud empfangen |

### SensorTypes API

| Methode | Endpoint | Beschreibung |
|---------|----------|--------------|
| `GET` | `/api/sensortypes` | Alle Sensor-Typen |
| `POST` | `/api/sensortypes` | Neuen Typ anlegen |

### AlertTypes API

| Methode | Endpoint | Beschreibung |
|---------|----------|--------------|
| `GET` | `/api/alerttypes` | Alle Alert-Typen |
| `POST` | `/api/alerttypes` | Neuen Typ anlegen |

### Health API

| Methode | Endpoint | Beschreibung |
|---------|----------|--------------|
| `GET` | `/health` | Health Check |
| `GET` | `/health/ready` | Readiness Check |

### SignalR Hub

| Endpoint | Events |
|----------|--------|
| `/hubs/sensors` | `NewSensorData`, `AlertReceived`, `AlertAcknowledged`, `HubStatusChanged`, `CloudSyncStatus` |

### MQTT Topics

| Topic | Richtung | Payload |
|-------|----------|---------|
| `myiotgrid/{tenantId}/sensordata` | Sensor → Hub | CreateSensorDataDto |
| `myiotgrid/{tenantId}/hubs/+/status` | Sensor → Hub | Online/Offline |
| `application/+/device/+/event/up` | ChirpStack → Hub | LoRaWAN Payload |

### Payload vom Sensor (REST)

```json
POST /api/sensordata
Content-Type: application/json

{
  "hubId": "sensor-wohnzimmer-01",
  "sensorType": "temperature",
  "value": 21.5,
  "location": {
    "name": "Wohnzimmer"
  }
}
```

Response: `201 Created` mit `SensorDataDto`

### Payload vom Sensor (MQTT)

```json
Topic: myiotgrid/{tenantId}/sensordata

{
  "hubId": "sensor-wohnzimmer-01",
  "sensorType": "temperature",
  "value": 21.5
}
```

---

## 🐳 DOCKER DEPLOYMENT

### Container-Stack

| Container | Image | Port | Funktion |
|-----------|-------|------|----------|
| hub-api | ghcr.io/myiotgrid/hub-api | 5000 | .NET 10 Backend |
| hub-frontend | ghcr.io/myiotgrid/hub-frontend | 443 | Angular 21 + nginx (HTTPS) |
| mosquitto | eclipse-mosquitto:2 | 1883, 9001 | MQTT Broker |
| chirpstack | chirpstack/chirpstack | 8080 | LoRaWAN (optional) |

### Volumes

| Volume | Inhalt |
|--------|--------|
| `./data/hub.db` | SQLite Datenbank |
| `./data/matter/` | Matter Credentials |
| `./logs/` | Application Logs |
| `./certs/` | SSL-Zertifikate |

### Container-Kommunikation

```
Browser ──:443──▶ [nginx/frontend] ──/api/──▶ [hub-api:5000]

Sensor ──:5000──▶ [hub-api] (REST JSON API)

Sensor ──:1883──▶ [mosquitto] ◀──subscribe── [hub-api]

Cloud ──:5000──▶ [hub-api] ◀──SignalR── [Cloud]

Apple Home ◀──Matter── [hub-api] (Matter.js Integration)
```

---

## 📋 DEFAULT SENSOR TYPES (Seed Data)

| Code | Name | Unit |
|------|------|------|
| temperature | Temperatur | °C |
| humidity | Luftfeuchtigkeit | % |
| pressure | Luftdruck | hPa |
| co2 | CO2 | ppm |
| pm25 | Feinstaub PM2.5 | µg/m³ |
| pm10 | Feinstaub PM10 | µg/m³ |
| soil_moisture | Bodenfeuchtigkeit | % |
| light | Helligkeit | lux |
| uv | UV-Index | index |
| wind_speed | Windgeschwindigkeit | m/s |
| rainfall | Niederschlag | mm |
| water_level | Wasserstand | cm |
| battery | Batterie | % |
| rssi | Signalstärke | dBm |

---

## 📋 DEFAULT ALERT TYPES (Seed Data)

| Code | Name | Default Level |
|------|------|---------------|
| mold_risk | Schimmelrisiko | Warning 🟡 |
| frost_warning | Frostwarnung | Critical 🔴 |
| heat_warning | Hitzewarnung | Warning 🟡 |
| air_quality | Luftqualität | Info 🔵 |
| battery_low | Batterie niedrig | Warning 🟡 |
| hub_offline | Hub offline | Critical 🔴 |
| sensor_error | Sensor-Fehler | Warning 🟡 |
| threshold_exceeded | Schwellwert überschritten | Info 🔵 |

---

## 🎨 HUB FRONTEND ARCHITEKTUR (Angular 21)

### Projektstruktur

```
myIoTGrid.Hub.Frontend/
├── src/
│   ├── app/
│   │   ├── core/              → Services, Guards, Interceptors
│   │   ├── shared/            → Shared Components, Pipes
│   │   ├── features/
│   │   │   ├── dashboard/     → Dashboard Feature
│   │   │   ├── hubs/          → Hub-Verwaltung
│   │   │   ├── sensordata/    → Messwerte-Anzeige
│   │   │   └── alerts/        → Warnungen
│   │   └── app.component.ts
│   └── environments/
└── angular.json
```

### 🚨 KRITISCHE FRONTEND-REGELN

#### 1. Standalone Components (PFLICHT ab Angular 21)

```typescript
// ✅ RICHTIG: Standalone Component
@Component({
  selector: 'app-hub-card',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule],
  templateUrl: './hub-card.component.html',
  styleUrl: './hub-card.component.scss'
})
export class HubCardComponent {
  @Input() hub!: Hub;
  @Input() latestSensorData: SensorData[] = [];
}
```

#### 2. Signals für Reactive State

```typescript
// ✅ RICHTIG: Signals verwenden
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent {
  private sensorDataService = inject(SensorDataService);
  private alertService = inject(AlertService);

  latestSensorData = signal<SensorData[]>([]);
  activeAlerts = signal<Alert[]>([]);
  isLoading = signal(false);
}
```

#### 3. SignalR Service für Live-Updates

```typescript
// core/services/signalr.service.ts
import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hubConnection!: signalR.HubConnection;

  connectionState = signal<signalR.HubConnectionState>(
    signalR.HubConnectionState.Disconnected
  );

  async startConnection(): Promise<void> {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/sensors`)
      .withAutomaticReconnect()
      .build();

    await this.hubConnection.start();
    this.connectionState.set(this.hubConnection.state);
  }

  onNewSensorData(callback: (data: SensorData) => void): void {
    this.hubConnection.on('NewSensorData', callback);
  }

  onAlertReceived(callback: (alert: Alert) => void): void {
    this.hubConnection.on('AlertReceived', callback);
  }

  async joinHubGroup(hubId: string): Promise<void> {
    await this.hubConnection.invoke('JoinHubGroup', hubId);
  }
}
```

#### 4. Interfaces müssen Backend-DTOs entsprechen

```typescript
// ✅ RICHTIG: Exakte Abbildung der Backend-DTOs
export interface SensorData {
  id: string;
  tenantId: string;
  hubId: string;
  sensorTypeId: string;
  sensorTypeCode: string;
  sensorTypeName: string;
  unit: string;
  value: number;
  timestamp: string;
  location?: Location;
  isSyncedToCloud: boolean;
}

export interface Hub {
  id: string;
  tenantId: string;
  hubId: string;
  name: string;
  protocol: Protocol;
  defaultLocation?: Location;
  lastSeen?: string;
  isOnline: boolean;
}

export interface Alert {
  id: string;
  tenantId: string;
  hubId?: string;
  alertTypeId: string;
  alertTypeCode: string;
  alertTypeName: string;
  level: AlertLevel;
  message: string;
  recommendation?: string;
  source: AlertSource;
  createdAt: string;
  expiresAt?: string;
  acknowledgedAt?: string;
  isActive: boolean;
}

export interface Location {
  name?: string;
  latitude?: number;
  longitude?: number;
}

export enum Protocol {
  Unknown = 0,
  WLAN = 1,
  LoRaWAN = 2
}

export enum AlertLevel {
  Ok = 0,
  Info = 1,
  Warning = 2,
  Critical = 3
}

export enum AlertSource {
  Local = 0,
  Cloud = 1
}

// ❌ FALSCH: Felder erfinden
export interface SensorData {
  icon: string;           // ❌ Backend hat das nicht!
  formattedValue: string; // ❌ Backend hat das nicht!
}
```

#### 5. 3 Dateien pro Component (PFLICHT)

```
hub-card/
├── hub-card.component.ts      → Logic
├── hub-card.component.html    → Template
└── hub-card.component.scss    → Styles

// ❌ FALSCH: Inline Template/Styles
@Component({
  template: `<div>...</div>`,  // ❌ VERBOTEN!
  styles: [`...`]              // ❌ VERBOTEN!
})
```

#### 6. Angular 21 Control Flow Syntax

```html
<!-- ✅ RICHTIG: Neue @-Syntax (Angular 17+) -->
@if (isLoading()) {
  <mat-spinner />
} @else {
  <div class="content">...</div>
}

@for (data of sensorData(); track data.id) {
  <app-sensor-data-card [data]="data" />
} @empty {
  <p>Keine Messwerte vorhanden</p>
}

@switch (alert.level) {
  @case (AlertLevel.Critical) { <span class="critical">Kritisch</span> }
  @case (AlertLevel.Warning) { <span class="warning">Warnung</span> }
  @default { <span class="ok">OK</span> }
}

<!-- ❌ FALSCH: Alte *ngIf/*ngFor Syntax -->
<div *ngIf="isLoading">...</div>
<div *ngFor="let data of sensorData">...</div>
```

#### 7. Alert-Level Styling

```scss
// shared/styles/_alerts.scss
.alert-ok {
  background-color: #c8e6c9;
  border-left: 4px solid #2e7d32;
}

.alert-info {
  background-color: #bbdefb;
  border-left: 4px solid #1565c0;
}

.alert-warning {
  background-color: #fff9c4;
  border-left: 4px solid #f9a825;
}

.alert-critical {
  background-color: #ffcdd2;
  border-left: 4px solid #c62828;
}
```

---

## 📡 SENSOR FIRMWARE (ESP32)

### PlatformIO Struktur

```
myIoTGrid.Sensor/
├── src/
│   ├── main.cpp
│   ├── config.h
│   ├── wifi_manager.cpp/.h
│   ├── mqtt_client.cpp/.h
│   ├── sensor_reader.cpp/.h
│   └── ota_updater.cpp/.h
├── platformio.ini
└── README.md
```

### Sensor-Payload (REST)

```cpp
// HTTP POST zu /api/sensordata
void sendReading(const char* sensorType, float value) {
    HTTPClient http;
    http.begin(HUB_URL "/api/sensordata");
    http.addHeader("Content-Type", "application/json");

    StaticJsonDocument<200> doc;
    doc["hubId"] = HUB_ID;
    doc["sensorType"] = sensorType;
    doc["value"] = value;

    String json;
    serializeJson(doc, json);

    int httpCode = http.POST(json);
    http.end();
}
```

### Sensor-Payload (MQTT)

```cpp
// main.cpp
#include <WiFi.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include "config.h"

const char* HUB_ID = "sensor-wohnzimmer-01";
const char* MQTT_TOPIC = "myiotgrid/{tenantId}/sensordata";

void sendReading(const char* sensorType, float value) {
    if (!mqttClient.connected()) return;

    StaticJsonDocument<200> doc;
    doc["hubId"] = HUB_ID;
    doc["sensorType"] = sensorType;
    doc["value"] = value;

    String json;
    serializeJson(doc, json);

    mqttClient.publish(MQTT_TOPIC, json.c_str());
}

void loop() {
    float temperature = readTemperature();
    float humidity = readHumidity();
    float co2 = readCO2();

    sendReading("temperature", temperature);
    sendReading("humidity", humidity);
    sendReading("co2", co2);

    delay(60000); // 1 Minute
}
```

### Unterstützte Sensoren

| Kategorie | Sensoren | Library |
|-----------|----------|---------|
| 🌡️ **Temperatur** | DHT22, BME280, BME680, DS18B20 | Adafruit_Sensor |
| 💧 **Luftfeuchte** | DHT22, BME280, SHT31 | Adafruit_Sensor |
| 💨 **CO₂** | MH-Z19B, SCD30, SCD40 | MH-Z19, SparkFun_SCD30 |
| 🌫️ **Feinstaub** | SDS011, PMS5003, SPS30 | SDS011sensor |
| 🌱 **Bodenfeuchte** | Capacitive Soil Sensor | analog |
| ☀️ **Licht** | BH1750, TSL2561 | BH1750 |

---

## 🚀 WICHTIGE BEFEHLE

### Backend (.NET 10)

```bash
# Build
cd myIoTGrid.Hub/myIoTGrid.Hub.Backend
dotnet build

# Tests ausführen
dotnet test

# Migration erstellen
dotnet ef migrations add InitialCreate \
    --project src/myIoTGrid.Hub.Infrastructure \
    --startup-project src/myIoTGrid.Hub.Api

# Datenbank aktualisieren
dotnet ef database update \
    --project src/myIoTGrid.Hub.Infrastructure \
    --startup-project src/myIoTGrid.Hub.Api

# Anwendung starten
dotnet run --project src/myIoTGrid.Hub.Api

# Für Raspberry Pi (ARM64) publishen
dotnet publish src/myIoTGrid.Hub.Api -c Release -r linux-arm64 --self-contained
```

### Frontend (Angular 21)

```bash
cd myIoTGrid.Hub/myIoTGrid.Hub.Frontend

# Projekt erstellen
ng new myIoTGrid.Hub.Frontend --standalone --style=scss

# Component erstellen
ng generate component features/dashboard/dashboard --standalone

# Service erstellen
ng generate service core/services/sensor-data

# Build
ng build

# Dev Server
ng serve
```

### ESP32 (PlatformIO)

```bash
cd myIoTGrid.Sensor

# Build
pio run

# Upload
pio run --target upload

# Serial Monitor
pio device monitor

# OTA Update
pio run --target upload --upload-port <IP>
```

### Docker (Raspberry Pi)

```bash
cd myIoTGrid.Hub/myIoTGrid.Hub.Backend/docker

# Stack starten
docker-compose up -d

# Logs
docker-compose logs -f hub-api

# Stack stoppen
docker-compose down
```

---

## ⚙️ KONFIGURATION

### appsettings.json Struktur

```json
{
  "ConnectionStrings": {
    "HubDb": "Data Source=./data/hub.db"
  },
  "Mqtt": {
    "Host": "mosquitto",
    "Port": 1883,
    "ClientId": "hub-api"
  },
  "Cloud": {
    "BaseUrl": "https://api.myiotgrid.com",
    "ApiKey": "your-api-key",
    "SyncIntervalSeconds": 60,
    "RetryCount": 3
  },
  "Hub": {
    "DefaultTenantId": "00000000-0000-0000-0000-000000000001",
    "DefaultTenantName": "Default",
    "DataRetentionDays": 30,
    "HubOfflineTimeoutMinutes": 5
  }
}
```

---

## ⚠️ HÄUFIGE FEHLER VERMEIDEN

### ❌ Falsch (Backend)

```csharp
// Entity im falschen Projekt (VERBOTEN!)
// myIoTGrid.Hub.Service/Entities/Hub.cs ← VERBOTEN!

// DTO im Service-Projekt definiert (VERBOTEN!)
public class SensorDataService
{
    public class SensorDataDto { }  // ❌ Gehört ins Shared-Projekt!
}

// Synchrone DB-Operation (VERBOTEN!)
public SensorData GetById(Guid id)
{
    return _context.SensorData.Find(id);  // ❌ VERBOTEN!
}

// TenantId vergessen
var sensorData = new SensorData
{
    HubId = hubId,
    // TenantId fehlt! ← VERBOTEN!
};
```

### ✅ Richtig (Backend)

```csharp
// Entity im Domain-Projekt
// myIoTGrid.Hub.Domain/Entities/SensorData.cs
namespace myIoTGrid.Hub.Domain.Entities;
public class SensorData { }

// DTO im Shared-Projekt
// myIoTGrid.Hub.Shared/DTOs/SensorDataDto.cs
namespace myIoTGrid.Hub.Shared.DTOs;
public record SensorDataDto { }

// Asynchron mit CancellationToken
public async Task<SensorData?> GetByIdAsync(Guid id, CancellationToken ct = default)
{
    return await _context.SensorData
        .AsNoTracking()
        .FirstOrDefaultAsync(s => s.Id == id, ct);
}

// TenantId immer setzen
var sensorData = new SensorData
{
    TenantId = _tenantService.GetCurrentTenantId(),  // ✅
    HubId = hubId,
    SensorTypeId = sensorType.Id,
    Value = dto.Value,
    Timestamp = DateTime.UtcNow
};
```

### ❌ Falsch (Frontend)

```typescript
// Alte Control Flow Syntax (VERALTET!)
<div *ngIf="isLoading">...</div>
<div *ngFor="let item of items">...</div>

// Subject statt Signal
sensorData$ = new BehaviorSubject<SensorData[]>([]);  // ❌ Signals bevorzugen!

// Felder erfinden
export interface SensorData {
  icon: string;  // ❌ Backend hat das nicht!
}
```

### ✅ Richtig (Frontend)

```typescript
// Neue @-Syntax (Angular 17+)
@if (isLoading()) { ... }
@for (item of items(); track item.id) { ... }

// Signal-based State
sensorData = signal<SensorData[]>([]);  // ✅ Modern!

// Exakte Backend-DTOs
export interface SensorData {
  id: string;
  tenantId: string;
  hubId: string;
  sensorTypeId: string;
  sensorTypeCode: string;
  value: number;
  timestamp: string;
}
```

---

## 🔍 VALIDATION CHECKLIST

### Vor jedem Commit (Backend):
- [ ] Entities im Domain-Projekt?
- [ ] DTOs im Shared-Projekt?
- [ ] Constants im Shared-Projekt?
- [ ] Controllers & Hubs im Interface-Projekt?
- [ ] DbContext & Repositories im Infrastructure-Projekt?
- [ ] Services im Service-Projekt?
- [ ] Keine Duplikate erstellt?
- [ ] Alle Operationen async mit CancellationToken?
- [ ] TenantId in allen Entities gesetzt?
- [ ] SignalR-Broadcast nach neuen SensorData?
- [ ] Tests vorhanden?
- [ ] Build erfolgreich?

### Vor jedem Commit (Frontend):
- [ ] Standalone Components verwendet?
- [ ] Neue @-Syntax statt *ngIf/*ngFor?
- [ ] Signals statt BehaviorSubject?
- [ ] Interfaces entsprechen Backend-DTOs?
- [ ] 3 Dateien pro Component?
- [ ] SignalR-Service für Live-Updates?
- [ ] Alert-Level-Styling korrekt?

### Vor jedem Commit (ESP32):
- [ ] **FIRMWARE_VERSION erhöht!** (in config.h) - PFLICHT bei jeder Änderung!
- [ ] Payload enthält hubId, sensorType, value?
- [ ] REST oder MQTT korrekt implementiert?
- [ ] WiFi/MQTT Reconnect implementiert?
- [ ] Deep Sleep für Batteriebetrieb?
- [ ] OTA-Updates möglich?

### 🚨 WICHTIG: Firmware-Versionierung
Bei **JEDER** Änderung an der ESP32-Firmware MUSS die Version in `config.h` erhöht werden:
```cpp
#define FIRMWARE_VERSION "X.Y.Z"
```
- **X** (Major): Breaking Changes, neue Architektur
- **Y** (Minor): Neue Features, neue Sensoren
- **Z** (Patch): Bugfixes, kleine Verbesserungen

Die Version wird beim Start im Serial Monitor angezeigt - so erkennt man sofort welche Firmware läuft!

---

## 📚 WICHTIGE LINKS & RESSOURCEN

### Confluence Dokumentation
- **Hauptseite:** https://mysocialcare-doku.atlassian.net/wiki/spaces/myIoTGrid
- **Hub Konzept:** https://mysocialcare-doku.atlassian.net/wiki/x/AYB-Ag
- **Hub Architektur:** Architektur-Seite im Space

### Technologie-Dokumentation
- **Angular 21:** https://angular.dev
- **.NET 10:** https://learn.microsoft.com/dotnet
- **EF Core:** https://learn.microsoft.com/ef/core
- **SignalR:** https://learn.microsoft.com/aspnet/core/signalr
- **Matter.js:** https://github.com/project-chip/matter.js
- **ESP32 Arduino:** https://docs.espressif.com/projects/arduino-esp32

---

## 💡 WICHTIGSTE PRINZIPIEN (ZUSAMMENFASSUNG)

### Backend (.NET 10 - Clean Architecture)
1. **Entities → Domain** - Niemals woanders!
2. **DTOs & Constants → Shared** - Von allen Layern referenziert
3. **Controllers & Hubs → Interface** - API-Endpunkte
4. **Business Logic → Service** - Services und Interfaces
5. **DbContext & Repos → Infrastructure** - Datenzugriff
6. **Async überall** - Keine synchronen DB-Operationen!
7. **Multi-Tenant** - TenantId in allen Entities!
8. **SignalR für Echtzeit** - Alle Clients erhalten Live-Updates
9. **Cloud-KI** - Alerts kommen von der Cloud

### Frontend (Angular 21)
1. **Standalone Components** - Keine NgModules!
2. **@-Syntax** - @if, @for, @switch statt *ngIf, *ngFor
3. **Signals statt RxJS** - Für lokalen State
4. **Interfaces = Backend-DTOs** - Keine Felder erfinden!
5. **3 Dateien pro Component** - .ts, .html, .scss
6. **SignalR für Live-Updates** - Echtzeit-Daten

### ESP32 (Firmware)
1. **REST oder MQTT** - Beide Protokolle unterstützt
2. **Minimaler Payload** - hubId, sensorType, value
3. **WiFi/MQTT Reconnect** - Automatische Wiederverbindung
4. **Deep Sleep** - Für Batteriebetrieb
5. **OTA-Updates** - Remote-Aktualisierung

### KI-Architektur
1. **Cloud-KI** - KI-Analyse in der Cloud, nicht lokal
2. **Alerts von Cloud** - Hub empfängt KI-Alerts
3. **Lokale Fallback-Regeln** - Für Offline-Betrieb
4. **SensorType/AlertType Sync** - Von Cloud synchronisiert

---

## 📞 BEI FRAGEN

1. Confluence Dokumentation durchsuchen
2. Backend DTOs prüfen (myIoTGrid.Hub.Shared/DTOs)
3. Existing Code-Base überprüfen (keine Duplikate!)
4. **NIEMALS einfach Felder erfinden oder Architektur-Regeln brechen!**

---

**ERFOLG = Clean Architecture + Multi-Tenant + Async + SignalR + Cloud-KI + Keine Duplikate!** 🚀

---

**myIoTGrid**

*Open Source · Privacy First · Cloud-KI*

*Made with ❤️ in Germany*
