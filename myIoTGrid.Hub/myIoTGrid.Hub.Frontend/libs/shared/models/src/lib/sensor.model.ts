/**
 * Communication protocol used by sensors.
 * Matches backend CommunicationProtocol enum.
 */
export enum CommunicationProtocol {
  I2C = 'I2C',
  SPI = 'SPI',
  OneWire = 'OneWire',
  Analog = 'Analog',
  UART = 'UART',
  Digital = 'Digital',
  UltraSonic = 'UltraSonic'
}

/**
 * Communication protocol labels for UI display
 */
export const COMMUNICATION_PROTOCOL_LABELS: Record<CommunicationProtocol, string> = {
  [CommunicationProtocol.I2C]: 'I²C',
  [CommunicationProtocol.SPI]: 'SPI',
  [CommunicationProtocol.OneWire]: '1-Wire',
  [CommunicationProtocol.Analog]: 'Analog',
  [CommunicationProtocol.UART]: 'UART',
  [CommunicationProtocol.Digital]: 'Digital',
  [CommunicationProtocol.UltraSonic]: 'Ultraschall'
};

/**
 * SensorCapability - Measurement capability of a sensor
 * Matches backend SensorCapabilityDto
 */
export interface SensorCapability {
  id: string;
  measurementType: string;
  displayName: string;
  unit: string;
  minValue?: number;
  maxValue?: number;
  resolution: number;
  accuracy: number;
  matterClusterId?: number;
  matterClusterName?: string;
  sortOrder: number;
  isActive: boolean;
}

/**
 * Standard Matter Cluster IDs
 */
export const MATTER_CLUSTERS = {
  TEMPERATURE: 0x0402,
  HUMIDITY: 0x0405,
  PRESSURE: 0x0403,
  ILLUMINANCE: 0x0400,
  OCCUPANCY: 0x0406,
  // Custom myIoTGrid (0xFC00+)
  WATER_LEVEL: 0xfc00,
  WATER_TEMPERATURE: 0xfc01,
  PH_VALUE: 0xfc02,
  FLOW_VELOCITY: 0xfc03,
  CO2: 0xfc04,
  PM25: 0xfc05,
  PM10: 0xfc06,
  SOIL_MOISTURE: 0xfc07,
  UV_INDEX: 0xfc08,
  WIND_SPEED: 0xfc09,
  RAINFALL: 0xfc0a,
  BATTERY: 0xfc0b,
  RSSI: 0xfc0c,
} as const;

export type SensorCategory = 'climate' | 'water' | 'location' | 'custom' | string;

/**
 * Sensor model - corresponds to Backend SensorDto (v3.0 Two-Tier Model)
 * All properties are now in Sensor (no more SensorType separation).
 */
export interface Sensor {
  [key: string]: unknown;
  id: string;
  tenantId: string;

  // Core Properties (formerly in SensorType)
  code: string;
  name: string;
  protocol: CommunicationProtocol;
  category: string;
  manufacturer?: string;
  model?: string;
  description?: string;

  // Hardware Configuration
  i2cAddress?: string;
  sdaPin?: number;
  sclPin?: number;
  oneWirePin?: number;
  analogPin?: number;
  digitalPin?: number;
  triggerPin?: number;
  echoPin?: number;
  baudRate?: number;

  // Timing Configuration
  intervalSeconds: number;
  minIntervalSeconds: number;
  warmupTimeMs: number;

  // Calibration
  offsetCorrection: number;
  gainCorrection: number;
  lastCalibratedAt?: string;
  calibrationNotes?: string;
  calibrationDueAt?: string;

  // Metadata
  icon?: string;
  color?: string;
  datasheetUrl?: string;
  serialNumber?: string;

  // Capabilities
  capabilities: SensorCapability[];

  // Status
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

/**
 * DTO for creating a SensorCapability
 * Matches backend CreateSensorCapabilityDto
 */
export interface CreateSensorCapabilityDto {
  measurementType: string;
  displayName: string;
  unit: string;
  minValue?: number;
  maxValue?: number;
  resolution?: number;
  accuracy?: number;
  matterClusterId?: number;
  matterClusterName?: string;
  sortOrder?: number;
}

/**
 * DTO for creating a Sensor (v3.0)
 * Matches backend CreateSensorDto
 */
export interface CreateSensorDto {
  code: string;
  name: string;
  protocol: CommunicationProtocol;
  category?: string;
  manufacturer?: string;
  model?: string;
  description?: string;
  serialNumber?: string;

  // Hardware Configuration
  i2cAddress?: string;
  sdaPin?: number;
  sclPin?: number;
  oneWirePin?: number;
  analogPin?: number;
  digitalPin?: number;
  triggerPin?: number;
  echoPin?: number;
  baudRate?: number;

  // Timing Configuration
  intervalSeconds?: number;
  minIntervalSeconds?: number;
  warmupTimeMs?: number;

  // Calibration
  offsetCorrection?: number;
  gainCorrection?: number;

  // Metadata
  icon?: string;
  color?: string;
  datasheetUrl?: string;

  // Capabilities
  capabilities?: CreateSensorCapabilityDto[];
}

/**
 * DTO for updating a SensorCapability
 * Matches backend UpdateSensorCapabilityDto
 * - If id is null/undefined, a new capability will be created.
 * - If id is set, the existing capability will be updated.
 * - Capabilities not included in the list will be deleted.
 */
export interface UpdateSensorCapabilityDto {
  id?: string;
  measurementType?: string;
  displayName?: string;
  unit?: string;
  minValue?: number;
  maxValue?: number;
  resolution?: number;
  accuracy?: number;
  matterClusterId?: number;
  matterClusterName?: string;
  sortOrder?: number;
  isActive?: boolean;
}

/**
 * DTO for updating a Sensor (v3.0)
 * Matches backend UpdateSensorDto
 */
export interface UpdateSensorDto {
  name?: string;
  manufacturer?: string;
  model?: string;
  description?: string;
  serialNumber?: string;

  // Hardware Configuration
  i2cAddress?: string;
  sdaPin?: number;
  sclPin?: number;
  oneWirePin?: number;
  analogPin?: number;
  digitalPin?: number;
  triggerPin?: number;
  echoPin?: number;
  baudRate?: number;

  // Timing Configuration
  intervalSeconds?: number;
  minIntervalSeconds?: number;
  warmupTimeMs?: number;

  // Calibration
  offsetCorrection?: number;
  gainCorrection?: number;
  calibrationNotes?: string;

  // Metadata
  icon?: string;
  color?: string;
  datasheetUrl?: string;

  // Status
  isActive?: boolean;

  // Capabilities (full replacement if provided)
  capabilities?: UpdateSensorCapabilityDto[];
}

/**
 * DTO for calibrating a Sensor
 * Matches backend CalibrateSensorDto
 */
export interface CalibrateSensorDto {
  offsetCorrection: number;
  gainCorrection?: number;
  calibrationNotes?: string;
  calibrationDueAt?: string;
}
