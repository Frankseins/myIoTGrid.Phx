namespace myIoTGrid.Hub.Shared.DTOs;

/// <summary>
/// DTO for measurement data information
/// </summary>
/// <param name="Id">Primary key</param>
/// <param name="TenantId">Tenant-ID</param>
/// <param name="SensorId">Sensor ID (FK)</param>
/// <param name="SensorTypeId">Sensor type ID</param>
/// <param name="SensorTypeCode">Sensor type code</param>
/// <param name="SensorTypeName">Sensor type display name</param>
/// <param name="Unit">Unit of measurement</param>
/// <param name="Value">Measurement value</param>
/// <param name="Timestamp">Timestamp of measurement</param>
/// <param name="Location">Location (inherited from Sensor)</param>
/// <param name="IsSyncedToCloud">Whether synced to cloud</param>
public record SensorDataDto(
    Guid Id,
    Guid TenantId,
    Guid SensorId,
    Guid SensorTypeId,
    string SensorTypeCode,
    string SensorTypeName,
    string Unit,
    double Value,
    DateTime Timestamp,
    LocationDto? Location,
    bool IsSyncedToCloud
);

/// <summary>
/// DTO for creating measurement data (from sensor)
/// </summary>
/// <param name="SensorId">Sensor identifier (e.g., "sensor-wohnzimmer-01")</param>
/// <param name="SensorType">Sensor type code (e.g., "temperature")</param>
/// <param name="Value">Measurement value</param>
/// <param name="HubId">Optional Hub identifier for auto-registration</param>
/// <param name="Timestamp">Optional timestamp (default: now)</param>
public record CreateSensorDataDto(
    string SensorId,
    string SensorType,
    double Value,
    string? HubId = null,
    DateTime? Timestamp = null
);

/// <summary>
/// DTO for filtering measurement data
/// </summary>
/// <param name="SensorId">Filter by Sensor (Guid)</param>
/// <param name="SensorIdentifier">Filter by Sensor identifier (string)</param>
/// <param name="HubId">Filter by Hub (Guid)</param>
/// <param name="SensorTypeCode">Filter by sensor type code</param>
/// <param name="From">Time range start</param>
/// <param name="To">Time range end</param>
/// <param name="IsSyncedToCloud">Filter by sync status</param>
/// <param name="Page">Page number (1-based)</param>
/// <param name="PageSize">Items per page</param>
public record SensorDataFilterDto(
    Guid? SensorId = null,
    string? SensorIdentifier = null,
    Guid? HubId = null,
    string? SensorTypeCode = null,
    DateTime? From = null,
    DateTime? To = null,
    bool? IsSyncedToCloud = null,
    int Page = 1,
    int PageSize = 50
);
