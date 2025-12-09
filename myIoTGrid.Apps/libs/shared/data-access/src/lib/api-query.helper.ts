import { HttpParams } from '@angular/common/http';
import { QueryParams } from '@myiotgrid/shared/models';

/**
 * Converts QueryParams to HttpParams for API requests.
 */
export function queryParamsToHttpParams(params: QueryParams): HttpParams {
  let httpParams = new HttpParams();

  // Page and Size (required)
  httpParams = httpParams.set('page', params.page.toString());
  httpParams = httpParams.set('size', params.size.toString());

  // Sort (optional)
  if (params.sort) {
    httpParams = httpParams.set('sort', params.sort);
  }

  // Search (optional)
  if (params.search) {
    httpParams = httpParams.set('search', params.search);
  }

  // Date range (optional)
  if (params.dateFrom) {
    httpParams = httpParams.set('dateFrom', params.dateFrom.toISOString());
  }

  if (params.dateTo) {
    httpParams = httpParams.set('dateTo', params.dateTo.toISOString());
  }

  // Filters (optional) - serialize as filters[key]=value
  if (params.filters) {
    for (const [key, value] of Object.entries(params.filters)) {
      if (value !== null && value !== undefined && value !== '') {
        httpParams = httpParams.set(`filters[${key}]`, value);
      }
    }
  }

  return httpParams;
}

/**
 * Converts QueryParams to a plain object for API requests
 */
export function queryParamsToObject(params: QueryParams): Record<string, string | number> {
  const result: Record<string, string | number> = {
    page: params.page,
    size: params.size
  };

  if (params.sort) {
    result['sort'] = params.sort;
  }

  if (params.search) {
    result['search'] = params.search;
  }

  if (params.dateFrom) {
    result['dateFrom'] = params.dateFrom.toISOString();
  }

  if (params.dateTo) {
    result['dateTo'] = params.dateTo.toISOString();
  }

  if (params.filters) {
    for (const [key, value] of Object.entries(params.filters)) {
      if (value !== null && value !== undefined && value !== '') {
        result[`filters[${key}]`] = value;
      }
    }
  }

  return result;
}

/**
 * Parses sort string into field and direction
 */
export function parseSortString(sort: string): { field: string; direction: 'asc' | 'desc' } {
  if (!sort) {
    return { field: '', direction: 'asc' };
  }

  const parts = sort.split(',');
  const field = parts[0] || '';
  const direction = (parts[1]?.toLowerCase() === 'desc' ? 'desc' : 'asc') as 'asc' | 'desc';

  return { field, direction };
}

/**
 * Creates a sort string from field and direction
 */
export function createSortString(field: string, direction: 'asc' | 'desc' = 'asc'): string {
  if (!field) {
    return '';
  }
  return `${field},${direction}`;
}

/**
 * Toggles sort direction for a field
 */
export function toggleSort(currentSort: string | undefined, field: string): string {
  if (!currentSort) {
    return createSortString(field, 'asc');
  }

  const { field: currentField, direction } = parseSortString(currentSort);

  if (currentField !== field) {
    // New field - start with ascending
    return createSortString(field, 'asc');
  }

  // Same field - toggle direction
  return createSortString(field, direction === 'asc' ? 'desc' : 'asc');
}

/**
 * Merges query params with partial updates
 */
export function mergeQueryParams(
  current: QueryParams,
  updates: Partial<QueryParams>
): QueryParams {
  return {
    ...current,
    ...updates,
    filters: {
      ...current.filters,
      ...updates.filters
    }
  };
}

/**
 * Checks if there are any active filters
 */
export function hasActiveFilters(params: QueryParams): boolean {
  return !!(
    params.search ||
    params.dateFrom ||
    params.dateTo ||
    (params.filters && Object.keys(params.filters).length > 0)
  );
}

/**
 * Clears all filters from query params
 */
export function clearFilters(params: QueryParams): QueryParams {
  return {
    ...params,
    search: undefined,
    dateFrom: undefined,
    dateTo: undefined,
    filters: undefined,
    page: 0 // Reset to first page
  };
}
