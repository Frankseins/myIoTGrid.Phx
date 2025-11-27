import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { Alert, AlertFilter } from '@myiotgrid/shared/models';

@Injectable({ providedIn: 'root' })
export class AlertApiService extends BaseApiService {
  private readonly endpoint = '/alerts';

  getActive(): Observable<Alert[]> {
    return this.get<Alert[]>(this.endpoint, { isActive: true });
  }

  getFiltered(filter: AlertFilter): Observable<Alert[]> {
    return this.get<Alert[]>(this.endpoint, filter as Record<string, unknown>);
  }

  getById(id: string): Observable<Alert> {
    return this.get<Alert>(`${this.endpoint}/${id}`);
  }

  acknowledge(id: string): Observable<Alert> {
    return this.post<Alert>(`${this.endpoint}/${id}/acknowledge`, {});
  }
}
