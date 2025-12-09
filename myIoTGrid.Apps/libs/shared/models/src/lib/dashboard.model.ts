/**
 * Dashboard Models for Sparkline Widgets (Home Assistant style)
 */

/**
 * Time period for sparkline data
 */
export enum SparklinePeriod {
  Hour = 'Hour',
  Day = 'Day',
  Week = 'Week'
}

/**
 * Complete dashboard with locations and their sensor widgets
 */
export interface LocationDashboard {
  locations: LocationGroup[];
}

/**
 * Location group with sensor widgets
 */
export interface LocationGroup {
  locationName: string;
  locationIcon: string | null;
  isHero: boolean;
  widgets: SensorWidget[];
}

/**
 * Single sensor widget with sparkline data
 */
export interface SensorWidget {
  widgetId: string;
  nodeId: string;
  nodeName: string;
  assignmentId: string | null;
  sensorId: string | null;
  measurementType: string;
  sensorName: string;
  locationName: string;
  label: string;
  unit: string;
  color: string;
  currentValue: number;
  lastUpdate: string;
  minMax: MinMax;
  dataPoints: SparklinePoint[];
}

/**
 * Min/Max values with timestamps
 */
export interface MinMax {
  minValue: number;
  minTimestamp: string;
  maxValue: number;
  maxTimestamp: string;
}

/**
 * Single sparkline data point
 */
export interface SparklinePoint {
  timestamp: string;
  value: number;
}

/**
 * Dashboard filter options
 */
export interface DashboardFilterOptions {
  locations: string[];
  measurementTypes: string[];
}

/**
 * Dashboard filter request
 */
export interface DashboardFilter {
  locations?: string[];
  measurementTypes?: string[];
  period?: SparklinePeriod;
}
