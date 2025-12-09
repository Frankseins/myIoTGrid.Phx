# Feature Pattern Guide - myIoTGrid Frontend

**Basierend auf:** Sensor-Types Feature
**Version:** 1.1
**Erstellt:** 01.12.2025
**Aktualisiert:** 01.12.2025

---

## 1. Übersicht

Diese Dokumentation beschreibt das Standard-Pattern für CRUD-Features im myIoTGrid Frontend. Alle neuen Features (Sensors, Nodes, Alerts, etc.) sollten diesem Pattern folgen.

### Architektur-Überblick

```
┌─────────────────────────────────────────────────────────────────┐
│                        FEATURE PATTERN                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────────────┐        ┌─────────────────────┐        │
│   │    List Component   │───────▶│   Form Component    │        │
│   │  (Master-Ansicht)   │        │  (Detail-Ansicht)   │        │
│   └──────────┬──────────┘        └──────────┬──────────┘        │
│              │                              │                   │
│              ▼                              ▼                   │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │              GenericTableComponent                       │  │
│   │  • Server-side Pagination  • Sorting  • Filtering        │  │
│   │  • State Persistence       • Custom Templates            │  │
│   └──────────────────────────────────────────────────────────┘  │
│                              │                                  │
│   ┌──────────────────────────┼──────────────────────────────┐  │
│   │                          ▼                               │  │
│   │              API Service (data-access)                   │  │
│   │  • HTTP-Aufrufe  • Caching  • Signal-basierte State      │  │
│   └──────────────────────────────────────────────────────────┘  │
│                              │                                  │
│   ┌──────────────────────────┼──────────────────────────────┐  │
│   │                          ▼                               │  │
│   │                    Models (shared)                       │  │
│   │  • Interfaces  • DTOs  • Enums  • Constants              │  │
│   └──────────────────────────────────────────────────────────┘  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Verzeichnisstruktur

```
libs/
├── features/
│   └── {feature-name}/                    # z.B. sensor-types, sensors, nodes
│       └── src/lib/
│           ├── components/
│           │   ├── {feature}-list/        # Master-Liste
│           │   │   ├── {feature}-list.component.ts
│           │   │   ├── {feature}-list.component.html
│           │   │   └── {feature}-list.component.scss
│           │   └── {feature}-form/        # Detail-Formular
│           │       ├── {feature}-form.component.ts
│           │       ├── {feature}-form.component.html
│           │       └── {feature}-form.component.scss
│           ├── index.ts                   # Public API
│           └── {feature}.routes.ts        # Routing
│
├── shared/
│   ├── ui/src/lib/
│   │   ├── components/
│   │   │   ├── generic-table/             # Wiederverwendbare Tabelle
│   │   │   └── confirm-dialog/            # Bestätigungs-Dialog
│   │   ├── services/
│   │   │   └── table-state.service.ts     # State Persistence
│   │   └── index.ts
│   │
│   ├── data-access/src/lib/
│   │   ├── api-query.helper.ts            # Query-Utilities
│   │   ├── base-api.service.ts            # HTTP-Basis
│   │   ├── {feature}-api.service.ts       # Feature-spezifischer API-Service
│   │   └── index.ts
│   │
│   └── models/src/lib/
│       ├── {feature}.model.ts             # Feature-Models & DTOs
│       ├── paged-result.model.ts          # Pagination-Model
│       ├── query-params.model.ts          # Query-Parameter
│       ├── column-config.model.ts         # Tabellen-Konfiguration
│       └── index.ts
```

---

## 3. Shared Models

### 3.1 PagedResult (Pagination)

**Datei:** `libs/shared/models/src/lib/paged-result.model.ts`

```typescript
export interface PagedResult<T> {
  items: T[];
  totalRecords: number;
  page: number;
  size: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
```

### 3.2 QueryParams (API-Abfragen)

**Datei:** `libs/shared/models/src/lib/query-params.model.ts`

```typescript
export interface QueryParams {
  page: number;                           // 0-basierter Index
  size: number;                           // Einträge pro Seite
  sort?: string;                          // "field,asc" oder "field,desc"
  search?: string;                        // Globale Suche
  dateFrom?: Date;
  dateTo?: Date;
  filters?: Record<string, string>;       // Zusätzliche Filter
}

export const DEFAULT_QUERY_PARAMS: QueryParams = {
  page: 0,
  size: 10
};
```

### 3.3 ColumnConfig (Tabellen-Spalten)

**Datei:** `libs/shared/models/src/lib/column-config.model.ts`

```typescript
export interface GenericTableColumn {
  field: string;                          // Property-Name im Datenobjekt
  header: string;                         // Spalten-Überschrift
  width?: string;                         // CSS-Breite (z.B. '150px', '20%')
  sortable?: boolean;                     // Sortierung erlauben
  type?: 'text' | 'boolean' | 'date' | 'number';  // Rendering-Typ
}

export interface MaterialLazyEvent {
  first: number;                          // Erster Zeilen-Index (0-basiert)
  rows: number;                           // Zeilen pro Seite
  sortField?: string;                     // Sortiertes Feld
  sortOrder?: 1 | -1;                     // 1=asc, -1=desc
  globalFilter?: string;                  // Suchbegriff
  filters?: Record<string, unknown>;      // Spalten-Filter
}

export type DetailMode = 'drawer' | 'route' | 'none';
```

---

## 4. Feature Model erstellen

**Datei:** `libs/shared/models/src/lib/{feature}.model.ts`

### Beispiel: Sensor-Model

```typescript
// ============================================
// ENUMS
// ============================================
export enum SensorStatus {
  Active = 1,
  Inactive = 2,
  Error = 3,
  Maintenance = 4
}

export const SENSOR_STATUS_LABELS: Record<SensorStatus, string> = {
  [SensorStatus.Active]: 'Aktiv',
  [SensorStatus.Inactive]: 'Inaktiv',
  [SensorStatus.Error]: 'Fehler',
  [SensorStatus.Maintenance]: 'Wartung'
};

// ============================================
// INTERFACES
// ============================================
export interface Sensor {
  id: string;
  nodeId: string;
  sensorTypeId: string;
  name: string;
  description?: string;
  status: SensorStatus;
  isActive: boolean;
  lastReadingAt?: string;
  lastValue?: number;
  createdAt: string;
  updatedAt: string;

