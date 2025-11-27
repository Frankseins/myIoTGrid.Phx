import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { SensorData, CreateSensorDataDto, SensorDataFilter, PaginatedResult } from '@myiotgrid/shared/models';

@Injectable({ providedIn: 'root' })
export class SensorDataApiService extends BaseApiService {
  private readonly endpoint = '/sensordata';

  getFiltered(filter: SensorDataFilter): Observable<PaginatedResult<SensorData>> {
    return this.get<PaginatedResult<SensorData>>(this.endpoint, filter as Record<string, unknown>);
  }

  getById(id: string): Observable<SensorData> {
    return this.get<SensorData>(`${this.endpoint}/${id}`);
  }

  getLatest(): Observable<SensorData[]> {
    return this.get<SensorData[]>(`${this.endpoint}/latest`);
  }

  getLatestByHub(hubId: string): Observable<SensorData[]> {
    return this.get<SensorData[]>(`${this.endpoint}/latest`, { hubId });
  }

  getLatestBySensor(sensorId: string): Observable<SensorData[]> {
    return this.get<SensorData[]>(`${this.endpoint}/latest`, { sensorId });
  }

  create(dto: CreateSensorDataDto): Observable<SensorData> {
    return this.post<SensorData>(this.endpoint, dto);
  }
}
