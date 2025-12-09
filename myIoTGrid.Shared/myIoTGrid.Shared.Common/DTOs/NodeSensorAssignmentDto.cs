namespace myIoTGrid.Shared.Common.DTOs;

/// <summary>
/// DTO for NodeSensorAssignment information (v3.0).
/// Hardware binding of a Sensor to a Node.
/// Two-tier model: Sensor → NodeSensorAssignment
/// </summary>
public record NodeSensorAssignmentDto(
    Guid Id,
    Guid NodeId,
    string NodeName,
    Guid SensorId,
    string SensorCode,
    string SensorName,
    int EndpointId,
    string? Alias,
    string? I2CAddressOverride,
    int? SdaPinOverride,
    int? SclPinOverride,
    int? OneWirePinOverride,
    int? AnalogPinOverride,
    int? DigitalPinOverride,
    int? TriggerPinOverride,
    int? EchoPinOverride,
    int? BaudRateOverride,
    int? IntervalSecondsOverride,
    bool IsActive,
    DateTime? LastSeenAt,
    DateTime AssignedAt,
    EffectiveConfigDto EffectiveConfig
);

/// <summary>
/// DTO for creating a NodeSensorAssignment
/// </summary>
public record CreateNodeSensorAssignmentDto(
    Guid SensorId,
    int EndpointId,
    string? Alias = null,
    string? I2CAddressOverride = null,
    int? SdaPinOverride = null,
    int? SclPinOverride = null,
    int? OneWirePinOverride = null,
    int? AnalogPinOverride = null,
    int? DigitalPinOverride = null,
    int? TriggerPinOverride = null,
    int? EchoPinOverride = null,
    int? BaudRateOverride = null,
    int? IntervalSecondsOverride = null
);

/// <summary>
/// DTO for updating a NodeSensorAssignment
/// </summary>
public record UpdateNodeSensorAssignmentDto(
    string? Alias = null,
    string? I2CAddressOverride = null,
    int? SdaPinOverride = null,
    int? SclPinOverride = null,
    int? OneWirePinOverride = null,
    int? AnalogPinOverride = null,
    int? DigitalPinOverride = null,
    int? TriggerPinOverride = null,
    int? EchoPinOverride = null,
    int? BaudRateOverride = null,
    int? IntervalSecondsOverride = null,
    bool? IsActive = null
);

/// <summary>
/// DTO for effective configuration after inheritance resolution (v3.0).
/// EffectiveValue = Assignment ?? Sensor
/// Two-tier inheritance model.
/// </summary>
public record EffectiveConfigDto(
    int IntervalSeconds,
    string? I2CAddress,
    int? SdaPin,
    int? SclPin,
    int? OneWirePin,
    int? AnalogPin,
    int? DigitalPin,
    int? TriggerPin,
    int? EchoPin,
    int? BaudRate,
    double OffsetCorrection,
    double GainCorrection
);