  // Navigation Properties (expandiert)
  node?: Node;
  sensorType?: SensorType;
}

// ============================================
// DTOs (Data Transfer Objects)
// ============================================
export interface CreateSensorDto {
  nodeId: string;
  sensorTypeId: string;
  name: string;
  description?: string;
}

export interface UpdateSensorDto {
  name?: string;
  description?: string;
  isActive?: boolean;
}

// ============================================
// QUERY DTOs
// ============================================
export interface SensorQueryDto {
  page?: number;
  size?: number;
  sort?: string;
  search?: string;
  nodeId?: string;
  sensorTypeId?: string;
  status?: SensorStatus;
  isActive?: boolean;
}

// ============================================
// FILTER OPTIONS
// ============================================
export interface SensorFilterOptions {
  nodes: { value: string; label: string }[];
  sensorTypes: { value: string; label: string }[];
  statuses: { value: SensorStatus; label: string }[];
}
```

### Export in index.ts

**Datei:** `libs/shared/models/src/lib/index.ts`

```typescript
// ... bestehende Exports
export * from './sensor.model';
```

---

## 5. API Service erstellen

**Datei:** `libs/shared/data-access/src/lib/{feature}-api.service.ts`

### Template

```typescript
import { Injectable, signal, computed } from '@angular/core';
import { Observable, tap, shareReplay } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { queryParamsToObject } from './api-query.helper';
import {
  Sensor,
  CreateSensorDto,
  UpdateSensorDto,
  SensorQueryDto,
  PagedResult,
  QueryParams
} from '@myiotgrid/shared/models';

@Injectable({ providedIn: 'root' })
export class SensorApiService extends BaseApiService {
  private readonly endpoint = '/sensors';

  // ============================================
  // CACHING & STATE (Optional)
  // ============================================
  private cache$?: Observable<Sensor[]>;
  readonly sensors = signal<Sensor[]>([]);
  readonly sensorsMap = signal<Map<string, Sensor>>(new Map());
  readonly isLoaded = signal(false);

  // ============================================
  // API METHODEN
  // ============================================

  /**
   * Alle Sensoren abrufen (mit Caching)
   */
  getAll(forceRefresh = false): Observable<Sensor[]> {
    if (!this.cache$ || forceRefresh) {
      this.cache$ = this.get<Sensor[]>(this.endpoint).pipe(
        tap(sensors => {
          this.sensors.set(sensors);
          this.sensorsMap.set(new Map(sensors.map(s => [s.id, s])));
          this.isLoaded.set(true);
        }),
        shareReplay(1)
      );
    }
    return this.cache$;
  }

  /**
   * Paginierte Abfrage
   */
  getPaged(params: QueryParams): Observable<PagedResult<Sensor>> {
    return this.get<PagedResult<Sensor>>(
      `${this.endpoint}/paged`,
      queryParamsToObject(params)
    );
  }

  /**
   * Einzelnen Sensor abrufen
   */
  getById(id: string): Observable<Sensor> {
    return this.get<Sensor>(`${this.endpoint}/${id}`);
  }

  /**
   * Sensor erstellen
   */
  create(dto: CreateSensorDto): Observable<Sensor> {
    return this.post<Sensor>(this.endpoint, dto).pipe(
      tap(() => this.clearCache())
    );
  }

  /**
   * Sensor aktualisieren
   */
  update(id: string, dto: UpdateSensorDto): Observable<Sensor> {
    return this.put<Sensor>(`${this.endpoint}/${id}`, dto).pipe(
      tap(() => this.clearCache())
    );
  }

  /**
   * Sensor löschen
   */
  deleteSensor(id: string): Observable<void> {
    return this.delete<void>(`${this.endpoint}/${id}`).pipe(
      tap(() => this.clearCache())
    );
  }

  // ============================================
  // SYNCHRONE HELPER (nach Cache-Load)
  // ============================================

  getDisplayName(id: string): string {
    return this.sensorsMap().get(id)?.name ?? 'Unbekannt';
  }

  getSensor(id: string): Sensor | undefined {
    return this.sensorsMap().get(id);
  }

  // ============================================
  // CACHE MANAGEMENT
  // ============================================

