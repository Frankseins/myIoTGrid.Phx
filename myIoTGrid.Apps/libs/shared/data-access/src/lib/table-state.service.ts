import { Injectable, signal, computed } from '@angular/core';
import { QueryParams, DEFAULT_QUERY_PARAMS, createQueryParams, ColumnConfig } from '@myiotgrid/shared/models';

/**
 * State for a specific table
 */
export interface TableState {
  /** Query parameters for the table */
  queryParams: QueryParams;

  /** Selected row IDs */
  selectedIds: string[];

  /** Visible column keys */
  visibleColumns: string[];

  /** Column order */
  columnOrder: string[];
}

/**
 * Default table state
 */
const DEFAULT_TABLE_STATE: TableState = {
  queryParams: DEFAULT_QUERY_PARAMS,
  selectedIds: [],
  visibleColumns: [],
  columnOrder: []
};

/**
 * Service for persisting table state in LocalStorage.
 * Maintains query params, selection, and column visibility per table.
 */
@Injectable({ providedIn: 'root' })
export class TableStateService {
  private readonly STORAGE_PREFIX = 'myiotgrid_table_';

  /**
   * Gets the table state from LocalStorage
   */
  getState(storageKey: string): TableState {
    const stored = localStorage.getItem(this.getStorageKey(storageKey));
    if (stored) {
      try {
        const parsed = JSON.parse(stored);
        return {
          ...DEFAULT_TABLE_STATE,
          ...parsed,
          queryParams: {
            ...DEFAULT_QUERY_PARAMS,
            ...parsed.queryParams,
            // Convert date strings back to Date objects
            dateFrom: parsed.queryParams?.dateFrom
              ? new Date(parsed.queryParams.dateFrom)
              : undefined,
            dateTo: parsed.queryParams?.dateTo
              ? new Date(parsed.queryParams.dateTo)
              : undefined
          }
        };
      } catch {
        return DEFAULT_TABLE_STATE;
      }
    }
    return DEFAULT_TABLE_STATE;
  }

  /**
   * Saves the table state to LocalStorage
   */
  saveState(storageKey: string, state: Partial<TableState>): void {
    const currentState = this.getState(storageKey);
    const newState = { ...currentState, ...state };
    localStorage.setItem(
      this.getStorageKey(storageKey),
      JSON.stringify(newState)
    );
  }

  /**
   * Updates query params for a table
   */
  updateQueryParams(storageKey: string, params: Partial<QueryParams>): QueryParams {
    const state = this.getState(storageKey);
    const newParams = { ...state.queryParams, ...params };
    this.saveState(storageKey, { queryParams: newParams });
    return newParams;
  }

  /**
   * Resets query params to defaults
   */
  resetQueryParams(storageKey: string): QueryParams {
    this.saveState(storageKey, { queryParams: DEFAULT_QUERY_PARAMS });
    return DEFAULT_QUERY_PARAMS;
  }

  /**
   * Updates selected row IDs
   */
  updateSelection(storageKey: string, selectedIds: string[]): void {
    this.saveState(storageKey, { selectedIds });
  }

  /**
   * Updates visible columns
   */
  updateVisibleColumns(storageKey: string, visibleColumns: string[]): void {
    this.saveState(storageKey, { visibleColumns });
  }

  /**
   * Updates column order
   */
  updateColumnOrder(storageKey: string, columnOrder: string[]): void {
    this.saveState(storageKey, { columnOrder });
  }

  /**
   * Clears the table state
   */
  clearState(storageKey: string): void {
    localStorage.removeItem(this.getStorageKey(storageKey));
  }

  /**
   * Clears all table states
   */
  clearAllStates(): void {
    const keys = Object.keys(localStorage);
    keys.forEach(key => {
      if (key.startsWith(this.STORAGE_PREFIX)) {
        localStorage.removeItem(key);
      }
    });
  }

  /**
   * Gets the full storage key
   */
  private getStorageKey(key: string): string {
    return `${this.STORAGE_PREFIX}${key}`;
  }
}

/**
 * Creates a reactive table state manager using signals
 */
export function createTableStateManager(
  service: TableStateService,
  storageKey: string,
  defaultColumns: ColumnConfig[]
) {
  // Initialize state from storage
  const initialState = service.getState(storageKey);

  // Initialize visible columns if not set
  if (initialState.visibleColumns.length === 0) {
    initialState.visibleColumns = defaultColumns
      .filter(c => c.visible !== false)
      .map(c => c.key);
  }

  // Create signals
  const queryParams = signal<QueryParams>(initialState.queryParams);
  const selectedIds = signal<string[]>(initialState.selectedIds);
  const visibleColumns = signal<string[]>(initialState.visibleColumns);
  const columnOrder = signal<string[]>(
    initialState.columnOrder.length > 0
      ? initialState.columnOrder
      : defaultColumns.map(c => c.key)
  );

  // Computed: visible columns in order
  const orderedVisibleColumns = computed(() => {
    const order = columnOrder();
    const visible = visibleColumns();
    return order.filter(key => visible.includes(key));
  });

  return {
    queryParams,
    selectedIds,
    visibleColumns,
    columnOrder,
    orderedVisibleColumns,

    setPage(page: number) {
      const newParams = { ...queryParams(), page };
      queryParams.set(newParams);
      service.saveState(storageKey, { queryParams: newParams });
    },

    setPageSize(size: number) {
      const newParams = { ...queryParams(), size, page: 0 };
      queryParams.set(newParams);
      service.saveState(storageKey, { queryParams: newParams });
    },

    setSort(sort: string) {
      const newParams = { ...queryParams(), sort };
      queryParams.set(newParams);
      service.saveState(storageKey, { queryParams: newParams });
    },

    setSearch(search: string) {
      const newParams = { ...queryParams(), search, page: 0 };
      queryParams.set(newParams);
      service.saveState(storageKey, { queryParams: newParams });
    },

    setFilters(filters: Record<string, string>) {
      const newParams = { ...queryParams(), filters, page: 0 };
      queryParams.set(newParams);
      service.saveState(storageKey, { queryParams: newParams });
    },

    setDateRange(dateFrom?: Date, dateTo?: Date) {
      const newParams = { ...queryParams(), dateFrom, dateTo, page: 0 };
      queryParams.set(newParams);
      service.saveState(storageKey, { queryParams: newParams });
    },

    resetParams() {
      queryParams.set(DEFAULT_QUERY_PARAMS);
      service.saveState(storageKey, { queryParams: DEFAULT_QUERY_PARAMS });
    },

    setSelection(ids: string[]) {
      selectedIds.set(ids);
      service.saveState(storageKey, { selectedIds: ids });
    },

    toggleSelection(id: string) {
      const current = selectedIds();
      const newSelection = current.includes(id)
        ? current.filter(i => i !== id)
        : [...current, id];
      selectedIds.set(newSelection);
      service.saveState(storageKey, { selectedIds: newSelection });
    },

    clearSelection() {
      selectedIds.set([]);
      service.saveState(storageKey, { selectedIds: [] });
    },

    setVisibleColumns(columns: string[]) {
      visibleColumns.set(columns);
      service.saveState(storageKey, { visibleColumns: columns });
    },

    toggleColumn(key: string) {
      const current = visibleColumns();
      const newColumns = current.includes(key)
        ? current.filter(c => c !== key)
        : [...current, key];
      visibleColumns.set(newColumns);
      service.saveState(storageKey, { visibleColumns: newColumns });
    },

    setColumnOrder(order: string[]) {
      columnOrder.set(order);
      service.saveState(storageKey, { columnOrder: order });
    }
  };
}
