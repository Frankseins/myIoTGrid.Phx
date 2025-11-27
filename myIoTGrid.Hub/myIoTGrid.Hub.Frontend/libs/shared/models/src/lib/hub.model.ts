import { Location } from './location.model';
import { Protocol } from './enums.model';

/**
 * Hub models - corresponds to Backend HubDto
 */
export interface Hub {
  id: string;
  tenantId: string;
  hubId: string;
  name: string;
  protocol: Protocol;
  defaultLocation?: Location;
  lastSeen?: string;
  isOnline: boolean;
  metadata?: string;
  createdAt: string;
}

export interface CreateHubDto {
  hubId: string;
  name: string;
  protocol?: Protocol;
  defaultLocation?: Location;
  metadata?: string;
}

export interface UpdateHubDto {
  name?: string;
  protocol?: Protocol;
  defaultLocation?: Location;
  metadata?: string;
}