  clearCache(): void {
    this.cache$ = undefined;
    this.isLoaded.set(false);
  }
}
```

### Export in index.ts

**Datei:** `libs/shared/data-access/src/lib/index.ts`

```typescript
// ... bestehende Exports
export * from './sensor-api.service';
```

---

## 6. List Component erstellen

**Datei:** `libs/features/{feature}/src/lib/components/{feature}-list/{feature}-list.component.ts`

### Template

```typescript
import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';

import {
  GenericTableComponent,
  ColumnTemplateDirective,
  EmptyStateComponent,
  LoadingSpinnerComponent
} from '@myiotgrid/shared/ui';
import { SensorApiService } from '@myiotgrid/shared/data-access';
import {
  Sensor,
  SensorStatus,
  SENSOR_STATUS_LABELS,
  GenericTableColumn,
  MaterialLazyEvent,
  QueryParams
} from '@myiotgrid/shared/models';

@Component({
  selector: 'myiotgrid-sensor-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatSnackBarModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    GenericTableComponent,
    ColumnTemplateDirective,
    EmptyStateComponent,
    LoadingSpinnerComponent
  ],
  templateUrl: './sensor-list.component.html',
  styleUrls: ['./sensor-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SensorListComponent implements OnInit {
  // ============================================
  // DATA
  // ============================================
  sensors: Sensor[] = [];
  totalRecords = 0;
  loading = false;
  initialLoadDone = false;  // Für Loading/Empty State Anzeige
  globalFilter = '';

  // ============================================
  // SPALTEN-KONFIGURATION
  // ============================================
  columns: GenericTableColumn[] = [
    { field: 'name', header: 'Name', sortable: true },
    { field: 'sensorType', header: 'Typ', width: '150px', sortable: true },
    { field: 'node', header: 'Node', width: '150px', sortable: true },
    { field: 'status', header: 'Status', width: '120px', sortable: true },
    { field: 'lastValue', header: 'Letzter Wert', width: '120px', sortable: true, type: 'number' },
    { field: 'lastReadingAt', header: 'Letzte Messung', width: '160px', sortable: true, type: 'date' },
    { field: 'isActive', header: 'Aktiv', width: '80px', sortable: true, type: 'boolean' }
  ];

  // ============================================
  // FILTER-OPTIONEN
  // ============================================
  statuses = Object.entries(SENSOR_STATUS_LABELS).map(([key, label]) => ({
    value: parseInt(key) as SensorStatus,
    label
  }));

  // ============================================
  // FIELD MAPPING (Frontend camelCase → Backend PascalCase)
  // ============================================
  private readonly fieldMapping: Record<string, string> = {
    name: 'Name',
    sensorType: 'SensorType.Name',
    node: 'Node.Name',
    status: 'Status',
    lastValue: 'LastValue',
    lastReadingAt: 'LastReadingAt',
    isActive: 'IsActive'
  };

  private lastQueryParams: QueryParams | null = null;
  private currentFilters: Record<string, unknown> = {};

  constructor(
    private router: Router,
    private sensorApiService: SensorApiService,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  // ============================================
  // LIFECYCLE
  // ============================================
  ngOnInit(): void {
    this.loadSensorsLazy({
      first: 0,
      rows: 10,
      sortField: 'name',
      sortOrder: 1,
      globalFilter: this.globalFilter
    });
  }

  // ============================================
  // DATA LOADING
  // ============================================
  loadSensorsLazy(event: MaterialLazyEvent): void {
    const currentSearchTerm = event.globalFilter || '';

    // Suche ignorieren wenn < 3 Zeichen
    if (currentSearchTerm.length > 0 && currentSearchTerm.length < 3) {
      return;
    }

    this.loading = true;

    // Pagination berechnen
    const page = Math.floor((event.first ?? 0) / (event.rows ?? 10));
    const size = event.rows ?? 10;

    // Sort-String mit Backend-Feldnamen erstellen
    let sort = 'Name,asc';
    if (event.sortField) {
      const direction = event.sortOrder === 1 ? 'asc' : 'desc';
      const backendField = this.fieldMapping[event.sortField] || event.sortField;
      sort = `${backendField},${direction}`;
    }

    const search = currentSearchTerm.length >= 3 ? currentSearchTerm : undefined;

    // Filter-Objekt erstellen
    const filters: Record<string, string> = {};
    if (event.filters?.['status'] !== undefined && event.filters['status'] !== '') {
      filters['status'] = String(event.filters['status']);
    }
    if (event.filters?.['nodeId']) {
      filters['nodeId'] = String(event.filters['nodeId']);
    }
    if (event.filters?.['sensorTypeId']) {
      filters['sensorTypeId'] = String(event.filters['sensorTypeId']);
    }

    const query: QueryParams = {
      page,
      size,
      sort,
      search,
      filters: Object.keys(filters).length > 0 ? filters : undefined
    };

    this.lastQueryParams = query;

    // API aufrufen
    this.sensorApiService.getPaged(query).subscribe({
      next: (data) => {
        this.sensors = data.items;
        this.totalRecords = data.totalRecords;
        this.loading = false;
        this.initialLoadDone = true;  // Wichtig: Erst nach dem ersten Load setzen
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Error loading sensors:', error);
        this.snackBar.open(
          error.message || 'Fehler beim Laden der Sensoren',
          'Schließen',
          { duration: 5000, panelClass: ['snackbar-error'] }
        );
        this.loading = false;
        this.initialLoadDone = true;  // Auch bei Fehler setzen
        this.cdr.markForCheck();
      }
    });
  }

  // ============================================
  // CRUD ACTIONS
  // ============================================
  onCreate(): void {
    this.router.navigate(['/sensors', 'new']);
  }

  onEdit(_sensor: Sensor): void {
    // Navigation wird durch GenericTable mit detailMode='route' gehandhabt
  }

  deleteSensor(sensor: Sensor): void {
    this.sensorApiService.deleteSensor(sensor.id).subscribe({
      next: () => {
        this.snackBar.open('Sensor erfolgreich gelöscht', 'Schließen', {
          duration: 3000,
          panelClass: ['snackbar-success']
        });
        this.reloadTable();
      },
      error: (error) => {
        this.snackBar.open(
          error.message || 'Fehler beim Löschen',
          'Schließen',
          { duration: 5000, panelClass: ['snackbar-error'] }
        );
      }
    });
  }

  // ============================================
  // TABLE HELPERS
  // ============================================
  reloadTable(): void {
    if (!this.lastQueryParams) {
      this.loadSensorsLazy({
        first: 0,
        rows: 10,
        sortField: 'name',
        sortOrder: 1,
        globalFilter: this.globalFilter
      });
      return;
    }

    const { page = 0, size = 10, sort = 'Name,asc' } = this.lastQueryParams;
    const [backendSortField, direction] = sort.split(',');
    const frontendSortField = Object.entries(this.fieldMapping)
      .find(([_, backend]) => backend === backendSortField)?.[0] || 'name';

    this.loadSensorsLazy({
      first: page * size,
      rows: size,
      sortField: frontendSortField,
      sortOrder: direction === 'desc' ? -1 : 1,
      globalFilter: this.globalFilter
    });
  }

  onFilterChange(filters: Record<string, unknown>): void {
    this.currentFilters = filters;
    this.loadSensorsLazy({
      first: 0,
      rows: 10,
      sortField: 'name',
      sortOrder: 1,
      globalFilter: this.globalFilter,
      filters: filters
    });
  }

  // ============================================
  // DISPLAY HELPERS
  // ============================================
  getStatusLabel(status: SensorStatus): string {
    return SENSOR_STATUS_LABELS[status] || 'Unbekannt';
  }
}
```

### HTML Template

**Datei:** `libs/features/{feature}/src/lib/components/{feature}-list/{feature}-list.component.html`

```html
<div class="sensor-list-page">
  <!-- ============================================ -->
  <!-- LOADING / EMPTY / TABLE STATES               -->
  <!-- ============================================ -->
  @if (!initialLoadDone) {
    <!-- Loading Spinner während erstem Laden -->
    <myiotgrid-loading-spinner message="Sensoren werden geladen..."></myiotgrid-loading-spinner>
  } @else if (totalRecords === 0) {
    <!-- Empty State wenn keine Daten vorhanden -->
    <myiotgrid-empty-state
      icon="sensors"
      title="Keine Sensoren"
      message="Es wurden noch keine Sensoren angelegt."
      actionLabel="Neuer Sensor"
      (actionClicked)="onCreate()">
    </myiotgrid-empty-state>
  } @else {
    <!-- Page Header (nur wenn Daten vorhanden) -->
    <header class="page-header">
      <div class="header-content">
        <h1 class="page-title">Sensoren</h1>
        <p class="page-subtitle">Sensor-Instanzen verwalten</p>
      </div>
    </header>

    <!-- Data Table -->
    <myiotgrid-generic-table
  [data]="sensors"
  [totalRecords]="totalRecords"
  [loading]="loading"
  [columns]="columns"
  dataKey="id"
  [stateKey]="'sensors-table'"
  [globalFilterFields]="['id', 'name', 'description']"
  createLabel="Neuer Sensor"
  [useGlobalSearch]="false"
  detailTitle="Sensor"
  (lazyLoad)="loadSensorsLazy($event)"
  (edit)="onEdit($any($event))"
  (remove)="deleteSensor($any($event))"
  (create)="onCreate()"
  [detailMode]="'route'"
  detailRouteBase="/sensors"
  [showFilter]="true"
  filterTitle="Sensoren filtern"
  (filterChange)="onFilterChange($event)">

  <!-- ============================================ -->
  <!-- FILTER TEMPLATE                              -->
  <!-- ============================================ -->
  <ng-template #filterTemplate let-filters>
    <div class="filter-fields">
      <!-- Status Filter -->
      <mat-form-field appearance="outline">
        <mat-label>Status</mat-label>
        <mat-select
          [ngModel]="filters['status']"
          (ngModelChange)="filters['status'] = $event">
          <mat-option [value]="''">Alle</mat-option>
          @for (status of statuses; track status.value) {
            <mat-option [value]="status.value">{{ status.label }}</mat-option>
          }
        </mat-select>
      </mat-form-field>

      <!-- Node Filter (falls Lookup verfügbar) -->
      <!-- <mat-form-field appearance="outline">
        <mat-label>Node</mat-label>
        <mat-select
          [ngModel]="filters['nodeId']"
          (ngModelChange)="filters['nodeId'] = $event">
          <mat-option [value]="''">Alle</mat-option>
          @for (node of nodes; track node.value) {
            <mat-option [value]="node.value">{{ node.label }}</mat-option>
          }
        </mat-select>
      </mat-form-field> -->
    </div>
  </ng-template>

  <!-- ============================================ -->
  <!-- CUSTOM COLUMN TEMPLATES                      -->
  <!-- ============================================ -->

  <!-- Status Column -->
  <ng-template [myiotgridColumnTemplate]="'status'" let-row>
    <span class="status-chip" [class]="'status-' + row.status">
      {{ getStatusLabel(row.status) }}
    </span>
  </ng-template>

  <!-- SensorType Column -->
  <ng-template [myiotgridColumnTemplate]="'sensorType'" let-row>
    @if (row.sensorType) {
      <div class="sensor-type-info">
        @if (row.sensorType.icon) {
          <mat-icon [style.color]="row.sensorType.color">
            {{ row.sensorType.icon }}
          </mat-icon>
        }
        <span>{{ row.sensorType.name }}</span>
      </div>
    } @else {
      <span class="text-muted">-</span>
    }
  </ng-template>

  <!-- Node Column -->
  <ng-template [myiotgridColumnTemplate]="'node'" let-row>
    @if (row.node) {
      <span>{{ row.node.name }}</span>
    } @else {
      <span class="text-muted">-</span>
    }
  </ng-template>

  <!-- LastValue Column -->
  <ng-template [myiotgridColumnTemplate]="'lastValue'" let-row>
    @if (row.lastValue !== null && row.lastValue !== undefined) {
      <span class="value">
        {{ row.lastValue | number:'1.1-2' }}
        @if (row.sensorType?.capabilities?.[0]?.unit) {
          <span class="unit">{{ row.sensorType.capabilities[0].unit }}</span>
        }
      </span>
    } @else {
      <span class="text-muted">-</span>
    }
  </ng-template>

    </myiotgrid-generic-table>
  }
</div>
```

### SCSS Styles

**Datei:** `libs/features/{feature}/src/lib/components/{feature}-list/{feature}-list.component.scss`

```scss
@use 'sass:map';

// Page Container
.sensor-list-page {
  // Volle Breite
}

// Page Header (nur sichtbar wenn Daten vorhanden)
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 24px;
  flex-wrap: wrap;
  gap: 16px;
  padding-left: 25px;  // Einrückung für Header-Text

  .header-content {
    .page-title {
      margin: 0;
      font-size: 28px;
      font-weight: 500;
      color: #212121;
    }

    .page-subtitle {
      margin: 8px 0 0 0;
      font-size: 14px;
      color: #757575;
    }
  }
}

// Filter Layout
.filter-fields {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 16px 0;

  mat-form-field {
    width: 100%;
  }
}

// Status Chips
.status-chip {
  display: inline-flex;
  align-items: center;
  padding: 4px 12px;
  border-radius: 16px;
  font-size: 12px;
  font-weight: 500;

  &.status-1 { // Active
    background-color: #c8e6c9;
    color: #2e7d32;
  }

  &.status-2 { // Inactive
    background-color: #e0e0e0;
    color: #616161;
  }

  &.status-3 { // Error
    background-color: #ffcdd2;
    color: #c62828;
  }

  &.status-4 { // Maintenance
    background-color: #fff9c4;
    color: #f9a825;
  }
}

// Sensor Type Info
.sensor-type-info {
  display: flex;
  align-items: center;
  gap: 8px;

  mat-icon {
    font-size: 18px;
    width: 18px;
    height: 18px;
  }
}

// Value Display
.value {
  font-weight: 500;

  .unit {
    font-size: 12px;
    color: #666;
    margin-left: 4px;
  }
}

// Muted Text
.text-muted {
  color: #999;
}
```

---

## 7. Form Component erstellen

**Datei:** `libs/features/{feature}/src/lib/components/{feature}-form/{feature}-form.component.ts`

### Template

```typescript
import {
  Component,
  OnInit,
  signal,
  computed,
  ChangeDetectionStrategy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';

import {
  SensorApiService,
  NodeApiService,
  SensorTypeApiService
} from '@myiotgrid/shared/data-access';
import {
  Sensor,
  CreateSensorDto,
  UpdateSensorDto,
  Node,
  SensorType
} from '@myiotgrid/shared/models';

type FormMode = 'view' | 'edit' | 'create';

@Component({
  selector: 'myiotgrid-sensor-form',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDividerModule,
    MatTooltipModule
  ],
  templateUrl: './sensor-form.component.html',
  styleUrls: ['./sensor-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SensorFormComponent implements OnInit {
  // ============================================
  // SIGNALS
  // ============================================
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly mode = signal<FormMode>('view');
  readonly sensor = signal<Sensor | null>(null);

  // Lookup-Daten
  readonly nodes = signal<Node[]>([]);
  readonly sensorTypes = signal<SensorType[]>([]);

  // ============================================
  // COMPUTED
  // ============================================
  readonly isViewMode = computed(() => this.mode() === 'view');
  readonly isEditMode = computed(() => this.mode() === 'edit');
  readonly isCreateMode = computed(() => this.mode() === 'create');
  readonly isReadonly = computed(() => this.mode() === 'view');

  readonly pageTitle = computed(() => {
    switch (this.mode()) {
      case 'create': return 'Neuer Sensor';
      case 'edit': return 'Sensor bearbeiten';
      default: return 'Sensor Details';
    }
  });

  readonly canEdit = computed(() => {
    const s = this.sensor();
    // Beispiel: Globale Sensoren können nicht bearbeitet werden
    return !s || !s.isGlobal;
  });

  // ============================================
  // FORM
  // ============================================
  form!: FormGroup;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar,
    private sensorApiService: SensorApiService,
    private nodeApiService: NodeApiService,
    private sensorTypeApiService: SensorTypeApiService
  ) {}

  // ============================================
  // LIFECYCLE
  // ============================================
  ngOnInit(): void {
    this.initForm();
    this.loadLookupData();

    const id = this.route.snapshot.paramMap.get('id');
    const queryMode = this.route.snapshot.queryParamMap.get('mode');

    if (id === 'new') {
      this.mode.set('create');
      this.isLoading.set(false);
    } else if (id) {
      this.mode.set(queryMode === 'edit' ? 'edit' : 'view');
      this.loadSensor(id);
    }
  }

  // ============================================
  // FORM INITIALIZATION
  // ============================================
  private initForm(): void {
    this.form = this.fb.group({
      // Basis-Informationen
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: [''],

      // Beziehungen
      nodeId: ['', [Validators.required]],
      sensorTypeId: ['', [Validators.required]],

      // Status
      isActive: [true]
    });
  }

  // ============================================
  // DATA LOADING
  // ============================================
  private loadLookupData(): void {
    // Nodes laden
    this.nodeApiService.getAll().subscribe({
      next: (nodes) => this.nodes.set(nodes),
      error: (error) => console.error('Error loading nodes:', error)
    });

    // SensorTypes laden
    this.sensorTypeApiService.getAll().subscribe({
      next: (types) => this.sensorTypes.set(types),
      error: (error) => console.error('Error loading sensor types:', error)
    });
  }

  private loadSensor(id: string): void {
    this.isLoading.set(true);

    this.sensorApiService.getById(id).subscribe({
      next: (sensor) => {
        this.sensor.set(sensor);
        this.patchForm(sensor);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading sensor:', error);
        this.snackBar.open(
          error?.error?.detail || 'Sensor nicht gefunden',
          'Schließen',
          { duration: 5000, panelClass: ['snackbar-error'] }
        );
        this.router.navigate(['/sensors']);
      }
    });
  }

  private patchForm(sensor: Sensor): void {
    this.form.patchValue({
      name: sensor.name,
      description: sensor.description || '',
      nodeId: sensor.nodeId,
      sensorTypeId: sensor.sensorTypeId,
      isActive: sensor.isActive
    });
  }

  // ============================================
  // ACTIONS
  // ============================================
  toggleEditMode(): void {
    if (this.isViewMode()) {
      this.mode.set('edit');
    } else {
      this.mode.set('view');
      const s = this.sensor();
      if (s) {
        this.patchForm(s);
      }
    }
  }

  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    const formValue = this.form.getRawValue();

    if (this.isCreateMode()) {
      // CREATE
      const createDto: CreateSensorDto = {
        name: formValue.name,
        description: formValue.description || undefined,
        nodeId: formValue.nodeId,
        sensorTypeId: formValue.sensorTypeId
      };

      this.sensorApiService.create(createDto).subscribe({
        next: () => {
          this.snackBar.open('Sensor erstellt', 'Schließen', {
            duration: 3000,
            panelClass: ['snackbar-success']
          });
          // WICHTIG: Nach Erstellen zur Liste navigieren
          this.router.navigate(['/sensors']);
        },
        error: (error) => {
          this.snackBar.open(
            error?.error?.detail || 'Fehler beim Erstellen',
            'Schließen',
            { duration: 5000, panelClass: ['snackbar-error'] }
          );
          this.isSaving.set(false);
        }
      });
    } else {
      // UPDATE
      const updateDto: UpdateSensorDto = {
        name: formValue.name,
        description: formValue.description || undefined,
        isActive: formValue.isActive
      };

      const currentId = this.sensor()?.id;
      if (currentId) {
        this.sensorApiService.update(currentId, updateDto).subscribe({
          next: () => {
            this.snackBar.open('Sensor aktualisiert', 'Schließen', {
              duration: 3000,
              panelClass: ['snackbar-success']
            });
            // WICHTIG: Nach Update zur Liste navigieren
            this.router.navigate(['/sensors']);
          },
          error: (error) => {
            this.snackBar.open(
              error?.error?.detail || 'Fehler beim Aktualisieren',
              'Schließen',
              { duration: 5000, panelClass: ['snackbar-error'] }
            );
            this.isSaving.set(false);
          }
        });
      }
    }
  }

  /**
   * Abbrechen navigiert IMMER zur Liste zurück
   * (sowohl bei Create als auch bei Edit)
   */
  onCancel(): void {
    this.router.navigate(['/sensors']);
  }

  onDelete(): void {
    const s = this.sensor();
    if (!s) return;

    // TODO: ConfirmDialog verwenden
    if (confirm(`Sensor "${s.name}" wirklich löschen?`)) {
      this.sensorApiService.deleteSensor(s.id).subscribe({
        next: () => {
          this.snackBar.open('Sensor gelöscht', 'Schließen', {
            duration: 3000,
            panelClass: ['snackbar-success']
          });
          this.router.navigate(['/sensors']);
        },
        error: (error) => {
          this.snackBar.open(
            error?.error?.detail || 'Fehler beim Löschen',
            'Schließen',
            { duration: 5000, panelClass: ['snackbar-error'] }
          );
        }
      });
    }
  }

  goBack(): void {
    this.router.navigate(['/sensors']);
  }
}
```

### HTML Template

**Datei:** `libs/features/{feature}/src/lib/components/{feature}-form/{feature}-form.component.html`

```html
<div class="form-container">
  <!-- ============================================ -->
  <!-- HEADER                                       -->
  <!-- ============================================ -->
  <div class="form-header">
    <div class="header-left">
      <button mat-icon-button (click)="goBack()" matTooltip="Zurück zur Liste">
        <mat-icon>arrow_back</mat-icon>
      </button>
      <h1>{{ pageTitle() }}</h1>
    </div>

    <div class="header-actions">
      @if (!isCreateMode() && canEdit()) {
        <button
          mat-icon-button
          (click)="toggleEditMode()"
          [matTooltip]="isViewMode() ? 'Bearbeiten' : 'Abbrechen'"
          [class.active]="isEditMode()">
          <mat-icon>{{ isViewMode() ? 'edit' : 'lock' }}</mat-icon>
        </button>
      }

      @if (!isViewMode()) {
        <button
          mat-flat-button
          color="primary"
          (click)="onSave()"
          [disabled]="isSaving() || form.invalid">
          @if (isSaving()) {
            <mat-spinner diameter="20"></mat-spinner>
          } @else {
            <mat-icon>save</mat-icon>
            <span>Speichern</span>
          }
        </button>
      }
    </div>
  </div>

  <!-- ============================================ -->
  <!-- LOADING STATE                                -->
  <!-- ============================================ -->
  @if (isLoading()) {
    <div class="loading-container">
      <mat-spinner diameter="48"></mat-spinner>
      <span>Lade Sensor...</span>
    </div>
  } @else {
    <!-- ============================================ -->
    <!-- FORM                                         -->
    <!-- ============================================ -->
    <form [formGroup]="form" (ngSubmit)="onSave()">
      <mat-card>
        <mat-card-content>
          <!-- ========== SECTION: Basis-Informationen ========== -->
          <div class="form-section">
            <h3>Basis-Informationen</h3>
            <div class="form-grid">
              <!-- Name -->
              <mat-form-field appearance="outline">
                <mat-label>Name</mat-label>
                <input
                  matInput
                  formControlName="name"
                  [readonly]="isReadonly()"
                  placeholder="z.B. Wohnzimmer Temperatur">
                @if (form.get('name')?.hasError('required')) {
                  <mat-error>Name ist erforderlich</mat-error>
                }
                @if (form.get('name')?.hasError('minlength')) {
                  <mat-error>Mindestens 2 Zeichen</mat-error>
                }
              </mat-form-field>

              <!-- Beschreibung -->
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Beschreibung</mat-label>
                <textarea
                  matInput
                  formControlName="description"
                  [readonly]="isReadonly()"
                  rows="3"
                  placeholder="Optionale Beschreibung"></textarea>
              </mat-form-field>
            </div>
          </div>

          <mat-divider></mat-divider>

          <!-- ========== SECTION: Zuordnung ========== -->
          <div class="form-section">
            <h3>Zuordnung</h3>
            <div class="form-grid two-columns">
              <!-- Node -->
              <mat-form-field appearance="outline">
                <mat-label>Node</mat-label>
                <mat-select formControlName="nodeId" [disabled]="isReadonly()">
                  @for (node of nodes(); track node.id) {
                    <mat-option [value]="node.id">{{ node.name }}</mat-option>
                  }
                </mat-select>
                @if (form.get('nodeId')?.hasError('required')) {
                  <mat-error>Node ist erforderlich</mat-error>
                }
              </mat-form-field>

              <!-- Sensor Type -->
              <mat-form-field appearance="outline">
                <mat-label>Sensor Typ</mat-label>
                <mat-select formControlName="sensorTypeId" [disabled]="isReadonly()">
                  @for (type of sensorTypes(); track type.id) {
                    <mat-option [value]="type.id">
                      <div class="option-with-icon">
                        @if (type.icon) {
                          <mat-icon [style.color]="type.color">{{ type.icon }}</mat-icon>
                        }
                        <span>{{ type.name }}</span>
                      </div>
                    </mat-option>
                  }
                </mat-select>
                @if (form.get('sensorTypeId')?.hasError('required')) {
                  <mat-error>Sensor Typ ist erforderlich</mat-error>
                }
              </mat-form-field>
            </div>
          </div>

          <!-- ========== SECTION: Status (nur bei Edit) ========== -->
          @if (!isCreateMode()) {
            <mat-divider></mat-divider>

            <div class="form-section">
              <h3>Status</h3>
              <div class="form-grid">
                <mat-slide-toggle
                  formControlName="isActive"
                  [disabled]="isReadonly()">
                  Sensor aktiv
                </mat-slide-toggle>
              </div>
            </div>
          }
        </mat-card-content>

        <!-- ========== CARD ACTIONS ========== -->
        @if (!isViewMode()) {
          <mat-card-actions align="end">
            <button mat-button type="button" (click)="onCancel()">
              Abbrechen
            </button>
            <button
              mat-flat-button
              color="primary"
              type="submit"
              [disabled]="isSaving() || form.invalid">
              @if (isSaving()) {
                <mat-spinner diameter="20"></mat-spinner>
              } @else {
                {{ isCreateMode() ? 'Erstellen' : 'Speichern' }}
              }
            </button>
          </mat-card-actions>
        }
      </mat-card>
    </form>

    <!-- ============================================ -->
    <!-- DELETE BUTTON (nur View/Edit, nicht Create)  -->
    <!-- ============================================ -->
    @if (!isCreateMode() && isViewMode()) {
      <div class="danger-zone">
        <h3>Gefahrenzone</h3>
        <button mat-stroked-button color="warn" (click)="onDelete()">
          <mat-icon>delete</mat-icon>
          Sensor löschen
        </button>
      </div>
    }
  }
</div>
```

### SCSS Styles

**Datei:** `libs/features/{feature}/src/lib/components/{feature}-form/{feature}-form.component.scss`

```scss
.form-container {
  max-width: 800px;
  margin: 0 auto;
  padding: 24px;
}

// Header
.form-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 16px;

    h1 {
      margin: 0;
      font-size: 24px;
      font-weight: 500;
    }
  }

