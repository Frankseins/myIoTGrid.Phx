import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { ChartData, ChartInterval, ReadingsList, ReadingsListRequest } from '@myiotgrid/shared/models';

@Injectable({ providedIn: 'root' })
export class ChartApiService extends BaseApiService {
  private readonly endpoint = '/readings';

  /**
   * Get chart data for a specific widget
   * GET /api/readings/chart/{nodeId}/{assignmentId}/{measurementType}
   */
  getChartData(
    nodeId: string,
    assignmentId: string,
    measurementType: string,
    interval: ChartInterval = ChartInterval.OneDay
  ): Observable<ChartData> {
    return this.get<ChartData>(
      `${this.endpoint}/chart/${nodeId}/${assignmentId}/${measurementType}`,
      { interval }
    );
  }

  /**
   * Get paginated readings list for a widget
   * GET /api/readings/list/{nodeId}/{assignmentId}/{measurementType}
   */
  getReadingsList(
    nodeId: string,
    assignmentId: string,
    measurementType: string,
    request: ReadingsListRequest = {}
  ): Observable<ReadingsList> {
    return this.get<ReadingsList>(
      `${this.endpoint}/list/${nodeId}/${assignmentId}/${measurementType}`,
      request as Record<string, unknown>
    );
  }

  /**
   * Export readings to CSV
   * GET /api/readings/list/{nodeId}/{assignmentId}/{measurementType}/csv
   */
  exportToCsv(
    nodeId: string,
    assignmentId: string,
    measurementType: string,
    from?: string,
    to?: string
  ): Observable<Blob> {
    const params: Record<string, unknown> = {};
    if (from) params['from'] = from;
    if (to) params['to'] = to;

    return this.http.get(
      `${this.baseUrl}${this.endpoint}/list/${nodeId}/${assignmentId}/${measurementType}/csv`,
      {
        params: this.buildParams(params),
        responseType: 'blob'
      }
    );
  }
}
