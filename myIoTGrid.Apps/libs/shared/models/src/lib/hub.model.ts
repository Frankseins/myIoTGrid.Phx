import { Location } from './location.model';
import { Protocol } from './enums.model';

/**
 * Hub models - corresponds to Backend HubDto
 * Single-Hub-Architecture: Only one Hub per installation
 */
export interface Hub {
  [key: string]: unknown;
  id: string;
  tenantId: string;
  hubId: string;
  name: string;
  description?: string;
  protocol?: Protocol;
  defaultLocation?: Location;
  lastSeen?: string;
  isOnline: boolean;
  metadata?: string;
  createdAt: string;
  sensorCount?: number;
  // Provisioning defaults for new nodes
  defaultWifiSsid?: string;
  defaultWifiPassword?: string;
  apiUrl?: string;
  apiPort: number;
}

/**
 * Hub status information
 */
export interface HubStatus {
  isOnline: boolean;
  lastSeen?: string;
  nodeCount: number;
  onlineNodeCount: number;
  services: ServiceStatus;
}

/**
 * Individual service status
 */
export interface ServiceStatus {
  api: ServiceState;
  database: ServiceState;
  mqtt: ServiceState;
  cloud: ServiceState;
}

/**
 * State of a single service
 */
export interface ServiceState {
  isOnline: boolean;
  message?: string;
}

export interface CreateHubDto {
  hubId: string;
  name: string;
  description?: string;
  protocol?: Protocol;
  defaultLocation?: Location;
  metadata?: string;
}

export interface UpdateHubDto {
  name?: string;
  description?: string;
  protocol?: Protocol;
  defaultLocation?: Location;
  metadata?: string;
  // Provisioning defaults for new nodes
  defaultWifiSsid?: string;
  defaultWifiPassword?: string;
  apiUrl?: string;
  apiPort?: number;
}

/**
 * Provisioning settings for new nodes (BLE setup)
 */
export interface HubProvisioningSettings {
  defaultWifiSsid?: string;
  defaultWifiPassword?: string;
  apiUrl?: string;
  apiPort: number;
}
