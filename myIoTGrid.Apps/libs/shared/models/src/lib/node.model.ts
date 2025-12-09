import { Location } from './location.model';
import { Protocol } from './enums.model';
import { Sensor } from './sensor.model';

/**
 * Node provisioning status
 */
export enum NodeProvisioningStatus {
  Unconfigured = 'Unconfigured',
  Pairing = 'Pairing',
  Configured = 'Configured',
  Error = 'Error'
}

/**
 * Storage mode for sensor readings (Sprint OS-01: Offline Storage)
 */
export enum StorageMode {
  /** Only send to Hub, no local storage (default) */
  RemoteOnly = 0,
  /** Store locally AND send to Hub simultaneously */
  LocalAndRemote = 1,
  /** Only store locally, never send to Hub */
  LocalOnly = 2,
  /** Store locally, auto-sync when WiFi available */
  LocalAutoSync = 3
}

/**
 * Node model - corresponds to Backend NodeDto
 * Matter-konform: Entspricht einem Matter Node (ESP32/LoRa32 device)
 */
export interface Node {
  [key: string]: unknown;
  id: string;
  hubId: string;
  nodeId: string;
  name: string;
  protocol: Protocol;
  location?: Location;
  assignmentCount: number;
  lastSeen?: string;
  isOnline: boolean;
  firmwareVersion?: string;
  batteryLevel?: number;
  createdAt: string;
  macAddress: string;
  status: NodeProvisioningStatus;
  /** Whether this node generates simulated sensor values */
  isSimulation: boolean;
  /** Optional: Assigned sensors (populated in some API responses) */
  sensors?: Sensor[];
  // Sprint OS-01: Offline Storage
  /** Storage mode for sensor readings */
  storageMode: StorageMode;
  /** Number of readings pending sync on the device */
  pendingSyncCount: number;
  /** Last successful sync timestamp */
  lastSyncAt?: string;
  /** Last sync error message (null if no error) */
  lastSyncError?: string;
}

export interface CreateNodeDto {
  nodeId: string;
  name?: string;
  hubIdentifier?: string;
  hubId?: string;
  protocol?: Protocol;
  location?: Location;
}

export interface UpdateNodeDto {
  name?: string;
  location?: Location;
  firmwareVersion?: string;
  isSimulation?: boolean;
  // Sprint OS-01: Offline Storage
  storageMode?: StorageMode;
}

export interface NodeStatusDto {
  nodeId: string;
  isOnline: boolean;
  lastSeen?: string;
  batteryLevel?: number;
}

/**
 * Node with its sensors and their latest readings.
 * Groups by sensor (not by measurement type) to show unique sensors.
 */
export interface NodeSensorsLatest {
  nodeId: string;
  nodeName: string;
  locationName?: string;
  sensors: SensorLatestReading[];
}

/**
 * A single sensor with its latest reading.
 */
export interface SensorLatestReading {
  /** Assignment ID (unique sensor instance on this node) */
  assignmentId: string;
  /** Sensor ID */
  sensorId: string;
  /** Display name: Alias > Sensor.Name > "SensorCode #EndpointId" */
  displayName: string;
  /** Full sensor name (for tooltip) */
  fullName: string;
  /** Optional alias (short name) */
  alias?: string;
  /** Sensor code (e.g., "bme280", "ds18b20") */
  sensorCode: string;
  /** Sensor model name */
  sensorModel: string;
  /** Endpoint ID on the node */
  endpointId: number;
  /** Material icon name */
  icon?: string;
  /** Hex color (e.g., "#FF5722") */
  color?: string;
  /** Whether the sensor is active */
  isActive: boolean;
  /** Latest readings per measurement type */
  measurements: LatestMeasurement[];
}

/**
 * A single measurement value (latest reading for a measurement type).
 */
export interface LatestMeasurement {
  /** Reading ID */
  readingId: number;
  /** Measurement type (e.g., "temperature", "humidity") */
  measurementType: string;
  /** Display name for the measurement type */
  displayName: string;
  /** Raw value from sensor */
  rawValue: number;
  /** Calibrated value */
  value: number;
  /** Unit of measurement (e.g., "°C", "%") */
  unit: string;
  /** Timestamp of the reading */
  timestamp: string;
}

// === GPS Status DTOs ===

/**
 * GPS status aggregated from latest readings.
 * Provides fix quality, satellite count, and position data.
 */
