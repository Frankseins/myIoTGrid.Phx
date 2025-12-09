import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { Sensor, CreateSensorDto, UpdateSensorDto, CalibrateSensorDto, QueryParams, PagedResult } from '@myiotgrid/shared/models';
import { queryParamsToObject } from './api-query.helper';

/**
 * API Service for Sensor instances (concrete sensors with calibration)
 */
@Injectable({ providedIn: 'root' })
export class SensorApiService extends BaseApiService {
  private readonly endpoint = '/sensors';

  /**
   * Get all sensors
   * GET /api/sensors
   */
  getAll(): Observable<Sensor[]> {
    return this.get<Sensor[]>(this.endpoint);
  }

  /**
   * Get sensors with server-side paging, sorting, and filtering
   * GET /api/sensors/paged
   */
  getPaged(params: QueryParams): Observable<PagedResult<Sensor>> {
    return this.get<PagedResult<Sensor>>(`${this.endpoint}/paged`, queryParamsToObject(params));
  }

  /**
   * Get sensor by ID
   * GET /api/sensors/{id}
   */
  getById(id: string): Observable<Sensor> {
    return this.get<Sensor>(`${this.endpoint}/${id}`);
  }

  /**
   * Get sensors by SensorType
   * GET /api/sensors/by-type/{sensorTypeId}
   */
  getBySensorType(sensorTypeId: string): Observable<Sensor[]> {
    return this.get<Sensor[]>(`${this.endpoint}/by-type/${sensorTypeId}`);
  }

  /**
   * Create new sensor
   * POST /api/sensors
   */
  create(dto: CreateSensorDto): Observable<Sensor> {
    return this.post<Sensor>(this.endpoint, dto);
  }

  /**
   * Update sensor
   * PUT /api/sensors/{id}
   */
  update(id: string, dto: UpdateSensorDto): Observable<Sensor> {
    return this.put<Sensor>(`${this.endpoint}/${id}`, dto);
  }

  /**
   * Calibrate sensor
   * POST /api/sensors/{id}/calibrate
   */
  calibrate(id: string, dto: CalibrateSensorDto): Observable<Sensor> {
    return this.post<Sensor>(`${this.endpoint}/${id}/calibrate`, dto);
  }

  /**
   * Delete sensor
   * DELETE /api/sensors/{id}
   */
  remove(id: string): Observable<void> {
    return this.delete<void>(`${this.endpoint}/${id}`);
  }

  /**
   * Activate/deactivate sensor
   * PUT /api/sensors/{id}
   */
  setActive(id: string, isActive: boolean): Observable<Sensor> {
    return this.put<Sensor>(`${this.endpoint}/${id}`, { isActive });
  }
}
