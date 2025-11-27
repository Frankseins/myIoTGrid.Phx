import { AlertLevel } from './enums.model';

/**
 * AlertType models - corresponds to Backend AlertTypeDto
 */
export interface AlertType {
  id: string;
  code: string;
  name: string;
  description?: string;
  defaultLevel: AlertLevel;
  iconName?: string;
  isGlobal: boolean;
  createdAt: string;
}

export interface CreateAlertTypeDto {
  code: string;
  name: string;
  description?: string;
  defaultLevel?: AlertLevel;
  iconName?: string;
}
