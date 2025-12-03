import { Injectable, signal, computed } from '@angular/core';
import { Observable, tap, shareReplay } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { SensorType, CreateSensorTypeDto, UpdateSensorTypeDto, SensorTypeCapability, QueryParams, PagedResult } from '@myiotgrid/shared/models';
import { queryParamsToObject } from './api-query.helper';

/**
 * API Service for SensorTypes with caching
 * Hardware library - defines what sensors CAN do
 */
@Injectable({ providedIn: 'root' })
export class SensorTypeApiService extends BaseApiService {
  private readonly endpoint = '/sensortypes';

  // Cache for SensorTypes (rarely changed)
  private cache$?: Observable<SensorType[]>;

  // Signals for reactive access
  readonly sensorTypes = signal<SensorType[]>([]);
  readonly sensorTypesMap = signal<Map<string, SensorType>>(new Map());
  readonly isLoaded = signal(false);

  // Computed values
  readonly categories = computed(() => {
    const types = this.sensorTypes();
    const uniqueCategories = new Set(types.map(t => t.category));
    return Array.from(uniqueCategories);
  });

  /**
   * Get all sensor types (with caching)
   * GET /api/sensortypes
   */
  getAll(): Observable<SensorType[]> {
    if (!this.cache$) {
      this.cache$ = this.get<SensorType[]>(this.endpoint).pipe(
        tap(types => {
          this.sensorTypes.set(types);
          this.sensorTypesMap.set(new Map(types.map(t => [t.id, t])));
          this.isLoaded.set(true);
        }),
        shareReplay(1)
      );
    }
    return this.cache$;
  }

  /**
   * Get sensor types with server-side paging, sorting, and filtering
   * GET /api/sensortypes/paged
   */
  getPaged(params: QueryParams): Observable<PagedResult<SensorType>> {
    return this.get<PagedResult<SensorType>>(`${this.endpoint}/paged`, queryParamsToObject(params));
  }

  /**
   * Get sensor type by ID (Guid)
   * GET /api/sensortypes/{id}
   */
  getById(id: string): Observable<SensorType> {
    return this.get<SensorType>(`${this.endpoint}/${id}`);
  }

  /**
   * Get sensor type by code
   * GET /api/sensortypes/code/{code}
   */
  getByCode(code: string): Observable<SensorType> {
    return this.get<SensorType>(`${this.endpoint}/code/${code}`);
  }

  /**
   * Get sensor types by category
   * GET /api/sensortypes/category/{category}
   */
  getByCategory(category: string): Observable<SensorType[]> {
    return this.get<SensorType[]>(`${this.endpoint}/category/${category}`);
  }

  /**
   * Get capabilities for a sensor type
   * GET /api/sensortypes/{id}/capabilities
   */
  getCapabilities(id: string): Observable<SensorTypeCapability[]> {
    return this.get<SensorTypeCapability[]>(`${this.endpoint}/${id}/capabilities`);
  }

  /**
   * Create new sensor type
   * POST /api/sensortypes
   */
  create(dto: CreateSensorTypeDto): Observable<SensorType> {
    return this.post<SensorType>(this.endpoint, dto).pipe(
      tap(() => this.clearCache())
    );
  }

  /**
   * Update sensor type
   * PUT /api/sensortypes/{id}
   */
  update(id: string, dto: UpdateSensorTypeDto): Observable<SensorType> {
    return this.put<SensorType>(`${this.endpoint}/${id}`, dto).pipe(
      tap(() => this.clearCache())
    );
  }

  /**
   * Delete sensor type
   * DELETE /api/sensortypes/{id}
   */
  deleteSensorType(id: string): Observable<void> {
    return this.delete<void>(`${this.endpoint}/${id}`).pipe(
      tap(() => this.clearCache())
    );
  }

  /**
   * Trigger sync from cloud
   * POST /api/sensortypes/sync
   */
  syncFromCloud(): Observable<void> {
    return this.post<void>(`${this.endpoint}/sync`, {}).pipe(
      tap(() => this.clearCache())
    );
  }

  // ==========================================
  // Synchronous helper methods (after initial load)
  // ==========================================

  /**
   * Get display name for a sensor type (synchronous)
   */
  getDisplayName(id: string): string {
    return this.sensorTypesMap().get(id)?.name ?? id;
  }

  /**
   * Get code for a sensor type (synchronous)
   */
  getCode(id: string): string {
    return this.sensorTypesMap().get(id)?.code ?? id;
  }

  /**
   * Get icon for a sensor type (synchronous)
   */
  getIcon(id: string): string {
    return this.sensorTypesMap().get(id)?.icon ?? 'sensors';
  }

  /**
   * Get color for a sensor type (synchronous)
   */
  getColor(id: string): string {
    return this.sensorTypesMap().get(id)?.color ?? '#666666';
  }

  /**
   * Get default unit for a sensor type (synchronous)
   * Uses the first capability's unit or empty string
   */
  getUnit(id: string): string {
    const type = this.sensorTypesMap().get(id);
    if (type?.capabilities?.length) {
      return type.capabilities[0].unit ?? '';
    }
    return '';
  }

  /**
   * Get full sensor type by ID (synchronous)
   */
  getType(id: string): SensorType | undefined {
    return this.sensorTypesMap().get(id);
  }

  /**
   * Get types by category (synchronous)
   */
  getTypesByCategory(category: string): SensorType[] {
    return this.sensorTypes().filter(t => t.category === category);
  }

  /**
   * Clear cache (e.g., after admin changes)
   */
  clearCache(): void {
    this.cache$ = undefined;
    this.isLoaded.set(false);
  }
}