  .header-actions {
    display: flex;
    align-items: center;
    gap: 8px;

    button.active {
      background-color: rgba(25, 118, 210, 0.1);
      color: #1976d2;
    }
  }
}

// Loading
.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 64px;
  gap: 16px;
  color: #666;
}

// Form Sections
.form-section {
  padding: 24px 0;

  h3 {
    margin: 0 0 16px;
    font-size: 16px;
    font-weight: 500;
    color: #333;
  }
}

// Form Grid
.form-grid {
  display: grid;
  gap: 16px;

  &.two-columns {
    grid-template-columns: 1fr 1fr;

    @media (max-width: 600px) {
      grid-template-columns: 1fr;
    }
  }

  .full-width {
    grid-column: 1 / -1;
  }
}

mat-form-field {
  width: 100%;
}

// Option with Icon
.option-with-icon {
  display: flex;
  align-items: center;
  gap: 8px;

  mat-icon {
    font-size: 20px;
    width: 20px;
    height: 20px;
  }
}

// Card Actions
mat-card-actions {
  padding: 16px 24px !important;
  margin: 0 !important;
  border-top: 1px solid #e0e0e0;
}

// Danger Zone
.danger-zone {
  margin-top: 32px;
  padding: 24px;
  border: 1px solid #ffcdd2;
  border-radius: 8px;
  background-color: #fff;

  h3 {
    margin: 0 0 16px;
    font-size: 16px;
    font-weight: 500;
    color: #c62828;
  }
}

