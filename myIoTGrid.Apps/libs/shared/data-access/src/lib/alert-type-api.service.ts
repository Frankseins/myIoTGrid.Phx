import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { AlertType, CreateAlertTypeDto } from '@myiotgrid/shared/models';

@Injectable({ providedIn: 'root' })
export class AlertTypeApiService extends BaseApiService {
  private readonly endpoint = '/alerttypes';

  getAll(): Observable<AlertType[]> {
    return this.get<AlertType[]>(this.endpoint);
  }

  getById(id: string): Observable<AlertType> {
    return this.get<AlertType>(`${this.endpoint}/${id}`);
  }

  getByCode(code: string): Observable<AlertType> {
    return this.get<AlertType>(`${this.endpoint}/by-code/${code}`);
  }

  create(dto: CreateAlertTypeDto): Observable<AlertType> {
    return this.post<AlertType>(this.endpoint, dto);
  }
}
