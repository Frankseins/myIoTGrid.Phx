import { AlertLevel, AlertSource } from './enums.model';

/**
 * Alert models - corresponds to Backend AlertDto
 */
export interface Alert {
  id: string;
  tenantId: string;
  hubId?: string;
  sensorId?: string;
  alertTypeId: string;
  alertTypeCode: string;
  alertTypeName: string;
  level: AlertLevel;
  message: string;
  recommendation?: string;
  source: AlertSource;
  createdAt: string;
  expiresAt?: string;
  acknowledgedAt?: string;
  isActive: boolean;
}

export interface CreateAlertDto {
  hubId?: string;
  sensorId?: string;
  alertTypeCode: string;
  level?: AlertLevel;
  message: string;
  recommendation?: string;
  source?: AlertSource;
  expiresAt?: string;
}

export interface AlertFilter {
  hubId?: string;
  sensorId?: string;
  level?: AlertLevel;
  isActive?: boolean;
  from?: string;
  to?: string;
}