// Divider spacing
mat-divider {
  margin: 0 -24px;
}
```

---

## 8. Routes konfigurieren

### Feature Routes

**Datei:** `libs/features/{feature}/src/lib/{feature}.routes.ts`

```typescript
import { Routes } from '@angular/router';
import { SensorListComponent } from './components/sensor-list/sensor-list.component';
import { SensorFormComponent } from './components/sensor-form/sensor-form.component';

export const SENSOR_ROUTES: Routes = [
  {
    path: '',
    component: SensorListComponent
  },
  {
    path: ':id',
    component: SensorFormComponent
  }
];
```

### Public API

**Datei:** `libs/features/{feature}/src/lib/index.ts`

```typescript
// Routes
export * from './sensor.routes';

// Components
export * from './components/sensor-list/sensor-list.component';
export * from './components/sensor-form/sensor-form.component';
```

### Integration in App Routes

**Datei:** `src/app/app.routes.ts`

```typescript
export const routes: Routes = [
  // ... andere Routes
  {
    path: 'sensors',
    loadChildren: () => import('@myiotgrid/features/sensors').then(m => m.SENSOR_ROUTES)
  },
  {
    path: 'sensor-types',
    loadChildren: () => import('@myiotgrid/features/sensor-types').then(m => m.SENSOR_TYPES_ROUTES)
  }
  // ... weitere Routes
];
```

---

## 9. GenericTable Referenz

### Inputs

| Input | Typ | Default | Beschreibung |
|-------|-----|---------|--------------|
| `data` | `unknown[]` | `[]` | Anzuzeigende Daten |
| `totalRecords` | `number` | `0` | Gesamtanzahl (für Pagination) |
| `loading` | `boolean` | `false` | Ladezustand anzeigen |
| `columns` | `GenericTableColumn[]` | `[]` | Spalten-Konfiguration |
| `dataKey` | `string` | `'id'` | Property für Row-ID |
| `stateKey` | `string` | `''` | Key für State-Persistence |
| `createLabel` | `string` | `'Neu'` | Text für Create-Button |
| `useGlobalSearch` | `boolean` | `false` | Globalen Such-Service nutzen |
| `detailMode` | `'drawer'\|'route'\|'none'` | `'drawer'` | Detail-Anzeige-Modus |
| `detailRouteBase` | `string` | `''` | Basis-Route für route-Mode |
| `showFilter` | `boolean` | `false` | Filter-Button anzeigen |
| `filterTitle` | `string` | `'Filter'` | Titel der Filter-Sidebar |
| `showToolbar` | `boolean` | `true` | Toolbar anzeigen |
| `globalFilterFields` | `string[]` | `[]` | Felder für globale Suche |

### Outputs

| Output | Typ | Beschreibung |
|--------|-----|--------------|
| `create` | `EventEmitter<void>` | Create-Button geklickt |
| `edit` | `EventEmitter<unknown>` | Row bearbeiten |
| `remove` | `EventEmitter<unknown>` | Row löschen bestätigt |
| `save` | `EventEmitter<unknown>` | Speichern (Drawer-Mode) |
| `lazyLoad` | `EventEmitter<MaterialLazyEvent>` | Pagination/Sort/Search |
| `filterChange` | `EventEmitter<Record<string,unknown>>` | Filter geändert |
| `refresh` | `EventEmitter<void>` | Refresh-Button geklickt |

### Content Projection

```html
<!-- Filter-Template (bei showFilter=true) -->
<ng-template #filterTemplate let-filters>
  <!-- Filter-Formularfelder -->
