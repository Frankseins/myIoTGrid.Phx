using myIoTGrid.Shared.Common.Enums;

namespace myIoTGrid.Shared.Common.DTOs;

/// <summary>
/// DTO for Sensor information (v3.0).
/// Complete sensor definition with hardware configuration and calibration.
/// Two-tier model: Sensor → NodeSensorAssignment
/// </summary>
public record SensorDto(
    Guid Id,
    Guid TenantId,

    // === Identification ===
    string Code,
    string Name,
    string? Description,
    string? SerialNumber,

    // === Hardware Info ===
    string? Manufacturer,
    string? Model,
    string? DatasheetUrl,

    // === Communication Protocol ===
    CommunicationProtocolDto Protocol,

    // === Pin Configuration ===
    string? I2CAddress,
    int? SdaPin,
    int? SclPin,
    int? OneWirePin,
    int? AnalogPin,
    int? DigitalPin,
    int? TriggerPin,
    int? EchoPin,

    // === UART Configuration ===
    int? BaudRate,

    // === Timing Configuration ===
    int IntervalSeconds,
    int MinIntervalSeconds,
    int WarmupTimeMs,

    // === Calibration ===
    double OffsetCorrection,
    double GainCorrection,
    DateTime? LastCalibratedAt,
    string? CalibrationNotes,
    DateTime? CalibrationDueAt,

    // === Categorization ===
    string Category,
    string? Icon,
    string? Color,

    // === Capabilities ===
    IEnumerable<SensorCapabilityDto> Capabilities,

    // === Status ===
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// DTO for SensorCapability (measurement type).
/// Defines a single measurement capability of a Sensor.
/// Matter-konform: Corresponds to a Matter Cluster.
/// </summary>
public record SensorCapabilityDto(
    Guid Id,
    Guid SensorId,
    string MeasurementType,
    string DisplayName,
    string Unit,
    double? MinValue,
    double? MaxValue,
    double Resolution,
    double Accuracy,
    uint? MatterClusterId,
    string? MatterClusterName,
    int SortOrder,
    bool IsActive
);

/// <summary>
/// DTO for creating a Sensor (v3.0)
/// </summary>
public record CreateSensorDto(
    // === Required ===
    string Code,
    string Name,
    CommunicationProtocolDto Protocol,
    string Category,

    // === Optional Identification ===
    string? Description = null,
    string? SerialNumber = null,

    // === Optional Hardware Info ===
    string? Manufacturer = null,
    string? Model = null,
    string? DatasheetUrl = null,

    // === Optional Pin Configuration ===
    string? I2CAddress = null,
    int? SdaPin = null,
    int? SclPin = null,
    int? OneWirePin = null,
    int? AnalogPin = null,
    int? DigitalPin = null,
    int? TriggerPin = null,
    int? EchoPin = null,

    // === Optional UART Configuration ===
    int? BaudRate = null,

    // === Optional Timing Configuration ===
    int IntervalSeconds = 60,
    int MinIntervalSeconds = 1,
    int WarmupTimeMs = 0,

    // === Optional Calibration ===
    double OffsetCorrection = 0,
    double GainCorrection = 1.0,

    // === Optional Categorization ===
    string? Icon = null,
    string? Color = null,

    // === Optional Capabilities ===
    IEnumerable<CreateSensorCapabilityDto>? Capabilities = null
);

/// <summary>
/// DTO for creating a SensorCapability
/// </summary>
public record CreateSensorCapabilityDto(
    string MeasurementType,
    string DisplayName,
    string Unit,
    double? MinValue = null,
    double? MaxValue = null,
    double Resolution = 0.01,
    double Accuracy = 0.5,
    uint? MatterClusterId = null,
    string? MatterClusterName = null,
    int SortOrder = 0
);

/// <summary>
/// DTO for updating a Sensor (v3.0)
/// </summary>
public record UpdateSensorDto(
    // === Identification ===
    string? Name = null,
    string? Description = null,
    string? SerialNumber = null,

    // === Hardware Info ===
    string? Manufacturer = null,
    string? Model = null,
    string? DatasheetUrl = null,

    // === Pin Configuration ===
    string? I2CAddress = null,
    int? SdaPin = null,
    int? SclPin = null,
    int? OneWirePin = null,
    int? AnalogPin = null,
    int? DigitalPin = null,
    int? TriggerPin = null,
    int? EchoPin = null,

    // === UART Configuration ===
    int? BaudRate = null,

    // === Timing Configuration ===
    int? IntervalSeconds = null,
    int? MinIntervalSeconds = null,
    int? WarmupTimeMs = null,

    // === Calibration ===
    double? OffsetCorrection = null,
    double? GainCorrection = null,
    string? CalibrationNotes = null,

    // === Categorization ===
    string? Category = null,
    string? Icon = null,
    string? Color = null,

    // === Status ===
    bool? IsActive = null,

    // === Capabilities (full replacement) ===
    IEnumerable<UpdateSensorCapabilityDto>? Capabilities = null
);

/// <summary>
/// DTO for updating a SensorCapability.
/// If Id is null, a new capability will be created.
/// If Id is set, the existing capability will be updated.
/// Capabilities not included in the list will be deleted.
/// </summary>
public record UpdateSensorCapabilityDto(
    Guid? Id = null,
    string? MeasurementType = null,
    string? DisplayName = null,
    string? Unit = null,
    double? MinValue = null,
    double? MaxValue = null,
    double? Resolution = null,
    double? Accuracy = null,
    uint? MatterClusterId = null,
    string? MatterClusterName = null,
    int? SortOrder = null,
    bool? IsActive = null
);

/// <summary>
/// DTO for calibrating a Sensor
/// </summary>
public record CalibrateSensorDto(
    double OffsetCorrection,
    double GainCorrection = 1.0,
    string? CalibrationNotes = null,
    DateTime? CalibrationDueAt = null
);
