# myIoTGrid Cloud API - Readings (Messwerte)

**Version:** 1.0
**Base URL:** `https://api.myiotgrid.cloud` (Production) | `http://localhost:5000` (Development)
**Content-Type:** `application/json`

---

## Inhaltsverzeichnis

1. [Übersicht](#übersicht)
2. [Authentifizierung](#authentifizierung)
3. [Datenmodelle](#datenmodelle)
4. [Endpunkte](#endpunkte)
   - [Readings eines Nodes abrufen](#1-readings-eines-nodes-abrufen)
   - [Letzte Readings eines Nodes](#2-letzte-readings-eines-nodes)
   - [Gefilterte Readings (allgemein)](#3-gefilterte-readings-allgemein)
   - [Paginierte Readings mit Sortierung](#4-paginierte-readings-mit-sortierung)
   - [Einzelnes Reading abrufen](#5-einzelnes-reading-abrufen)
   - [Chart-Daten (aggregiert)](#6-chart-daten-aggregiert)
   - [Readings-Liste (paginiert)](#7-readings-liste-paginiert)
   - [CSV-Export](#8-csv-export)
   - [Reading erstellen](#9-reading-erstellen)
   - [Batch-Upload](#10-batch-upload)
   - [Readings löschen](#11-readings-löschen)
5. [Frontend-Integration (TypeScript/Angular)](#frontend-integration)
6. [Postman Collection](#postman-collection)
7. [Fehlerbehandlung](#fehlerbehandlung)

---

## Übersicht

Die Readings API ermöglicht das Abrufen, Erstellen und Verwalten von Sensormesswerten. Jedes Reading enthält:

- **Node** - Das Gerät (ESP32/LoRa32), das den Messwert gesendet hat
- **Sensor** - Der Sensor, der gemessen hat (z.B. BME280, DHT22)
- **MeasurementType** - Der Messwerttyp (z.B. `temperature`, `humidity`, `co2`)
- **Value** - Der kalibrierte Messwert
- **RawValue** - Der Rohwert vor Kalibrierung
- **Timestamp** - Zeitpunkt der Messung

---

## Authentifizierung

> **Hinweis:** Aktuell keine Authentifizierung erforderlich (Development-Modus).
> In Production wird Bearer Token Authentication verwendet.

```http
Authorization: Bearer <your-api-key>
```

---

## Datenmodelle

### ReadingDto (Response)

```typescript
interface ReadingDto {
  id: number;                    // Unique ID (long)
  tenantId: string;              // Tenant UUID
  nodeId: string;                // Node UUID
  nodeName: string;              // Node display name
  assignmentId: string | null;   // Sensor assignment UUID
  sensorId: string | null;       // Sensor UUID
  sensorCode: string;            // Sensor code (e.g., "BME280")
  sensorName: string;            // Sensor display name
  sensorIcon: string | null;     // Material icon name
  sensorColor: string | null;    // Hex color code
  measurementType: string;       // Type (e.g., "temperature")
  displayName: string;           // Human-readable name
  rawValue: number;              // Raw sensor value
  value: number;                 // Calibrated value
  unit: string;                  // Unit (e.g., "°C", "%", "ppm")
  timestamp: string;             // ISO 8601 datetime
  location: LocationDto | null;  // Optional location
  isSyncedToCloud: boolean;      // Cloud sync status
}

interface LocationDto {
  name: string | null;
  latitude: number | null;
  longitude: number | null;
}
```

### ReadingFilterDto (Query Parameters)

```typescript
interface ReadingFilterDto {
  nodeId?: string;           // Filter by Node UUID
  nodeIdentifier?: string;   // Filter by Node serial number
  hubId?: string;            // Filter by Hub UUID
  assignmentId?: string;     // Filter by sensor assignment UUID
  measurementType?: string;  // Filter by type (e.g., "temperature")
  from?: string;             // Start datetime (ISO 8601)
  to?: string;               // End datetime (ISO 8601)
  isSyncedToCloud?: boolean; // Filter by sync status
  page?: number;             // Page number (default: 1)
  pageSize?: number;         // Items per page (default: 50)
}
```

### PaginatedResultDto (Response)

```typescript
interface PaginatedResultDto<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
```

---

## Endpunkte

### 1. Readings eines Nodes abrufen

Ruft alle Readings für einen bestimmten Node ab, mit optionalen Filtern.

```http
GET /api/readings/node/{nodeId}
```

#### Parameter

| Name | Typ | In | Required | Beschreibung |
|------|-----|-----|----------|--------------|
| `nodeId` | UUID | path | Ja | Node-ID |
| `assignmentId` | UUID | query | Nein | Filter nach Sensor-Zuweisung |
| `measurementType` | string | query | Nein | Filter nach Typ (z.B. "temperature") |
| `from` | datetime | query | Nein | Start-Zeitpunkt (ISO 8601) |
| `to` | datetime | query | Nein | End-Zeitpunkt (ISO 8601) |
| `page` | int | query | Nein | Seite (Default: 1) |
| `pageSize` | int | query | Nein | Einträge pro Seite (Default: 50) |

#### Beispiel Request

```http
GET /api/readings/node/3fa85f64-5717-4562-b3fc-2c963f66afa6?measurementType=temperature&from=2025-12-01T00:00:00Z&to=2025-12-12T23:59:59Z&pageSize=100
```

#### Beispiel Response

```json
[
  {
    "id": 12345,
    "tenantId": "00000000-0000-0000-0000-000000000001",
    "nodeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "nodeName": "Wohnzimmer Sensor",
    "assignmentId": "a1b2c3d4-5678-90ab-cdef-1234567890ab",
    "sensorId": "b2c3d4e5-6789-0abc-def1-234567890abc",
    "sensorCode": "BME280",
    "sensorName": "BME280 Umweltsensor",
    "sensorIcon": "thermostat",
    "sensorColor": "#FF5722",
    "measurementType": "temperature",
    "displayName": "Temperatur",
    "rawValue": 21.45,
    "value": 21.5,
    "unit": "°C",
    "timestamp": "2025-12-12T10:30:00Z",
    "location": {
      "name": "Wohnzimmer",
      "latitude": 50.9375,
      "longitude": 6.9603
    },
    "isSyncedToCloud": true
  },
  {
    "id": 12344,
    "tenantId": "00000000-0000-0000-0000-000000000001",
    "nodeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "nodeName": "Wohnzimmer Sensor",
    "assignmentId": "a1b2c3d4-5678-90ab-cdef-1234567890ab",
    "sensorId": "b2c3d4e5-6789-0abc-def1-234567890abc",
    "sensorCode": "BME280",
    "sensorName": "BME280 Umweltsensor",
    "sensorIcon": "thermostat",
    "sensorColor": "#FF5722",
    "measurementType": "temperature",
    "displayName": "Temperatur",
    "rawValue": 21.32,
    "value": 21.3,
    "unit": "°C",
    "timestamp": "2025-12-12T10:25:00Z",
    "location": null,
    "isSyncedToCloud": true
  }
]
```

#### cURL

```bash
curl -X GET "http://localhost:5000/api/readings/node/3fa85f64-5717-4562-b3fc-2c963f66afa6?measurementType=temperature&from=2025-12-01T00:00:00Z&to=2025-12-12T23:59:59Z" \
  -H "Accept: application/json"
```

---

### 2. Letzte Readings eines Nodes

Gibt die neuesten Messwerte pro Sensor und Messwerttyp zurück.

```http
GET /api/readings/latest/{nodeId}
```

#### Parameter

| Name | Typ | In | Required | Beschreibung |
|------|-----|-----|----------|--------------|
| `nodeId` | UUID | path | Ja | Node-ID |

#### Beispiel Request

```http
GET /api/readings/latest/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

#### Beispiel Response

```json
[
  {
    "id": 12345,
    "nodeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "nodeName": "Wohnzimmer Sensor",
    "sensorCode": "BME280",
    "measurementType": "temperature",
    "displayName": "Temperatur",
    "value": 21.5,
    "unit": "°C",
    "timestamp": "2025-12-12T10:30:00Z"
  },
  {
    "id": 12346,
    "nodeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "nodeName": "Wohnzimmer Sensor",
    "sensorCode": "BME280",
    "measurementType": "humidity",
    "displayName": "Luftfeuchtigkeit",
    "value": 45.2,
    "unit": "%",
    "timestamp": "2025-12-12T10:30:00Z"
  },
  {
    "id": 12347,
    "nodeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "nodeName": "Wohnzimmer Sensor",
    "sensorCode": "MH-Z19B",
    "measurementType": "co2",
    "displayName": "CO₂",
    "value": 823,
    "unit": "ppm",
    "timestamp": "2025-12-12T10:30:00Z"
  }
]
```

#### cURL

```bash
curl -X GET "http://localhost:5000/api/readings/latest/3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -H "Accept: application/json"
```

---

### 3. Gefilterte Readings (allgemein)

Ruft Readings mit flexiblen Filteroptionen ab.

```http
GET /api/readings
```

#### Query Parameters

| Name | Typ | Required | Beschreibung |
|------|-----|----------|--------------|
| `nodeId` | UUID | Nein | Filter nach Node |
| `nodeIdentifier` | string | Nein | Filter nach Node Serial Number |
| `hubId` | UUID | Nein | Filter nach Hub |
| `assignmentId` | UUID | Nein | Filter nach Sensor-Zuweisung |
| `measurementType` | string | Nein | Filter nach Typ |
| `from` | datetime | Nein | Start-Zeitpunkt |
| `to` | datetime | Nein | End-Zeitpunkt |
| `isSyncedToCloud` | boolean | Nein | Filter nach Sync-Status |
| `page` | int | Nein | Seite (Default: 1) |
| `pageSize` | int | Nein | Einträge pro Seite (Default: 50) |

#### Beispiele

**Alle Temperaturmessungen der letzten 24 Stunden:**
```http
GET /api/readings?measurementType=temperature&from=2025-12-11T10:30:00Z
```

**Alle ungesyncten Readings eines Hubs:**
```http
GET /api/readings?hubId=abc123&isSyncedToCloud=false
```

**Readings eines Nodes nach Serial Number:**
```http
GET /api/readings?nodeIdentifier=ESP32-WROOM-001&pageSize=100
```

#### Beispiel Response

```json
{
  "items": [
    {
      "id": 12345,
      "nodeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "nodeName": "Wohnzimmer Sensor",
      "measurementType": "temperature",
      "value": 21.5,
      "unit": "°C",
      "timestamp": "2025-12-12T10:30:00Z"
    }
  ],
  "totalCount": 1542,
  "page": 1,
  "pageSize": 50,
  "totalPages": 31
}
```

#### cURL

```bash
curl -X GET "http://localhost:5000/api/readings?measurementType=temperature&from=2025-12-11T00:00:00Z&pageSize=100" \
  -H "Accept: application/json"
```

---

### 4. Paginierte Readings mit Sortierung

Erweiterte Abfrage mit Server-Side Paging, Sorting und Filtering.

```http
GET /api/readings/paged
```

#### Query Parameters

| Name | Typ | Beschreibung |
|------|-----|--------------|
| `page` | int | Seitennummer (1-basiert) |
| `size` | int | Einträge pro Seite |
| `sort` | string | Sortierung: `field,direction` (z.B. `timestamp,desc`) |
| `search` | string | Volltextsuche |
| `filters[n].field` | string | Feldname für Filter n |
| `filters[n].operator` | string | Operator: `eq`, `contains`, `gt`, `lt`, `gte`, `lte` |
| `filters[n].value` | string | Filterwert |

#### Beispiele

**Neueste Readings zuerst, 20 pro Seite:**
```http
GET /api/readings/paged?page=1&size=20&sort=timestamp,desc
```

**Nur Temperatur > 25°C:**
```http
GET /api/readings/paged?filters[0].field=measurementType&filters[0].operator=eq&filters[0].value=temperature&filters[1].field=value&filters[1].operator=gt&filters[1].value=25
```

**Suche nach Node-Namen:**
```http
GET /api/readings/paged?search=Wohnzimmer&sort=timestamp,desc
```

#### Beispiel Response

```json
{
  "items": [...],
  "totalCount": 5420,
  "page": 1,
  "size": 20,
  "totalPages": 271,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

#### cURL

```bash
curl -X GET "http://localhost:5000/api/readings/paged?page=1&size=20&sort=timestamp,desc&filters[0].field=measurementType&filters[0].value=temperature" \
  -H "Accept: application/json"
```

---

### 5. Einzelnes Reading abrufen

```http
GET /api/readings/{id}
```

#### Parameter

| Name | Typ | In | Required | Beschreibung |
|------|-----|-----|----------|--------------|
| `id` | long | path | Ja | Reading-ID |

#### Beispiel Response

```json
{
  "id": 12345,
  "tenantId": "00000000-0000-0000-0000-000000000001",
  "nodeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "nodeName": "Wohnzimmer Sensor",
  "assignmentId": "a1b2c3d4-5678-90ab-cdef-1234567890ab",
  "sensorId": "b2c3d4e5-6789-0abc-def1-234567890abc",
  "sensorCode": "BME280",
  "sensorName": "BME280 Umweltsensor",
  "sensorIcon": "thermostat",
  "sensorColor": "#FF5722",
  "measurementType": "temperature",
  "displayName": "Temperatur",
  "rawValue": 21.45,
  "value": 21.5,
  "unit": "°C",
  "timestamp": "2025-12-12T10:30:00Z",
  "location": {
    "name": "Wohnzimmer",
    "latitude": 50.9375,
    "longitude": 6.9603
  },
  "isSyncedToCloud": true
}
```

#### cURL

```bash
curl -X GET "http://localhost:5000/api/readings/12345" \
  -H "Accept: application/json"
```

---

### 6. Chart-Daten (aggregiert)

Liefert aggregierte Daten für Chart-Visualisierungen mit Statistiken und Trend.

```http
GET /api/readings/chart/{nodeId}/{assignmentId}/{measurementType}
```

#### Parameter

| Name | Typ | In | Required | Beschreibung |
|------|-----|-----|----------|--------------|
| `nodeId` | UUID | path | Ja | Node-ID |
| `assignmentId` | UUID | path | Ja | Sensor-Zuweisung-ID |
| `measurementType` | string | path | Ja | Messwerttyp |
| `interval` | enum | query | Nein | Zeitintervall (Default: `OneDay`) |

#### Intervall-Optionen

| Wert | Beschreibung | Aggregation |
|------|--------------|-------------|
| `OneHour` | Letzte Stunde | Pro Minute |
| `SixHours` | Letzte 6 Stunden | Pro 5 Minuten |
| `TwelveHours` | Letzte 12 Stunden | Pro 10 Minuten |
| `OneDay` | Letzte 24 Stunden | Pro 15 Minuten |
| `OneWeek` | Letzte 7 Tage | Pro Stunde |
| `OneMonth` | Letzte 30 Tage | Pro 4 Stunden |
| `ThreeMonths` | Letzte 90 Tage | Pro Tag |
| `OneYear` | Letztes Jahr | Pro Woche |

#### Beispiel Request

```http
GET /api/readings/chart/3fa85f64-5717-4562-b3fc-2c963f66afa6/a1b2c3d4-5678-90ab-cdef-1234567890ab/temperature?interval=OneDay
```

#### Beispiel Response

```json
{
  "nodeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "nodeName": "Wohnzimmer Sensor",
  "assignmentId": "a1b2c3d4-5678-90ab-cdef-1234567890ab",
  "sensorCode": "BME280",
  "sensorName": "BME280 Umweltsensor",
  "measurementType": "temperature",
  "displayName": "Temperatur",
  "unit": "°C",
  "interval": "OneDay",
  "from": "2025-12-11T10:30:00Z",
  "to": "2025-12-12T10:30:00Z",
  "dataPoints": [
    {
      "timestamp": "2025-12-11T10:30:00Z",
      "value": 20.5,
      "min": 20.1,
      "max": 20.9,
      "count": 4
    },
    {
      "timestamp": "2025-12-11T10:45:00Z",
      "value": 20.8,
      "min": 20.5,
      "max": 21.2,
      "count": 4
    }
  ],
  "statistics": {
    "min": 18.5,
    "max": 24.2,
    "avg": 21.3,
    "count": 96
  },
  "trend": {
    "direction": "up",
    "changePercent": 2.5,
    "changeAbsolute": 0.5
  }
}
```

#### cURL

```bash
curl -X GET "http://localhost:5000/api/readings/chart/3fa85f64-5717-4562-b3fc-2c963f66afa6/a1b2c3d4-5678-90ab-cdef-1234567890ab/temperature?interval=OneDay" \
  -H "Accept: application/json"
```

---

### 7. Readings-Liste (paginiert)

Paginierte Liste für Tabellen-Ansicht eines spezifischen Messwerttyps.

```http
GET /api/readings/list/{nodeId}/{assignmentId}/{measurementType}
```

#### Parameter

| Name | Typ | In | Required | Beschreibung |
|------|-----|-----|----------|--------------|
| `nodeId` | UUID | path | Ja | Node-ID |
| `assignmentId` | UUID | path | Ja | Sensor-Zuweisung-ID |
| `measurementType` | string | path | Ja | Messwerttyp |
| `page` | int | query | Nein | Seite (Default: 1) |
| `pageSize` | int | query | Nein | Einträge pro Seite (Default: 20) |
| `from` | datetime | query | Nein | Start-Zeitpunkt |
| `to` | datetime | query | Nein | End-Zeitpunkt |

#### Beispiel Request

```http
GET /api/readings/list/3fa85f64-5717-4562-b3fc-2c963f66afa6/a1b2c3d4-5678-90ab-cdef-1234567890ab/temperature?page=1&pageSize=20&from=2025-12-01T00:00:00Z
```

#### Beispiel Response

```json
{
  "items": [
    {
      "id": 12345,
      "value": 21.5,
      "rawValue": 21.45,
      "timestamp": "2025-12-12T10:30:00Z"
    },
    {
      "id": 12344,
      "value": 21.3,
      "rawValue": 21.32,
      "timestamp": "2025-12-12T10:25:00Z"
    }
  ],
  "totalCount": 1542,
  "page": 1,
  "pageSize": 20,
  "totalPages": 78,
  "measurementType": "temperature",
  "displayName": "Temperatur",
  "unit": "°C"
}
```

#### cURL

```bash
curl -X GET "http://localhost:5000/api/readings/list/3fa85f64-5717-4562-b3fc-2c963f66afa6/a1b2c3d4-5678-90ab-cdef-1234567890ab/temperature?page=1&pageSize=20" \
  -H "Accept: application/json"
```

---

### 8. CSV-Export

Exportiert Readings als CSV-Datei für Excel/Analyse.

```http
GET /api/readings/list/{nodeId}/{assignmentId}/{measurementType}/csv
```

#### Parameter

| Name | Typ | In | Required | Beschreibung |
|------|-----|-----|----------|--------------|
| `nodeId` | UUID | path | Ja | Node-ID |
| `assignmentId` | UUID | path | Ja | Sensor-Zuweisung-ID |
| `measurementType` | string | path | Ja | Messwerttyp |
| `from` | datetime | query | Nein | Start-Zeitpunkt |
| `to` | datetime | query | Nein | End-Zeitpunkt |

#### Beispiel Request

```http
GET /api/readings/list/3fa85f64-5717-4562-b3fc-2c963f66afa6/a1b2c3d4-5678-90ab-cdef-1234567890ab/temperature/csv?from=2025-12-01&to=2025-12-12
```

#### Beispiel Response (CSV)

```csv
Id,Timestamp,RawValue,Value,Unit
12345,2025-12-12T10:30:00Z,21.45,21.5,°C
12344,2025-12-12T10:25:00Z,21.32,21.3,°C
12343,2025-12-12T10:20:00Z,21.28,21.3,°C
```

#### cURL

```bash
curl -X GET "http://localhost:5000/api/readings/list/3fa85f64-5717-4562-b3fc-2c963f66afa6/a1b2c3d4-5678-90ab-cdef-1234567890ab/temperature/csv?from=2025-12-01" \
  -H "Accept: text/csv" \
  -o readings_temperature.csv
```

---

### 9. Reading erstellen

Erstellt ein neues Reading (typischerweise von ESP32 Sensor).

```http
POST /api/readings
```

#### Request Body

```json
{
  "deviceId": "ESP32-WROOM-001",
  "type": "temperature",
  "value": 21.5,
  "unit": "°C",
  "timestamp": 1702377000000,
  "endpointId": 1
}
```

| Feld | Typ | Required | Beschreibung |
|------|-----|----------|--------------|
| `deviceId` | string | Ja | Node Serial Number |
| `type` | string | Ja | Messwerttyp |
| `value` | number | Ja | Messwert |
| `unit` | string | Nein | Einheit |
| `timestamp` | long | Nein | Unix Timestamp in ms (Default: now) |
| `endpointId` | int | Nein | Sensor-Endpoint (für Multi-Sensor-Nodes) |

#### Beispiel Response

```json
{
  "id": 12346,
  "nodeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "nodeName": "Wohnzimmer Sensor",
  "measurementType": "temperature",
  "value": 21.5,
  "unit": "°C",
  "timestamp": "2025-12-12T10:30:00Z"
}
```

#### cURL

```bash
curl -X POST "http://localhost:5000/api/readings" \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "ESP32-WROOM-001",
    "type": "temperature",
    "value": 21.5
  }'
```

---

### 10. Batch-Upload

Lädt mehrere Readings in einem Request hoch (für Offline-Sync).

```http
POST /api/readings/batch
```

#### Request Body

```json
{
  "nodeId": "ESP32-WROOM-001",
  "hubId": null,
  "readings": [
    {
      "endpointId": 1,
      "measurementType": "temperature",
      "rawValue": 21.5
    },
    {
      "endpointId": 1,
      "measurementType": "humidity",
      "rawValue": 45.2
    },
    {
      "endpointId": 2,
      "measurementType": "co2",
      "rawValue": 823
    }
  ],
  "timestamp": "2025-12-12T10:30:00Z"
}
```

#### Beispiel Response

```json
{
  "successCount": 3,
  "failedCount": 0,
  "totalCount": 3,
  "nodeId": "ESP32-WROOM-001",
  "processedAt": "2025-12-12T10:30:05Z",
  "errors": null
}
```

#### cURL

```bash
curl -X POST "http://localhost:5000/api/readings/batch" \
  -H "Content-Type: application/json" \
  -d '{
    "nodeId": "ESP32-WROOM-001",
    "readings": [
      {"endpointId": 1, "measurementType": "temperature", "rawValue": 21.5},
      {"endpointId": 1, "measurementType": "humidity", "rawValue": 45.2}
    ]
  }'
```

---

### 11. Readings löschen

Löscht Readings in einem Zeitraum.

```http
DELETE /api/readings/range
```

#### Request Body

```json
{
  "nodeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "from": "2025-12-01T00:00:00Z",
  "to": "2025-12-05T23:59:59Z",
  "assignmentId": null,
  "measurementType": "temperature"
}
```

| Feld | Typ | Required | Beschreibung |
|------|-----|----------|--------------|
| `nodeId` | UUID | Ja | Node-ID |
| `from` | datetime | Ja | Start-Zeitpunkt |
| `to` | datetime | Ja | End-Zeitpunkt |
| `assignmentId` | UUID | Nein | Nur bestimmte Sensor-Zuweisung |
| `measurementType` | string | Nein | Nur bestimmter Messwerttyp |

#### Beispiel Response

```json
{
  "deletedCount": 1542,
  "nodeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "from": "2025-12-01T00:00:00Z",
  "to": "2025-12-05T23:59:59Z",
  "assignmentId": null,
  "measurementType": "temperature"
}
```

#### cURL

```bash
curl -X DELETE "http://localhost:5000/api/readings/range" \
  -H "Content-Type: application/json" \
  -d '{
    "nodeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "from": "2025-12-01T00:00:00Z",
    "to": "2025-12-05T23:59:59Z",
    "measurementType": "temperature"
  }'
```

---

## Frontend-Integration

### TypeScript Interfaces

```typescript
// models/reading.model.ts

export interface Reading {
  id: number;
  tenantId: string;
  nodeId: string;
  nodeName: string;
  assignmentId: string | null;
  sensorId: string | null;
  sensorCode: string;
  sensorName: string;
  sensorIcon: string | null;
  sensorColor: string | null;
  measurementType: string;
  displayName: string;
  rawValue: number;
  value: number;
  unit: string;
  timestamp: string;
  location: Location | null;
  isSyncedToCloud: boolean;
}

export interface Location {
  name: string | null;
  latitude: number | null;
  longitude: number | null;
}

export interface ReadingFilter {
  nodeId?: string;
  nodeIdentifier?: string;
  hubId?: string;
  assignmentId?: string;
  measurementType?: string;
  from?: string;
  to?: string;
  isSyncedToCloud?: boolean;
  page?: number;
  pageSize?: number;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ChartData {
  nodeId: string;
  nodeName: string;
  measurementType: string;
  displayName: string;
  unit: string;
  interval: string;
  from: string;
  to: string;
  dataPoints: ChartDataPoint[];
  statistics: ChartStatistics;
  trend: ChartTrend;
}

export interface ChartDataPoint {
  timestamp: string;
  value: number;
  min: number;
  max: number;
  count: number;
}

export interface ChartStatistics {
  min: number;
  max: number;
  avg: number;
  count: number;
}

export interface ChartTrend {
  direction: 'up' | 'down' | 'stable';
  changePercent: number;
  changeAbsolute: number;
}

export type ChartInterval =
  | 'OneHour'
  | 'SixHours'
  | 'TwelveHours'
  | 'OneDay'
  | 'OneWeek'
  | 'OneMonth'
  | 'ThreeMonths'
  | 'OneYear';
```

### Angular Service

```typescript
// services/reading.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  Reading,
  ReadingFilter,
  PaginatedResult,
  ChartData,
  ChartInterval
} from '../models/reading.model';

@Injectable({ providedIn: 'root' })
export class ReadingService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/readings`;

  /**
   * Alle Readings eines Nodes abrufen
   */
  getByNode(nodeId: string, filter?: ReadingFilter): Observable<Reading[]> {
    let params = new HttpParams();

    if (filter) {
      if (filter.measurementType) params = params.set('measurementType', filter.measurementType);
      if (filter.assignmentId) params = params.set('assignmentId', filter.assignmentId);
      if (filter.from) params = params.set('from', filter.from);
      if (filter.to) params = params.set('to', filter.to);
      if (filter.page) params = params.set('page', filter.page.toString());
      if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    }

    return this.http.get<Reading[]>(`${this.baseUrl}/node/${nodeId}`, { params });
  }

  /**
   * Letzte Readings eines Nodes
   */
  getLatestByNode(nodeId: string): Observable<Reading[]> {
    return this.http.get<Reading[]>(`${this.baseUrl}/latest/${nodeId}`);
  }

  /**
   * Gefilterte Readings (allgemein)
   */
  getFiltered(filter: ReadingFilter): Observable<PaginatedResult<Reading>> {
    let params = new HttpParams();

    Object.entries(filter).forEach(([key, value]) => {
      if (value !== undefined && value !== null) {
        params = params.set(key, value.toString());
      }
    });

    return this.http.get<PaginatedResult<Reading>>(this.baseUrl, { params });
  }

  /**
   * Einzelnes Reading abrufen
   */
  getById(id: number): Observable<Reading> {
    return this.http.get<Reading>(`${this.baseUrl}/${id}`);
  }

  /**
   * Chart-Daten abrufen
   */
  getChartData(
    nodeId: string,
    assignmentId: string,
    measurementType: string,
    interval: ChartInterval = 'OneDay'
  ): Observable<ChartData> {
    const params = new HttpParams().set('interval', interval);
    return this.http.get<ChartData>(
      `${this.baseUrl}/chart/${nodeId}/${assignmentId}/${measurementType}`,
      { params }
    );
  }

  /**
   * CSV-Export URL generieren
   */
  getCsvExportUrl(
    nodeId: string,
    assignmentId: string,
    measurementType: string,
    from?: string,
    to?: string
  ): string {
    let url = `${this.baseUrl}/list/${nodeId}/${assignmentId}/${measurementType}/csv`;
    const params: string[] = [];

    if (from) params.push(`from=${encodeURIComponent(from)}`);
    if (to) params.push(`to=${encodeURIComponent(to)}`);

    if (params.length > 0) {
      url += '?' + params.join('&');
    }

    return url;
  }

  /**
   * Readings im Zeitraum löschen
   */
  deleteRange(
    nodeId: string,
    from: string,
    to: string,
    options?: { assignmentId?: string; measurementType?: string }
  ): Observable<{ deletedCount: number }> {
    return this.http.delete<{ deletedCount: number }>(`${this.baseUrl}/range`, {
      body: {
        nodeId,
        from,
        to,
        ...options
      }
    });
  }
}
```

### Angular Component Beispiel

```typescript
// components/node-readings/node-readings.component.ts

import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { ReadingService } from '../../services/reading.service';
import { Reading, ChartData } from '../../models/reading.model';

@Component({
  selector: 'app-node-readings',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="readings-container">
      <h2>Readings für Node</h2>

      <!-- Filter -->
      <div class="filter-bar">
        <select (change)="onMeasurementTypeChange($event)">
          <option value="">Alle Messwerttypen</option>
          <option value="temperature">Temperatur</option>
          <option value="humidity">Luftfeuchtigkeit</option>
          <option value="co2">CO₂</option>
        </select>

        <input type="date" (change)="onFromDateChange($event)" />
        <input type="date" (change)="onToDateChange($event)" />

        <button (click)="loadReadings()">Filtern</button>
        <button (click)="exportCsv()">CSV Export</button>
      </div>

      <!-- Loading -->
      @if (isLoading()) {
        <div class="loading">Lade Daten...</div>
      }

      <!-- Readings Table -->
      @if (!isLoading() && readings().length > 0) {
        <table class="readings-table">
          <thead>
            <tr>
              <th>Zeitpunkt</th>
              <th>Sensor</th>
              <th>Typ</th>
              <th>Wert</th>
              <th>Einheit</th>
            </tr>
          </thead>
          <tbody>
            @for (reading of readings(); track reading.id) {
              <tr>
                <td>{{ reading.timestamp | date:'dd.MM.yyyy HH:mm:ss' }}</td>
                <td>{{ reading.sensorName }}</td>
                <td>{{ reading.displayName }}</td>
                <td>{{ reading.value | number:'1.1-2' }}</td>
                <td>{{ reading.unit }}</td>
              </tr>
            }
          </tbody>
        </table>
      }

      <!-- Empty State -->
      @if (!isLoading() && readings().length === 0) {
        <div class="empty-state">
          Keine Messwerte gefunden.
        </div>
      }
    </div>
  `
})
export class NodeReadingsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private readingService = inject(ReadingService);

  readings = signal<Reading[]>([]);
  isLoading = signal(false);

  private nodeId = '';
  private measurementType = '';
  private fromDate = '';
  private toDate = '';

  ngOnInit() {
    this.nodeId = this.route.snapshot.paramMap.get('nodeId') || '';
    this.loadReadings();
  }

  loadReadings() {
    if (!this.nodeId) return;

    this.isLoading.set(true);

    this.readingService.getByNode(this.nodeId, {
      measurementType: this.measurementType || undefined,
      from: this.fromDate || undefined,
      to: this.toDate || undefined,
      pageSize: 100
    }).subscribe({
      next: (data) => {
        this.readings.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading readings:', err);
        this.isLoading.set(false);
      }
    });
  }

  onMeasurementTypeChange(event: Event) {
    this.measurementType = (event.target as HTMLSelectElement).value;
  }

  onFromDateChange(event: Event) {
    this.fromDate = (event.target as HTMLInputElement).value;
  }

  onToDateChange(event: Event) {
    this.toDate = (event.target as HTMLInputElement).value;
  }

  exportCsv() {
    // Für CSV-Export brauchen wir assignmentId - hier vereinfacht
    const url = this.readingService.getCsvExportUrl(
      this.nodeId,
      'assignment-id-here', // In Realität aus UI-State
      this.measurementType || 'temperature',
      this.fromDate,
      this.toDate
    );
    window.open(url, '_blank');
  }
}
```

### React/Fetch Beispiel

```typescript
// Vanilla TypeScript/React Beispiel

async function getNodeReadings(
  nodeId: string,
  options?: {
    measurementType?: string;
    from?: string;
    to?: string;
    pageSize?: number;
  }
): Promise<Reading[]> {
  const params = new URLSearchParams();

  if (options?.measurementType) params.set('measurementType', options.measurementType);
  if (options?.from) params.set('from', options.from);
  if (options?.to) params.set('to', options.to);
  if (options?.pageSize) params.set('pageSize', options.pageSize.toString());

  const response = await fetch(
    `http://localhost:5000/api/readings/node/${nodeId}?${params}`,
    {
      method: 'GET',
      headers: {
        'Accept': 'application/json',
      },
    }
  );

  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }

  return response.json();
}

// Verwendung
const readings = await getNodeReadings('3fa85f64-5717-4562-b3fc-2c963f66afa6', {
  measurementType: 'temperature',
  from: '2025-12-01T00:00:00Z',
  to: '2025-12-12T23:59:59Z',
  pageSize: 100
});

console.log(`Loaded ${readings.length} readings`);
```

---

## Postman Collection

Importiere diese Collection in Postman:

```json
{
  "info": {
    "name": "myIoTGrid Cloud API - Readings",
    "description": "API-Endpunkte für Sensor-Messwerte",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "variable": [
    {
      "key": "baseUrl",
      "value": "http://localhost:5000",
      "type": "string"
    },
    {
      "key": "nodeId",
      "value": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "type": "string"
    },
    {
      "key": "assignmentId",
      "value": "a1b2c3d4-5678-90ab-cdef-1234567890ab",
      "type": "string"
    }
  ],
  "item": [
    {
      "name": "Readings",
      "item": [
        {
          "name": "Get Readings by Node",
          "request": {
            "method": "GET",
            "header": [
              {
                "key": "Accept",
                "value": "application/json"
              }
            ],
            "url": {
              "raw": "{{baseUrl}}/api/readings/node/{{nodeId}}?measurementType=temperature&from=2025-12-01T00:00:00Z&pageSize=50",
              "host": ["{{baseUrl}}"],
              "path": ["api", "readings", "node", "{{nodeId}}"],
              "query": [
                { "key": "measurementType", "value": "temperature" },
                { "key": "from", "value": "2025-12-01T00:00:00Z" },
                { "key": "pageSize", "value": "50" }
              ]
            }
          }
        },
        {
          "name": "Get Latest Readings by Node",
          "request": {
            "method": "GET",
            "header": [
              {
                "key": "Accept",
                "value": "application/json"
              }
            ],
            "url": {
              "raw": "{{baseUrl}}/api/readings/latest/{{nodeId}}",
              "host": ["{{baseUrl}}"],
              "path": ["api", "readings", "latest", "{{nodeId}}"]
            }
          }
        },
        {
          "name": "Get Filtered Readings",
          "request": {
            "method": "GET",
            "header": [
              {
                "key": "Accept",
                "value": "application/json"
              }
            ],
            "url": {
              "raw": "{{baseUrl}}/api/readings?measurementType=temperature&from=2025-12-01T00:00:00Z&to=2025-12-12T23:59:59Z&page=1&pageSize=50",
              "host": ["{{baseUrl}}"],
              "path": ["api", "readings"],
              "query": [
                { "key": "measurementType", "value": "temperature" },
                { "key": "from", "value": "2025-12-01T00:00:00Z" },
                { "key": "to", "value": "2025-12-12T23:59:59Z" },
                { "key": "page", "value": "1" },
                { "key": "pageSize", "value": "50" }
              ]
            }
          }
        },
        {
          "name": "Get Paged Readings (with sorting)",
          "request": {
            "method": "GET",
            "header": [
              {
                "key": "Accept",
                "value": "application/json"
              }
            ],
            "url": {
              "raw": "{{baseUrl}}/api/readings/paged?page=1&size=20&sort=timestamp,desc",
              "host": ["{{baseUrl}}"],
              "path": ["api", "readings", "paged"],
              "query": [
                { "key": "page", "value": "1" },
                { "key": "size", "value": "20" },
                { "key": "sort", "value": "timestamp,desc" }
              ]
            }
          }
        },
        {
          "name": "Get Reading by ID",
          "request": {
            "method": "GET",
            "header": [
              {
                "key": "Accept",
                "value": "application/json"
              }
            ],
            "url": {
              "raw": "{{baseUrl}}/api/readings/12345",
              "host": ["{{baseUrl}}"],
              "path": ["api", "readings", "12345"]
            }
          }
        },
        {
          "name": "Get Chart Data",
          "request": {
            "method": "GET",
            "header": [
              {
                "key": "Accept",
                "value": "application/json"
              }
            ],
            "url": {
              "raw": "{{baseUrl}}/api/readings/chart/{{nodeId}}/{{assignmentId}}/temperature?interval=OneDay",
              "host": ["{{baseUrl}}"],
              "path": ["api", "readings", "chart", "{{nodeId}}", "{{assignmentId}}", "temperature"],
              "query": [
                { "key": "interval", "value": "OneDay" }
              ]
            }
          }
        },
        {
          "name": "Get Readings List (paginated)",
          "request": {
            "method": "GET",
            "header": [
              {
                "key": "Accept",
                "value": "application/json"
              }
            ],
            "url": {
              "raw": "{{baseUrl}}/api/readings/list/{{nodeId}}/{{assignmentId}}/temperature?page=1&pageSize=20",
              "host": ["{{baseUrl}}"],
              "path": ["api", "readings", "list", "{{nodeId}}", "{{assignmentId}}", "temperature"],
              "query": [
                { "key": "page", "value": "1" },
                { "key": "pageSize", "value": "20" }
              ]
            }
          }
        },
        {
          "name": "Export CSV",
          "request": {
            "method": "GET",
            "header": [
              {
                "key": "Accept",
                "value": "text/csv"
              }
            ],
            "url": {
              "raw": "{{baseUrl}}/api/readings/list/{{nodeId}}/{{assignmentId}}/temperature/csv?from=2025-12-01&to=2025-12-12",
              "host": ["{{baseUrl}}"],
              "path": ["api", "readings", "list", "{{nodeId}}", "{{assignmentId}}", "temperature", "csv"],
              "query": [
                { "key": "from", "value": "2025-12-01" },
                { "key": "to", "value": "2025-12-12" }
              ]
            }
          }
        },
        {
          "name": "Create Reading",
          "request": {
            "method": "POST",
            "header": [
              {
                "key": "Content-Type",
                "value": "application/json"
              }
            ],
            "body": {
              "mode": "raw",
              "raw": "{\n  \"deviceId\": \"ESP32-WROOM-001\",\n  \"type\": \"temperature\",\n  \"value\": 21.5,\n  \"unit\": \"°C\",\n  \"endpointId\": 1\n}"
            },
            "url": {
              "raw": "{{baseUrl}}/api/readings",
              "host": ["{{baseUrl}}"],
              "path": ["api", "readings"]
            }
          }
        },
        {
          "name": "Batch Upload Readings",
          "request": {
            "method": "POST",
            "header": [
              {
                "key": "Content-Type",
                "value": "application/json"
              }
            ],
            "body": {
              "mode": "raw",
              "raw": "{\n  \"nodeId\": \"ESP32-WROOM-001\",\n  \"readings\": [\n    {\"endpointId\": 1, \"measurementType\": \"temperature\", \"rawValue\": 21.5},\n    {\"endpointId\": 1, \"measurementType\": \"humidity\", \"rawValue\": 45.2},\n    {\"endpointId\": 2, \"measurementType\": \"co2\", \"rawValue\": 823}\n  ]\n}"
            },
            "url": {
              "raw": "{{baseUrl}}/api/readings/batch",
              "host": ["{{baseUrl}}"],
              "path": ["api", "readings", "batch"]
            }
          }
        },
        {
          "name": "Delete Readings Range",
          "request": {
            "method": "DELETE",
            "header": [
              {
                "key": "Content-Type",
                "value": "application/json"
              }
            ],
            "body": {
              "mode": "raw",
              "raw": "{\n  \"nodeId\": \"{{nodeId}}\",\n  \"from\": \"2025-12-01T00:00:00Z\",\n  \"to\": \"2025-12-05T23:59:59Z\",\n  \"measurementType\": \"temperature\"\n}"
            },
            "url": {
              "raw": "{{baseUrl}}/api/readings/range",
              "host": ["{{baseUrl}}"],
              "path": ["api", "readings", "range"]
            }
          }
        }
      ]
    }
  ]
}
```

### Postman Collection importieren

1. Öffne Postman
2. Klicke auf **Import** (oben links)
3. Wähle **Raw text** und füge die JSON-Collection ein
4. Klicke **Import**
5. Passe die Variablen an:
   - `baseUrl`: Deine API-URL
   - `nodeId`: Eine gültige Node-ID aus deiner Datenbank
   - `assignmentId`: Eine gültige Assignment-ID

---

## Fehlerbehandlung

### HTTP Status Codes

| Code | Bedeutung | Beschreibung |
|------|-----------|--------------|
| 200 | OK | Request erfolgreich |
| 201 | Created | Resource erstellt |
| 204 | No Content | Erfolgreich, keine Daten |
| 400 | Bad Request | Ungültige Parameter |
| 401 | Unauthorized | Authentifizierung erforderlich |
| 404 | Not Found | Resource nicht gefunden |
| 500 | Internal Server Error | Server-Fehler |

### Error Response Format

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "DeviceId is required",
  "traceId": "00-1234567890abcdef-1234567890abcdef-00"
}
```

### Frontend Error Handling

```typescript
// Angular Error Interceptor
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMessage = 'Ein unbekannter Fehler ist aufgetreten';

      if (error.error instanceof ErrorEvent) {
        // Client-side error
        errorMessage = error.error.message;
      } else {
        // Server-side error
        switch (error.status) {
          case 400:
            errorMessage = error.error?.detail || 'Ungültige Anfrage';
            break;
          case 401:
            errorMessage = 'Authentifizierung erforderlich';
            // Redirect to login
            break;
          case 404:
            errorMessage = 'Daten nicht gefunden';
            break;
          case 500:
            errorMessage = 'Server-Fehler. Bitte später erneut versuchen.';
            break;
        }
      }

      console.error('API Error:', errorMessage, error);
      return throwError(() => new Error(errorMessage));
    })
  );
};
```

---

## Häufige Anwendungsfälle

### 1. Dashboard: Aktuelle Werte aller Sensoren eines Nodes

```typescript
// Lade die neuesten Werte
const latestReadings = await readingService.getLatestByNode(nodeId);

// Gruppiere nach Sensor
const sensorGroups = latestReadings.reduce((acc, reading) => {
  const key = reading.sensorCode;
  if (!acc[key]) acc[key] = [];
  acc[key].push(reading);
  return acc;
}, {} as Record<string, Reading[]>);
```

### 2. Zeitreihen-Chart für Temperatur (letzte 24h)

```typescript
const chartData = await readingService.getChartData(
  nodeId,
  assignmentId,
  'temperature',
  'OneDay'
);

// Für Chart.js oder ähnliche Libraries
const labels = chartData.dataPoints.map(p => new Date(p.timestamp));
const values = chartData.dataPoints.map(p => p.value);
```

### 3. Export aller Temperaturwerte eines Monats

```typescript
// Generiere Download-URL
const csvUrl = readingService.getCsvExportUrl(
  nodeId,
  assignmentId,
  'temperature',
  '2025-12-01T00:00:00Z',
  '2025-12-31T23:59:59Z'
);

// Trigger Download
const link = document.createElement('a');
link.href = csvUrl;
link.download = 'temperature_december_2025.csv';
link.click();
```

### 4. Infinite Scroll für Readings-Tabelle

```typescript
let page = 1;
const pageSize = 50;
let hasMore = true;

async function loadMore() {
  if (!hasMore) return;

  const result = await readingService.getFiltered({
    nodeId,
    measurementType: 'temperature',
    page,
    pageSize
  });

  readings.push(...result.items);
  hasMore = page < result.totalPages;
  page++;
}
```

---

**Version:** 1.0 | **Stand:** Dezember 2025 | **myIoTGrid Cloud API**
