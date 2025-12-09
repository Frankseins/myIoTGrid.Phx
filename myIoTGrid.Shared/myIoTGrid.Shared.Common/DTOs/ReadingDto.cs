namespace myIoTGrid.Shared.Common.DTOs;

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
    Guid? SensorId,
    string SensorCode,
    string SensorName,
    string? SensorIcon,
    string? SensorColor,
    string MeasurementType,
    string DisplayName,
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
    long? Timestamp = null,
    /// <summary>
    /// Optional EndpointId to identify which sensor assignment this reading belongs to.
    /// Required when multiple sensors have the same measurement type (e.g., BME280 and DHT22 both measure temperature).
    /// </summary>
    int? EndpointId = null
);

/// <summary>
/// DTO for deleting readings in a date range.
/// Allows filtering by node, sensor assignment, and measurement type.
/// </summary>
public record DeleteReadingsRangeDto(
    /// <summary>Node ID (required) - readings will only be deleted for this node</summary>
    Guid NodeId,
    /// <summary>Start of the date range (inclusive)</summary>
    DateTime From,
    /// <summary>End of the date range (inclusive)</summary>
    DateTime To,
    /// <summary>Optional: Only delete readings for a specific sensor assignment</summary>
    Guid? AssignmentId = null,
    /// <summary>Optional: Only delete readings for a specific measurement type (e.g., "temperature")</summary>
    string? MeasurementType = null
);

/// <summary>
/// Response DTO for delete readings operation.
/// Returns the count of deleted readings.
/// </summary>
public record DeleteReadingsResultDto(
    /// <summary>Number of readings that were deleted</summary>
    int DeletedCount,
    /// <summary>The filter criteria that were applied</summary>
    Guid NodeId,
    DateTime From,
    DateTime To,
    Guid? AssignmentId,
    string? MeasurementType
);

/// <summary>
/// Response DTO for batch readings creation (Sprint OS-01: Offline Storage).
/// Returns summary of batch upload result.
/// </summary>
public record BatchReadingsResultDto(
    /// <summary>Number of readings successfully created</summary>
    int SuccessCount,
    /// <summary>Number of readings that failed to create</summary>
    int FailedCount,
    /// <summary>Total readings in the batch</summary>
    int TotalCount,
    /// <summary>Node ID that received the readings</summary>
    string NodeId,
    /// <summary>Timestamp when batch was processed</summary>
    DateTime ProcessedAt,
    /// <summary>Error messages for failed readings (if any)</summary>
    IEnumerable<string>? Errors = null
);
