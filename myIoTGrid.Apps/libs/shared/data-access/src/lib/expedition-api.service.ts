import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import {
  Expedition,
  CreateExpeditionDto,
  UpdateExpeditionDto,
  ExpeditionStats,
  ExpeditionFilter,
  ExpeditionStatus,
  ExpeditionGpsData
} from '@myiotgrid/shared/models';

@Injectable({ providedIn: 'root' })
export class ExpeditionApiService extends BaseApiService {
  private readonly endpoint = '/expeditions';

  /**
   * Get all expeditions with optional filters
   * GET /api/expeditions
   */
  getAll(filter?: ExpeditionFilter): Observable<Expedition[]> {
    const params: Record<string, unknown> = {};

    if (filter?.status) {
      params['status'] = filter.status;
    }
    if (filter?.nodeId) {
      params['nodeId'] = filter.nodeId;
    }
    if (filter?.tags) {
      params['tags'] = filter.tags;
    }
    if (filter?.fromDate) {
      params['fromDate'] = filter.fromDate;
    }
    if (filter?.toDate) {
      params['toDate'] = filter.toDate;
    }

    return this.get<Expedition[]>(this.endpoint, params);
  }

  /**
   * Get expedition by ID
   * GET /api/expeditions/{id}
   */
  getById(id: string): Observable<Expedition> {
    return this.get<Expedition>(`${this.endpoint}/${id}`);
  }

  /**
   * Get expeditions for a specific node
   * GET /api/expeditions/node/{nodeId}
   */
  getByNodeId(nodeId: string): Observable<Expedition[]> {
    return this.get<Expedition[]>(`${this.endpoint}/node/${nodeId}`);
  }

  /**
   * Create new expedition
   * POST /api/expeditions
   */
  create(dto: CreateExpeditionDto): Observable<Expedition> {
    return this.post<Expedition>(this.endpoint, dto);
  }

  /**
   * Update expedition
   * PUT /api/expeditions/{id}
   */
  update(id: string, dto: UpdateExpeditionDto): Observable<Expedition> {
    return this.put<Expedition>(`${this.endpoint}/${id}`, dto);
  }

  /**
   * Delete expedition
   * DELETE /api/expeditions/{id}
   */
  remove(id: string): Observable<void> {
    return this.delete<void>(`${this.endpoint}/${id}`);
  }

  /**
   * Get detailed statistics for an expedition
   * GET /api/expeditions/{id}/stats
   */
  getStatistics(id: string): Observable<ExpeditionStats> {
    return this.get<ExpeditionStats>(`${this.endpoint}/${id}/stats`);
  }

  /**
   * Recalculate statistics for an expedition
   * POST /api/expeditions/{id}/recalculate
   */
  recalculateStatistics(id: string): Observable<Expedition> {
    return this.post<Expedition>(`${this.endpoint}/${id}/recalculate`, {});
  }

  /**
   * Update expedition status
   * PATCH /api/expeditions/{id}/status
   */
  updateStatus(id: string, status: ExpeditionStatus): Observable<Expedition> {
    return this.http.patch<Expedition>(
      `${this.baseUrl}${this.endpoint}/${id}/status`,
      JSON.stringify(status),
      { headers: { 'Content-Type': 'application/json' } }
    );
  }

  /**
   * Get GPS data (points with measurements) for an expedition
   * GET /api/expeditions/{id}/gps
   */
  getGpsData(id: string): Observable<ExpeditionGpsData> {
    return this.get<ExpeditionGpsData>(`${this.endpoint}/${id}/gps`);
  }
}
