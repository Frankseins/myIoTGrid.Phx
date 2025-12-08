/**
 * NodeSensorAssignment model - corresponds to Backend NodeSensorAssignmentDto
 * Hardware binding of a Sensor to a Node with pin configuration and effective config.
 */
export interface NodeSensorAssignment {
  id: string;
  nodeId: string;
  nodeName: string;
  sensorId: string;
  sensorCode: string;
  sensorName: string;
  endpointId: number;
  alias?: string;
  i2cAddressOverride?: string;
  sdaPinOverride?: number;
  sclPinOverride?: number;
  oneWirePinOverride?: number;
  analogPinOverride?: number;
  digitalPinOverride?: number;
  triggerPinOverride?: number;
  echoPinOverride?: number;
  baudRateOverride?: number;
  intervalSecondsOverride?: number;
  isActive: boolean;
  lastSeenAt?: string;
  assignedAt: string;
  effectiveConfig: EffectiveConfig;
}

/**
 * DTO for creating a NodeSensorAssignment
 */
export interface CreateNodeSensorAssignmentDto {
  sensorId: string;
  endpointId: number;
  alias?: string;
  i2cAddressOverride?: string;
  sdaPinOverride?: number;
  sclPinOverride?: number;
  oneWirePinOverride?: number;
  analogPinOverride?: number;
  digitalPinOverride?: number;
  triggerPinOverride?: number;
  echoPinOverride?: number;
  baudRateOverride?: number;
  intervalSecondsOverride?: number;
}

/**
 * DTO for updating a NodeSensorAssignment
 */
export interface UpdateNodeSensorAssignmentDto {
  alias?: string;
  i2cAddressOverride?: string;
  sdaPinOverride?: number;
  sclPinOverride?: number;
  oneWirePinOverride?: number;
  analogPinOverride?: number;
  digitalPinOverride?: number;
  triggerPinOverride?: number;
  echoPinOverride?: number;
  baudRateOverride?: number;
  intervalSecondsOverride?: number;
  isActive?: boolean;
}

/**
 * Effective configuration after inheritance resolution.
 * EffectiveValue = Assignment ?? Sensor
 */
export interface EffectiveConfig {
  intervalSeconds: number;
  i2cAddress?: string;
  sdaPin?: number;
  sclPin?: number;
  oneWirePin?: number;
  analogPin?: number;
  digitalPin?: number;
  triggerPin?: number;
  echoPin?: number;
  baudRate?: number;
  offsetCorrection: number;
  gainCorrection: number;
}