export interface NodeGpsStatus {
  nodeId: string;
  nodeName: string;
  hasGps: boolean;
  satellites: number;
  fixType: number;
  fixTypeText: string;
  hdop: number;
  hdopQuality: string;
  latitude?: number;
  longitude?: number;
  altitude?: number;
  speed?: number;
  lastUpdate?: string;
}

/**
 * GPS quality level for visual display
 */
export type GpsQualityLevel = 'excellent' | 'good' | 'moderate' | 'poor' | 'nofix';

/**
 * Helper to determine GPS quality level from HDOP
 */
export function getGpsQualityLevel(hdop: number, hasFix: boolean): GpsQualityLevel {
  if (!hasFix) return 'nofix';
  if (hdop < 2) return 'excellent';
  if (hdop < 5) return 'good';
  if (hdop < 10) return 'moderate';
  return 'poor';
}

/**
 * GPS position data
 */
export interface GpsPosition {
  latitude: number;
  longitude: number;
  altitude?: number;
  speed?: number;
  timestamp: string;
}

// === Sprint 8: Remote Debug System ===

/**
 * Debug levels for node logging
 */
export enum DebugLevel {
  Production = 'Production',
  Normal = 'Normal',
  Debug = 'Debug'
}

/**
 * Log categories for filtering
 */
export enum LogCategory {
  System = 'System',
  Hardware = 'Hardware',
  Network = 'Network',
  Sensor = 'Sensor',
  GPS = 'GPS',
  API = 'API',
  Storage = 'Storage',
  Error = 'Error'
}

/**
 * Debug configuration for a node
 */
export interface NodeDebugConfiguration {
  nodeId: string;
  nodeName: string;
  debugLevel: DebugLevel;
  enableRemoteLogging: boolean;
  lastDebugChange?: string;
}

/**
 * Set debug level request
 */
export interface SetNodeDebugLevelDto {
  debugLevel: DebugLevel;
  enableRemoteLogging: boolean;
}

/**
 * Debug log entry
 */
export interface NodeDebugLog {
  id: string;
  nodeId: string;
  nodeTimestamp: number;
  receivedAt: string;
  level: DebugLevel;
  category: LogCategory;
  message: string;
  stackTrace?: string;
}

/**
 * Filter for debug logs
 */
export interface DebugLogFilter {
  nodeId?: string;
  minLevel?: DebugLevel;
  category?: LogCategory;
  fromDate?: string;
  toDate?: string;
  pageNumber?: number;
  pageSize?: number;
}

/**
 * Error statistics for a node
 */
export interface NodeErrorStatistics {
  nodeId: string;
  nodeName: string;
  totalLogs: number;
  errorCount: number;
  warningCount: number;
  infoCount: number;
  errorsByCategory: { [key: string]: number };
  lastErrorAt?: string;
  lastErrorMessage?: string;
}

/**
 * Debug log cleanup result
 */
export interface DebugLogCleanupResult {
  deletedCount: number;
  cleanupBefore: string;
}

// === Hardware Status (Sprint 8) ===

/**
 * Complete hardware status for a node
 */
export interface NodeHardwareStatus {
  nodeId: string;
  serialNumber: string;
  firmwareVersion: string;
  hardwareType: string;
  reportedAt: string;
  summary: HardwareSummary;
  detectedDevices: DetectedDevice[];
  storage: StorageStatus;
  busStatus: BusStatus;
}

/**
 * Summary of hardware status
 */
export interface HardwareSummary {
  totalDevicesDetected: number;
  sensorsConfigured: number;
  sensorsOk: number;
  sensorsError: number;
  hasSdCard: boolean;
  hasGps: boolean;
  overallStatus: 'OK' | 'Warning' | 'Error';
}

/**
 * A detected hardware device
 */
export interface DetectedDevice {
  deviceType: string;
  bus: string;
  address: string;
  status: 'OK' | 'Error' | 'NotConfigured';
  sensorCode?: string;
  endpointId?: number;
  errorMessage?: string;
}

/**
 * Storage (SD card) status
 */
export interface StorageStatus {
  available: boolean;
  mode: string;
  totalBytes: number;
  usedBytes: number;
  freeBytes: number;
  pendingSyncCount: number;
  lastSyncAt?: string;
  lastSyncError?: string;
}

/**
 * Bus status (I2C, UART, etc.)
 */
export interface BusStatus {
  i2cAvailable: boolean;
  i2cDeviceCount: number;
  i2cAddresses: string[];
  oneWireAvailable: boolean;
  oneWireDeviceCount: number;
  uartAvailable: boolean;
  gpsDetected: boolean;
}

