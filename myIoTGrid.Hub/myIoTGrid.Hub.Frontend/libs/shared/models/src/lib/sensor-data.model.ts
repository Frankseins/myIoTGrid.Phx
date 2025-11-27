import { Location } from './location.model';

/**
 * SensorData models - corresponds to Backend SensorDataDto
 */
export interface SensorData {
  id: string;
  tenantId: string;
  hubId: string;
  sensorId?: string;
  sensorTypeId: string;
  sensorTypeCode: string;
  sensorTypeName: string;
  unit: string;
  value: number;
  timestamp: string;
  location?: Location;
  isSyncedToCloud: boolean;
}

export interface CreateSensorDataDto {
  sensorId: string;
  sensorType: string;
  value: number;
  hubId?: string;
  location?: Location;
}

export interface SensorDataFilter {
  hubId?: string;
  sensorId?: string;
  sensorTypeCode?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
