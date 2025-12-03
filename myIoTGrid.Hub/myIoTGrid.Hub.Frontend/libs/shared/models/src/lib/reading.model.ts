import { Location } from './location.model';

/**
 * Reading model - corresponds to Backend ReadingDto
 * Matter-konform: Entspricht einem Matter Attribute Report
 */
export interface Reading {
  [key: string]: unknown;
  id: number;
  tenantId: string;
  nodeId: string;
  sensorId: string;
  sensorCode: string;
  sensorName: string;
  measurementType: string;
  displayName: string;
  value: number;
  unit: string;
  timestamp: string;
  location?: Location;
  isSyncedToCloud: boolean;
}

export interface CreateReadingDto {
  nodeId: string;
  type: string;
  value: number;
  hubId?: string;
  timestamp?: string;
}

export interface ReadingFilter {
  nodeId?: string;
  nodeIdentifier?: string;
  hubId?: string;
  sensorId?: string;
  measurementType?: string;
  from?: string;
  to?: string;
  isSyncedToCloud?: boolean;
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

export interface LatestReadingDto {
  nodeId: string;
  readings: { [measurementType: string]: Reading };
}
