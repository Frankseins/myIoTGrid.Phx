import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { Alert, AlertFilter, PaginatedResult } from '@myiotgrid/shared/models';

@Injectable({ providedIn: 'root' })
export class AlertApiService extends BaseApiService {
  private readonly endpoint = '/alerts';

  /**
   * Get all active alerts
   * GET /api/alerts
   */
  getActive(): Observable<Alert[]> {
    return this.get<Alert[]>(this.endpoint);
  }

  /**
   * Get filtered/paginated alerts
   * GET /api/alerts/filtered
   */
  getFiltered(filter: AlertFilter): Observable<PaginatedResult<Alert>> {
    return this.get<PaginatedResult<Alert>>(`${this.endpoint}/filtered`, filter as Record<string, unknown>);
  }

  /**
   * Get alert by ID
   * GET /api/alerts/{id}
   */
  getById(id: string): Observable<Alert> {
    return this.get<Alert>(`${this.endpoint}/${id}`);
  }

  /**
   * Acknowledge/confirm alert
   * POST /api/alerts/{id}/acknowledge
   */
  acknowledge(id: string): Observable<Alert> {
    return this.post<Alert>(`${this.endpoint}/${id}/acknowledge`, {});
  }
}
