import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { SensorType, CreateSensorTypeDto } from '@myiotgrid/shared/models';

@Injectable({ providedIn: 'root' })
export class SensorTypeApiService extends BaseApiService {
  private readonly endpoint = '/sensortypes';

  getAll(): Observable<SensorType[]> {
    return this.get<SensorType[]>(this.endpoint);
  }

  getById(id: string): Observable<SensorType> {
    return this.get<SensorType>(`${this.endpoint}/${id}`);
  }

  getByCode(code: string): Observable<SensorType> {
    return this.get<SensorType>(`${this.endpoint}/by-code/${code}`);
  }

  create(dto: CreateSensorTypeDto): Observable<SensorType> {
    return this.post<SensorType>(this.endpoint, dto);
  }
}
