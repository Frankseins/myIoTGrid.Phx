import { TemplateRef } from '@angular/core';

/**
 * Column data type for formatting
 */
export type ColumnDataType =
  | 'text'
  | 'number'
  | 'date'
  | 'datetime'
  | 'boolean'
  | 'currency'
  | 'percent'
  | 'template'
  | 'custom';

/**
 * Configuration for a table column.
 * Used by GenericTableComponent.
 */
export interface ColumnConfig<T = unknown> {
  /** Unique identifier for the column (used for sorting) */
  key: string;

  /** Display header text */
  header: string;

  /** Property path for nested values (e.g., "location.name") */
  field?: string;

  /** Data type for formatting */
  type?: ColumnDataType;

  /** Whether the column is sortable (default: true) */
  sortable?: boolean;

  /** Whether the column is visible (default: true) */
  visible?: boolean;

  /** CSS class for the column */
  cssClass?: string;

  /** Header CSS class */
  headerClass?: string;

  /** Column width (e.g., "100px", "10%") */
  width?: string;

  /** Custom template for cell content */
  template?: TemplateRef<ColumnTemplateContext<T>>;

  /** Custom value formatter */
  formatter?: (value: unknown, row: T) => string;

  /** Number format (for type 'number' or 'currency') */
  numberFormat?: string;

  /** Date format (for type 'date' or 'datetime') */
  dateFormat?: string;

  /** Whether to show tooltip with full value */
  showTooltip?: boolean;

  /** Sticky column position */
  sticky?: 'start' | 'end';
}

/**
 * Context passed to column templates
 */
export interface ColumnTemplateContext<T> {
  /** The cell value */
  $implicit: unknown;

  /** The row data */
  row: T;

  /** The column configuration */
  column: ColumnConfig<T>;

  /** The row index */
  index: number;
}

/**
 * Table actions configuration
 */
export interface TableActionsConfig {
  /** Show view action */
  view?: boolean;

  /** Show edit action */
  edit?: boolean;

  /** Show delete action */
  delete?: boolean;
}

/**
 * Table configuration
 */
export interface TableConfig<T = unknown> {
  /** Column configurations */
  columns: ColumnConfig<T>[];

  /** Property to use as row identifier (default: 'id') */
  idProperty?: string;

  /** Whether to show row selection checkboxes */
  selectable?: boolean;

  /** Selection mode */
  selectionMode?: 'single' | 'multiple';

  /** Whether to show row actions column */
  showActions?: boolean;

  /** Actions column position */
  actionsPosition?: 'start' | 'end';

  /** Row CSS class function */
  rowClass?: (row: T, index: number) => string;

  /** Whether to show column visibility toggle */
  showColumnToggle?: boolean;

  /** Whether to show export button */
  showExport?: boolean;

  /** Storage key for persisting table state */
  storageKey?: string;

  /** Whether to show search input */
  showSearch?: boolean;

  /** Page size options */
  pageSizeOptions?: number[];

  /** Whether rows are clickable */
  rowClickable?: boolean;

  /** Actions configuration */
  actions?: TableActionsConfig;
}

/**
 * Default table configuration
 */
export const DEFAULT_TABLE_CONFIG: Partial<TableConfig> = {
  idProperty: 'id',
  selectable: false,
  selectionMode: 'multiple',
  showActions: true,
  actionsPosition: 'end',
  showColumnToggle: true,
  showExport: false
};

/**
 * Creates table configuration with defaults
 */
export function createTableConfig<T>(
  columns: ColumnConfig<T>[],
  overrides?: Partial<TableConfig<T>>
): TableConfig<T> {
  return {
    ...DEFAULT_TABLE_CONFIG,
    columns,
    ...overrides
  } as TableConfig<T>;
}