</ng-template>

<!-- Detail-Template (bei detailMode='drawer') -->
<ng-template #detailTemplate let-item let-readonly="readonly">
  <!-- Detail-/Edit-Formular -->
</ng-template>

<!-- Custom Column Template -->
<ng-template [myiotgridColumnTemplate]="'feldName'" let-row>
  <!-- Custom Rendering für diese Spalte -->
</ng-template>
```

---

## 10. Checkliste für neue Features

### Schritt 1: Models erstellen
- [ ] Feature-Model in `libs/shared/models/src/lib/{feature}.model.ts`
- [ ] Enums mit Labels definieren
- [ ] Create/Update DTOs erstellen
- [ ] Query DTO erstellen
- [ ] In `index.ts` exportieren

### Schritt 2: API Service erstellen
- [ ] Service in `libs/shared/data-access/src/lib/{feature}-api.service.ts`
- [ ] CRUD-Methoden implementieren
- [ ] `getPaged()` für Pagination
- [ ] Caching wenn sinnvoll
- [ ] In `index.ts` exportieren

### Schritt 3: List Component erstellen
- [ ] Component mit `ChangeDetectionStrategy.OnPush`
- [ ] `initialLoadDone` Flag für Loading/Empty State
- [ ] Page Header mit Titel und Subtitle (nur wenn Daten vorhanden)
- [ ] EmptyStateComponent für leere Liste
- [ ] LoadingSpinnerComponent für initiales Laden
- [ ] Spalten-Konfiguration definieren
- [ ] Field-Mapping (camelCase → PascalCase)
- [ ] `loadLazy()` implementieren
- [ ] Filter-Optionen definieren
- [ ] Custom Column Templates
- [ ] Filter Template

### Schritt 4: Form Component erstellen
- [ ] Component mit Signals für State
- [ ] Reactive Form mit Validierung
- [ ] Drei Modi: view, edit, create
- [ ] Lookup-Daten laden
- [ ] Save navigiert zur Liste (Create + Update)
- [ ] Cancel navigiert IMMER zur Liste
- [ ] Delete mit Bestätigung

### Schritt 5: Routes konfigurieren
- [ ] Feature Routes erstellen
- [ ] In `index.ts` exportieren
- [ ] In App Routes integrieren

### Schritt 6: Testen
- [ ] List lädt korrekt
- [ ] Pagination funktioniert
- [ ] Sorting funktioniert
- [ ] Filter funktionieren
- [ ] Create funktioniert
- [ ] View funktioniert
- [ ] Edit funktioniert
- [ ] Delete funktioniert
- [ ] State wird persistiert

---

## 11. UX Patterns

### 11.1 Loading / Empty / Data States (List Component)

Die List Component verwendet drei Zustände für optimale UX:

```typescript
// State-Variablen
loading = false;           // Wird während API-Calls gesetzt
initialLoadDone = false;   // Wird nach erstem erfolgreichen Load gesetzt
totalRecords = 0;          // Anzahl der Datensätze
```

```html
<!-- Template-Logik -->
@if (!initialLoadDone) {
  <!-- 1. LOADING: Initiales Laden -->
  <myiotgrid-loading-spinner message="Sensoren werden geladen...">
  </myiotgrid-loading-spinner>
} @else if (totalRecords === 0) {
  <!-- 2. EMPTY: Keine Daten vorhanden -->
  <myiotgrid-empty-state
    icon="sensors"
    title="Keine Sensoren"
    message="Es wurden noch keine Sensoren angelegt."
    actionLabel="Neuer Sensor"
    (actionClicked)="onCreate()">
  </myiotgrid-empty-state>
} @else {
  <!-- 3. DATA: Header + Tabelle -->
  <header class="page-header">...</header>
  <myiotgrid-generic-table>...</myiotgrid-generic-table>
}
```

**Wichtig:**
- Page Header wird NUR angezeigt wenn Daten vorhanden sind
- EmptyState hat eigenen Titel, daher kein doppelter Header
- `initialLoadDone` wird sowohl bei Erfolg als auch bei Fehler gesetzt

### 11.2 Navigation nach Save/Cancel (Form Component)

**Regel: Speichern und Abbrechen navigieren IMMER zur Liste zurück.**

```typescript
// Nach erfolgreichem Create
this.sensorApiService.create(createDto).subscribe({
  next: () => {
    this.snackBar.open('Sensor erstellt', 'Schließen', { duration: 3000 });
    this.router.navigate(['/sensors']);  // Zur Liste!
  }
});

