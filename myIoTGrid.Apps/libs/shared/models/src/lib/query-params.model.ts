/**
 * Query parameters for server-side paging, sorting, and filtering.
 * Corresponds to Backend QueryParamsDto.
 */
export interface QueryParams {
  /** Current page (0-based) */
  page: number;

  /** Page size (items per page) */
  size: number;

  /** Sort field and direction (e.g., "name,asc" or "createdAt,desc") */
  sort?: string;

  /** Global search term */
  search?: string;

  /** Date range filter start */
  dateFrom?: Date;

  /** Date range filter end */
  dateTo?: Date;

  /** Additional filters (key-value pairs) */
  filters?: Record<string, string>;
}

/**
 * Default query parameters
 */
export const DEFAULT_QUERY_PARAMS: QueryParams = {
  page: 0,
  size: 10
};

/**
 * Creates query parameters with defaults
 */
export function createQueryParams(overrides?: Partial<QueryParams>): QueryParams {
  return { ...DEFAULT_QUERY_PARAMS, ...overrides };
}

/**
 * Converts QueryParams to URL search params
 */
export function toHttpParams(params: QueryParams): Record<string, string> {
  const httpParams: Record<string, string> = {
    page: params.page.toString(),
    size: params.size.toString()
  };

  if (params.sort) {
    httpParams['sort'] = params.sort;
  }

  if (params.search) {
    httpParams['search'] = params.search;
  }

  if (params.dateFrom) {
    httpParams['dateFrom'] = params.dateFrom.toISOString();
  }

  if (params.dateTo) {
    httpParams['dateTo'] = params.dateTo.toISOString();
  }

  if (params.filters) {
    for (const [key, value] of Object.entries(params.filters)) {
      httpParams[`filters[${key}]`] = value;
    }
  }

  return httpParams;
}

/**
 * Material Table Lazy Load Event
 * Used for converting Material Table events to QueryParams
 */
export interface MaterialLazyEvent {
  /** First row index (for pagination) */
  first: number;

  /** Number of rows per page */
  rows: number;

  /** Sort field name */
  sortField?: string;

  /** Sort order: 1 = ascending, -1 = descending */
  sortOrder?: 1 | -1;

  /** Global filter value */
  globalFilter?: string;

  /** Column filters */
  filters?: Record<string, unknown>;
}

/**
 * Converts Material lazy event to QueryParams
 */
export function lazyEventToQueryParams(event: MaterialLazyEvent): QueryParams {
  const page = event.rows > 0 ? Math.floor(event.first / event.rows) : 0;

  let sort: string | undefined;
  if (event.sortField) {
    const direction = event.sortOrder === -1 ? 'desc' : 'asc';
    sort = `${event.sortField},${direction}`;
  }

  const filters: Record<string, string> = {};
  if (event.filters) {
    for (const [key, value] of Object.entries(event.filters)) {
      if (value != null && value !== '') {
        filters[key] = String(value);
      }
    }
  }

  return {
    page,
    size: event.rows || 10,
    sort,
    search: event.globalFilter || undefined,
    filters: Object.keys(filters).length > 0 ? filters : undefined
  };
}

/**
 * Parse sort string to field and direction
 */
export function parseSortString(sort?: string): { field?: string; direction?: 'asc' | 'desc' } {
  if (!sort) return {};
  const [field, direction] = sort.split(',');
  return {
    field,
    direction: direction as 'asc' | 'desc' | undefined
  };
}

/**
 * Create sort string from field and direction
 */
export function createSortString(field: string, direction: 'asc' | 'desc'): string {
  return `${field},${direction}`;
}
