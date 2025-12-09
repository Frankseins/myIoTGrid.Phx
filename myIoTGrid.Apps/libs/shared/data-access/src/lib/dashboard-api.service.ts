import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { LocationDashboard, SparklinePeriod, DashboardFilterOptions, DashboardFilter } from '@myiotgrid/shared/models';

@Injectable({ providedIn: 'root' })
export class DashboardApiService extends BaseApiService {
  private readonly endpoint = '/dashboard';

  /**
   * Get dashboard data grouped by location with sparkline data
   * GET /api/dashboard/locations?period={period}
   */
  getLocationDashboard(period: SparklinePeriod = SparklinePeriod.Day): Observable<LocationDashboard> {
    return this.get<LocationDashboard>(`${this.endpoint}/locations`, { period: period.toString() });
  }

  /**
   * Get filtered dashboard data with widgets
   * GET /api/dashboard/widgets?locations=...&measurementTypes=...&period=...
   */
  getFilteredDashboard(filter: DashboardFilter): Observable<LocationDashboard> {
    const params: Record<string, string | string[]> = {};

    if (filter.locations && filter.locations.length > 0) {
      params['locations'] = filter.locations;
    }
    if (filter.measurementTypes && filter.measurementTypes.length > 0) {
      params['measurementTypes'] = filter.measurementTypes;
    }
    if (filter.period !== undefined) {
      params['period'] = filter.period.toString();
    }

    return this.get<LocationDashboard>(`${this.endpoint}/widgets`, params);
  }

  /**
   * Get available filter options (locations and measurement types)
   * GET /api/dashboard/filter-options
   */
  getFilterOptions(): Observable<DashboardFilterOptions> {
    return this.get<DashboardFilterOptions>(`${this.endpoint}/filter-options`);
  }
}
