namespace myIoTGrid.Hub.Shared.DTOs;

/// <summary>
/// DTO for Reading (measurement) information.
/// Contains both raw and calibrated values.
/// </summary>
public record ReadingDto(
    long Id,
    Guid TenantId,
    Guid NodeId,
    string NodeName,
    Guid? AssignmentId,
    string MeasurementType,
    double RawValue,
    double Value,
    string Unit,
    DateTime Timestamp,
    LocationDto? Location,
    bool IsSyncedToCloud
);

/// <summary>
/// DTO for creating a Reading (from sensor/node).
/// Simple API for ESP32: nodeId as String, auto-applies calibration.
/// </summary>
public record CreateReadingDto(
    string NodeId,
    int EndpointId,
    string MeasurementType,
    double RawValue,
    string? HubId = null,
    DateTime? Timestamp = null
);

/// <summary>
/// DTO for filtering Readings
/// </summary>
public record ReadingFilterDto(
    Guid? NodeId = null,
    string? NodeIdentifier = null,
    Guid? HubId = null,
    Guid? AssignmentId = null,
    string? MeasurementType = null,
    DateTime? From = null,
    DateTime? To = null,
    bool? IsSyncedToCloud = null,
    int Page = 1,
    int PageSize = 50
);

/// <summary>
/// DTO for batch reading creation (multiple readings in one request)
/// </summary>
public record CreateBatchReadingsDto(
    string NodeId,
    string? HubId,
    IEnumerable<ReadingValueDto> Readings,
    DateTime? Timestamp = null
);

/// <summary>
/// Single reading value within a batch
/// </summary>
public record ReadingValueDto(
    int EndpointId,
    string MeasurementType,
    double RawValue
);

/// <summary>
/// DTO for creating a Reading from sensor device (ESP32/LoRa32).
/// Accepts the firmware's native payload format.
/// </summary>
public record CreateSensorReadingDto(
    string DeviceId,
    string Type,
    double Value,
    string? Unit = null,
    long? Timestamp = null
);
