/**
 * Time intervals for chart data aggregation.
 */
export enum ChartInterval {
  OneHour = 'OneHour',
  OneDay = 'OneDay',
  OneWeek = 'OneWeek',
  OneMonth = 'OneMonth',
  ThreeMonths = 'ThreeMonths',
  SixMonths = 'SixMonths',
  OneYear = 'OneYear'
}

/**
 * Label mapping for intervals (Börsen-Style)
 */
export const ChartIntervalLabels: Record<ChartInterval, string> = {
  [ChartInterval.OneHour]: '1H',
  [ChartInterval.OneDay]: '1D',
  [ChartInterval.OneWeek]: '1W',
  [ChartInterval.OneMonth]: '1M',
  [ChartInterval.ThreeMonths]: '3M',
  [ChartInterval.SixMonths]: '6M',
  [ChartInterval.OneYear]: '1Y'
};

/**
 * Complete chart data for widget detail view.
 */
export interface ChartData {
  nodeId: string;
  nodeName: string;
  assignmentId: string | null;
  sensorId: string | null;
  sensorName: string;
  measurementType: string;
  locationName: string;
  unit: string;
  color: string;
  currentValue: number;
  lastUpdate: string;
  stats: ChartStats;
  trend: Trend;
  dataPoints: ChartPoint[];
}

/**
 * Statistics for a chart period.
 */
export interface ChartStats {
  minValue: number;
  minTimestamp: string;
  maxValue: number;
  maxTimestamp: string;
  avgValue: number;
}

/**
 * Trend information comparing current value to previous period.
 */
export interface Trend {
  change: number;
  changePercent: number;
  direction: 'up' | 'down' | 'stable';
}

/**
 * Single data point for chart.
 */
export interface ChartPoint {
  timestamp: string;
  value: number;
  min?: number | null;
  max?: number | null;
}

/**
 * Request parameters for readings list.
 */
export interface ReadingsListRequest {
  page?: number;
  pageSize?: number;
  from?: string;
  to?: string;
}

/**
 * Single reading for the list view.
 */
export interface ReadingListItem {
  id: number;
  timestamp: string;
  value: number;
  unit: string;
  trendDirection?: 'up' | 'down' | 'stable' | null;
}

/**
 * Paginated readings list response.
 */
export interface ReadingsList {
  items: ReadingListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