// Nach erfolgreichem Update
this.sensorApiService.update(id, updateDto).subscribe({
  next: () => {
    this.snackBar.open('Sensor aktualisiert', 'Schließen', { duration: 3000 });
    this.router.navigate(['/sensors']);  // Zur Liste!
  }
});

// Abbrechen - IMMER zur Liste
onCancel(): void {
  this.router.navigate(['/sensors']);
}
```

**Begründung:**
- Benutzer erwartet nach Aktion die aktualisierte Liste zu sehen
- Vermeidet "View Mode" Verwirrung nach Edit
- Konsistentes Verhalten über alle Features

### 11.3 Page Header Styling

```scss
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 24px;
  flex-wrap: wrap;
  gap: 16px;
  padding-left: 25px;  // Einrückung für Header-Text

  .header-content {
    .page-title {
      margin: 0;
      font-size: 28px;
      font-weight: 500;
      color: #212121;
    }

    .page-subtitle {
      margin: 8px 0 0 0;
      font-size: 14px;
      color: #757575;
    }
  }
}
```

---

## 12. Best Practices

### Performance
- `ChangeDetectionStrategy.OnPush` verwenden
- `markForCheck()` nach async Operationen
- Signals statt BehaviorSubject für lokalen State
- Server-side Pagination für große Datenmengen

### UX
- Minimum 3 Zeichen für Suche
- Loading-States anzeigen (LoadingSpinnerComponent)
- Empty-States anzeigen (EmptyStateComponent)
- Page Header nur bei vorhandenen Daten
- Fehler mit SnackBar melden
- State zwischen Navigation persistieren
- Save/Cancel navigiert zur Liste

### Code-Qualität
- Strikte Typisierung (keine `any`)
- DTOs exakt ans Backend anpassen
- Field-Mapping für Sort-Parameter
- Validierung im Form-Component

### Styling
- Konsistente Farben für Status
- Responsive Grids
- Material Design Patterns
- Outlook-inspirierte Toolbar

---

**Erstellt:** 01.12.2025
**Aktualisiert:** 01.12.2025
**Basierend auf:** Sensor-Types & Sensors Feature Implementation

### Changelog

**v1.1 (01.12.2025)**
- NEU: Loading/Empty/Data State Pattern mit `initialLoadDone` Flag
- NEU: EmptyStateComponent und LoadingSpinnerComponent Integration
- NEU: Page Header nur bei vorhandenen Daten anzeigen
- NEU: Save/Cancel navigiert IMMER zur Liste zurück
- NEU: Abschnitt 11 "UX Patterns" hinzugefügt
- Aktualisiert: Checkliste mit neuen Anforderungen
- Aktualisiert: Best Practices erweitert
