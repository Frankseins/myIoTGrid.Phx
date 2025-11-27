import { Location } from './location.model';

/**
 * Sensor models - corresponds to Backend SensorDto
 */
export interface Sensor {
  id: string;
  tenantId: string;
  hubId: string;
  sensorId: string;
  sensorTypeId: string;
  sensorTypeCode: string;
  sensorTypeName: string;
  unit: string;
  name: string;
  description?: string;
  defaultLocation?: Location;
  isActive: boolean;
  lastSeen?: string;
  isOnline: boolean;
  createdAt: string;
}

export interface CreateSensorDto {
  hubId: string;
  sensorId: string;
  sensorTypeCode: string;
  name: string;
  description?: string;
  defaultLocation?: Location;
}

export interface UpdateSensorDto {
  name?: string;
  description?: string;
  defaultLocation?: Location;
  isActive?: boolean;
}
