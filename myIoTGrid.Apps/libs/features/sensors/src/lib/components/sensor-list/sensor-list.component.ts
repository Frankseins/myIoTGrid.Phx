// ================================
// Sensor List Component
// Verwaltung aller Sensoren mit CRUD
// Verwendet Route-basiertes Detail-Formular
// ================================

import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

// Angular Material
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { FormsModule } from '@angular/forms';

// Services & DTOs
import { SensorApiService } from '@myiotgrid/shared/data-access';
import { Sensor } from '@myiotgrid/shared/models';

// GenericTable
import {
  GenericTableComponent,
  GenericTableColumn,
  MaterialLazyEvent,
  ColumnTemplateDirective,
  EmptyStateComponent,
  LoadingSpinnerComponent
} from '@myiotgrid/shared/ui';

interface SensorQueryDto {
  page: number;
  size: number;
  sort: string;
  search?: string;
  category?: string;
  isActive?: string;
}

@Component({
  selector: 'myiotgrid-sensor-list',
  standalone: true,
  templateUrl: './sensor-list.component.html',
  styleUrls: ['./sensor-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatSnackBarModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatChipsModule,
    GenericTableComponent,
    ColumnTemplateDirective,
    EmptyStateComponent,
    LoadingSpinnerComponent,
  ],
})
export class SensorListComponent implements OnInit {
  sensors: Sensor[] = [];
  totalRecords = 0;
  loading = false;
  initialLoadDone = false;
  globalFilter = '';

  // Available categories for filter dropdown
  sensorCategories: string[] = ['climate', 'water', 'location', 'custom'];

  columns: GenericTableColumn[] = [
    { field: 'icon', header: '', width: '60px', sortable: false },
    { field: 'name', header: 'Name', sortable: true },
    { field: 'code', header: 'Code', sortable: true },
    { field: 'category', header: 'Kategorie', sortable: true },
    { field: 'protocol', header: 'Protokoll', sortable: true },
    { field: 'serialNumber', header: 'Seriennummer', width: '150px', sortable: true },
    { field: 'isActive', header: 'Status', width: '100px', sortable: true },
    {
      field: 'createdAt',
      header: 'Erstellt',
      width: '160px',
      sortable: true,
      type: 'date',
    },
  ];

  // Status-Optionen für Filter
  statusOptions = [
    { value: '', label: 'Alle' },
    { value: 'true', label: 'Aktiv' },
    { value: 'false', label: 'Inaktiv' },
  ];

  // Aktuelle Filter-Werte
  currentFilters: Record<string, unknown> = {};

  private lastQueryParams: SensorQueryDto | null = null;
  private lastSearchTerm = '';

  // Mapping von Frontend-Feldnamen (camelCase) zu Backend-Feldnamen (PascalCase)
  private readonly fieldMapping: Record<string, string> = {
    id: 'Id',
    name: 'Name',
    code: 'Code',
    category: 'Category',
    protocol: 'Protocol',
    serialNumber: 'SerialNumber',
    isActive: 'IsActive',
    createdAt: 'CreatedAt',
    updatedAt: 'UpdatedAt',
  };

  constructor(
    private router: Router,
    private sensorApiService: SensorApiService,
    private snackBar: MatSnackBar,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadSensorsLazy({
      first: 0,
      rows: 10,
      sortField: 'name',
      sortOrder: 1,
      globalFilter: this.globalFilter,
    });
  }

