import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { Sensor, CreateSensorDto, UpdateSensorDto } from '@myiotgrid/shared/models';

@Injectable({ providedIn: 'root' })
export class SensorApiService extends BaseApiService {
  private readonly endpoint = '/sensors';

  getAll(): Observable<Sensor[]> {
    return this.get<Sensor[]>(this.endpoint);
  }

  getById(id: string): Observable<Sensor> {
    return this.get<Sensor>(`${this.endpoint}/${id}`);
  }

  getByHubId(hubId: string): Observable<Sensor[]> {
    return this.get<Sensor[]>(this.endpoint, { hubId });
  }

  create(dto: CreateSensorDto): Observable<Sensor> {
    return this.post<Sensor>(this.endpoint, dto);
  }

  update(id: string, dto: UpdateSensorDto): Observable<Sensor> {
    return this.put<Sensor>(`${this.endpoint}/${id}`, dto);
  }

  remove(id: string): Observable<void> {
    return this.delete<void>(`${this.endpoint}/${id}`);
  }
}
