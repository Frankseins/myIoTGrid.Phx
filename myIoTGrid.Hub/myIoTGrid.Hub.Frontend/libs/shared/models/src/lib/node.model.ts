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

