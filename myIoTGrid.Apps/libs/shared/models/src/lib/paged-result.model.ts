/**
 * Paginated result from server.
 * Corresponds to Backend PagedResultDto<T>.
 */
export interface PagedResult<T> {
  /** The items for the current page */
  items: T[];

  /** Total number of records across all pages */
  totalRecords: number;

  /** Current page (0-based) */
  page: number;

  /** Page size */
  size: number;

  /** Total number of pages */
  totalPages: number;

  /** Whether there is a next page */
  hasNextPage: boolean;

  /** Whether there is a previous page */
  hasPreviousPage: boolean;
}

/**
 * Creates an empty paged result
 */
export function emptyPagedResult<T>(): PagedResult<T> {
  return {
    items: [],
    totalRecords: 0,
    page: 0,
    size: 10,
    totalPages: 0,
    hasNextPage: false,
    hasPreviousPage: false
  };
}

/**
 * Maps the items of a paged result
 */
export function mapPagedResult<T, U>(
  result: PagedResult<T>,
  mapper: (item: T) => U
): PagedResult<U> {
  return {
    ...result,
    items: result.items.map(mapper)
  };
}
