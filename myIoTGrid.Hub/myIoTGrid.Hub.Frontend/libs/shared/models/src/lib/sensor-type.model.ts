/**
 * SensorType models - corresponds to Backend SensorTypeDto
 */
export interface SensorType {
  id: string;
  code: string;
  name: string;
  unit: string;
  description?: string;
  iconName?: string;
  isGlobal: boolean;
  createdAt: string;
}

export interface CreateSensorTypeDto {
  code: string;
  name: string;
  unit: string;
  description?: string;
  iconName?: string;
}