  /**
   * Lazy Loading von Sensors (wird von GenericTable aufgerufen)
   */
  loadSensorsLazy(event: MaterialLazyEvent): void {
    const currentSearchTerm = event.globalFilter || '';

    // Wenn Suchbegriff 1-2 Zeichen hat, ignoriere das Event
    if (currentSearchTerm.length > 0 && currentSearchTerm.length < 3) {
      this.lastSearchTerm = currentSearchTerm;
      return;
    }

    this.lastSearchTerm = currentSearchTerm;
    this.loading = true;

    // Seite berechnen (Backend erwartet 0-indexed page)
    const page = Math.floor((event.first ?? 0) / (event.rows ?? 10));
    const size = event.rows ?? 10;

    // Sort-String bauen mit Backend-Feldnamen (PascalCase)
    let sort = 'Name,asc';
    if (event.sortField) {
      const direction = event.sortOrder === 1 ? 'asc' : 'desc';
      const backendField =
        this.fieldMapping[event.sortField] || event.sortField;
      sort = `${backendField},${direction}`;
    }

    const search =
      currentSearchTerm.length >= 3 ? currentSearchTerm : undefined;

    // Filter aus Event extrahieren
    const eventFilters = event.filters || this.currentFilters || {};

    // Filters für QueryParams vorbereiten (lowercase Keys für Backend)
    const filters: Record<string, string> = {};
    if (eventFilters['category']) {
      filters['category'] = String(eventFilters['category']);
    }
    if (eventFilters['isActive'] !== undefined && eventFilters['isActive'] !== '') {
      filters['isActive'] = String(eventFilters['isActive']);
    }

    const query: SensorQueryDto = {
      page,
      size,
      sort,
      search,
    };

    this.lastQueryParams = query;

    this.sensorApiService
      .getPaged({
        page: query.page,
        size: query.size,
        sort: query.sort,
        search: query.search,
        filters: Object.keys(filters).length > 0 ? filters : undefined,
      })
      .subscribe({
        next: (data) => {
          this.sensors = data.items;
          this.totalRecords = data.totalRecords;
          this.loading = false;
          this.initialLoadDone = true;
          this.cd.markForCheck();
        },
        error: (error) => {
          console.error('Error loading sensors:', error);
          this.snackBar.open(
            error.message || 'Fehler beim Laden der Sensoren',
            'Schließen',
            { duration: 5000, panelClass: ['snackbar-error'] }
          );
          this.loading = false;
          this.initialLoadDone = true;
          this.cd.markForCheck();
        },
      });
  }

  /**
   * Navigiert zum Neu-Formular
   */
  onCreate(): void {
    this.router.navigate(['/sensors', 'new']);
  }

  /**
   * Navigiert zum Detail-Formular (wird vom GenericTable über Route gemacht)
   */
  onEdit(_sensor: Sensor): void {
    // Navigation wird vom GenericTable mit detailMode='route' gehandhabt
  }

  /**
   * Löscht Sensor
   */
  deleteSensor(sensor: Sensor): void {
    this.sensorApiService.remove(sensor.id).subscribe({
      next: () => {
        this.snackBar.open('Sensor erfolgreich gelöscht', 'Schließen', {
          duration: 3000,
          panelClass: ['snackbar-success'],
        });
        this.reloadTable();
      },
      error: (error) => {
        this.snackBar.open(
          error.message || 'Fehler beim Löschen',
          'Schließen',
          { duration: 5000, panelClass: ['snackbar-error'] }
        );
      },
    });
  }

  /**
   * Lädt Tabelle mit den zuletzt verwendeten Parametern neu
   */
  reloadTable(): void {
    if (!this.lastQueryParams) {
      this.loadSensorsLazy({
        first: 0,
        rows: 10,
        sortField: 'name',
        sortOrder: 1,
        globalFilter: this.globalFilter,
      });
      return;
    }

    const {
      page = 0,
      size = 10,
      sort = 'Name,asc',
      search = '',
    } = this.lastQueryParams;
    const [backendSortField, direction] = sort.split(',');

    // Reverse-Mapping: Backend PascalCase → Frontend camelCase
    const frontendSortField =
      Object.entries(this.fieldMapping).find(
        ([_, backend]) => backend === backendSortField
      )?.[0] || 'name';

    this.loadSensorsLazy({
      first: page * size,
      rows: size,
      sortField: frontendSortField,
      sortOrder: direction === 'desc' ? -1 : 1,
      globalFilter: search || this.globalFilter,
    });
  }

  /**
   * Hilfsmethode: Sensor Icon
   */
  getSensorIcon(sensor: Sensor): string {
    return sensor.icon || 'sensors';
  }

  /**
   * Hilfsmethode: Sensor Color
   */
  getSensorColor(sensor: Sensor): string {
    return sensor.color || '#607d8b';
  }

  /**
   * Hilfsmethode: Category Label
   */
  getCategoryLabel(category: string): string {
    const labels: Record<string, string> = {
      climate: 'Klima',
      water: 'Wasser',
      location: 'Standort',
      custom: 'Benutzerdefiniert'
    };
    return labels[category] || category;
  }

  /**
   * Hilfsmethode: Protocol Label
   */
  getProtocolLabel(protocol: number): string {
    const labels: Record<number, string> = {
      1: 'I²C',
      2: 'SPI',
      3: '1-Wire',
      4: 'Analog',
      5: 'UART',
      6: 'Digital',
      7: 'Ultraschall'
    };
    return labels[protocol] || 'Unbekannt';
  }

  /**
   * Filter wurden geändert
   */
  onFilterChange(filters: Record<string, unknown>): void {
    this.currentFilters = filters;
    // Tabelle mit Filtern neu laden
    this.loadSensorsLazy({
      first: 0,
      rows: 10,
      sortField: 'name',
      sortOrder: 1,
      globalFilter: this.globalFilter,
      filters: filters
    });
  }
}
