namespace myIoTGrid.Shared.Common.DTOs;

/// <summary>
/// DTO for a node with its sensors and their latest readings.
/// Groups readings by sensor (not by measurement type) to show unique sensors.
/// </summary>
public record NodeSensorsLatestDto(
    Guid NodeId,
    string NodeName,
    string? LocationName,
    IReadOnlyList<SensorLatestReadingDto> Sensors
);

/// <summary>
/// DTO for a single sensor with its latest reading.
/// Shows the display name (Alias > SensorName > SensorType fallback).
/// </summary>
public record SensorLatestReadingDto(
    /// <summary>Assignment ID (unique sensor instance on this node)</summary>
    Guid AssignmentId,
    /// <summary>Sensor ID</summary>
    Guid SensorId,
    /// <summary>Display name: Alias > Sensor.Name > "SensorCode #EndpointId"</summary>
    string DisplayName,
    /// <summary>Full sensor name (for tooltip)</summary>
    string FullName,
    /// <summary>Optional alias (short name)</summary>
    string? Alias,
    /// <summary>Sensor code (e.g., "bme280", "ds18b20")</summary>
    string SensorCode,
    /// <summary>Sensor model name</summary>
    string SensorModel,
    /// <summary>Endpoint ID on the node</summary>
    int EndpointId,
    /// <summary>Material icon name</summary>
    string? Icon,
    /// <summary>Hex color (e.g., "#FF5722")</summary>
    string? Color,
    /// <summary>Whether the sensor is active</summary>
    bool IsActive,
    /// <summary>Latest readings per measurement type</summary>
    IReadOnlyList<LatestMeasurementDto> Measurements
);

/// <summary>
/// DTO for a single measurement value (latest reading for a measurement type).
/// </summary>
public record LatestMeasurementDto(
    /// <summary>Reading ID</summary>
    long ReadingId,
    /// <summary>Measurement type (e.g., "temperature", "humidity")</summary>
    string MeasurementType,
    /// <summary>Display name for the measurement type</summary>
    string DisplayName,
    /// <summary>Raw value from sensor</summary>
    double RawValue,
    /// <summary>Calibrated value</summary>
    double Value,
    /// <summary>Unit of measurement (e.g., "°C", "%")</summary>
    string Unit,
    /// <summary>Timestamp of the reading</summary>
    DateTime Timestamp
);
