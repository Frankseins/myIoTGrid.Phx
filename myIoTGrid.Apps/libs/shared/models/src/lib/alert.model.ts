import { AlertLevel, AlertSource } from './enums.model';

/**
 * Alert models - corresponds to Backend AlertDto
 */
export interface Alert {
  [key: string]: unknown;
  id: string;
  tenantId: string;
  hubId?: string;
  nodeId?: string;
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
  nodeId?: string;
  alertTypeCode: string;
  level?: AlertLevel;
  message: string;
  recommendation?: string;
  source?: AlertSource;
  expiresAt?: string;
}

export interface AlertFilter {
  hubId?: string;
  nodeId?: string;
  level?: AlertLevel;
  isActive?: boolean;
  from?: string;
  to?: string;
}
