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

/**
 * Hub properties for sensor setup (Hub/Cloud selection)
 * Contains Address, Port, TenantID (GUID), TenantName and Version.
 */
export interface HubProperties {
  address: string;
  port: number;
  tenantId: string;
  tenantName: string;
  version: string;
  cloudAddress: string;
  cloudPort: number;
}

/**
 * Sensor target mode - where the sensor sends data
 */
export type SensorTargetMode = 'local' | 'cloud';

/**
 * Sensor connection type - how the sensor connects (WiFi or Bluetooth)
 * Only applicable when mode is 'local'
 */
export type SensorConnectionType = 'wifi' | 'bluetooth';

/**
 * Sensor target configuration for setup wizard
 */
export interface SensorTargetConfig {
  mode: SensorTargetMode;
  address: string;
  port: number;
  tenantId: string;
  tenantName: string;
  useSsl: boolean;
  connectionType: SensorConnectionType;  // WiFi or Bluetooth connection
}

/**
 * BluetoothHub - Bluetooth Gateway for ESP32 BLE sensors
 * Corresponds to Backend BluetoothHubDto
 */
export interface BluetoothHub {
  id: string;
  hubId: string;
  name: string;
  macAddress?: string;
  status: string;  // Active, Inactive, Error
  lastSeen?: string;
  createdAt: string;
  updatedAt: string;
  nodeCount: number;
}

/**
 * Create BluetoothHub DTO
 */
export interface CreateBluetoothHubDto {
  name: string;
  macAddress?: string;
  hubId?: string;
}

/**
 * Update BluetoothHub DTO
 */
export interface UpdateBluetoothHubDto {
  name?: string;
  macAddress?: string;
  status?: string;
}
