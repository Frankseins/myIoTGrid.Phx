import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { Reading, CreateReadingDto, ReadingFilter, PaginatedResult, QueryParams, PagedResult, DeleteReadingsRangeDto, DeleteReadingsResultDto } from '@myiotgrid/shared/models';
import { queryParamsToObject } from './api-query.helper';

@Injectable({ providedIn: 'root' })
export class ReadingApiService extends BaseApiService {
  private readonly endpoint = '/readings';

  /**
   * Get filtered/paginated readings (legacy)
   * GET /api/readings
   */
  getFiltered(filter: ReadingFilter): Observable<PaginatedResult<Reading>> {
    return this.get<PaginatedResult<Reading>>(this.endpoint, filter as Record<string, unknown>);
  }

  /**
   * Get readings with server-side paging, sorting, and filtering
   * GET /api/readings/paged
   */
  getPaged(params: QueryParams): Observable<PagedResult<Reading>> {
    return this.get<PagedResult<Reading>>(`${this.endpoint}/paged`, queryParamsToObject(params));
  }

  /**
   * Get reading by ID
   * GET /api/readings/{id}
   */
  getById(id: number): Observable<Reading> {
    return this.get<Reading>(`${this.endpoint}/${id}`);
  }

  /**
   * Get latest readings for all nodes
   * GET /api/readings/latest
   */
  getLatest(): Observable<Reading[]> {
    return this.get<Reading[]>(`${this.endpoint}/latest`);
  }

  /**
   * Get latest readings for specific node
   * GET /api/readings/latest?nodeId={nodeId}
   */
  getLatestByNode(nodeId: string): Observable<Reading[]> {
    return this.get<Reading[]>(`${this.endpoint}/latest`, { nodeId });
  }

  /**
   * Get latest readings for specific hub
   * GET /api/readings/latest?hubId={hubId}
   */
  getLatestByHub(hubId: string): Observable<Reading[]> {
    return this.get<Reading[]>(`${this.endpoint}/latest`, { hubId });
  }

  /**
   * Get readings by node within time range
   * GET /api/readings?nodeId={nodeId}&from={from}&to={to}
   */
  getByNode(nodeId: string, hours: number = 24): Observable<PaginatedResult<Reading>> {
    const from = new Date(Date.now() - hours * 60 * 60 * 1000).toISOString();
    return this.getFiltered({ nodeId, from });
  }

  /**
   * Create new reading
   * POST /api/readings
   */
  create(dto: CreateReadingDto): Observable<Reading> {
    return this.post<Reading>(this.endpoint, dto);
  }

  /**
   * Delete readings within a date range
   * DELETE /api/readings/range
   */
  deleteRange(dto: DeleteReadingsRangeDto): Observable<DeleteReadingsResultDto> {
    return this.deleteWithBody<DeleteReadingsResultDto>(`${this.endpoint}/range`, dto);
  }
}
