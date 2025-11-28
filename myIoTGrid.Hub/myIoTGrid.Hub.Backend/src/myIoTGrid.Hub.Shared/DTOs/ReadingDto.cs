namespace myIoTGrid.Hub.Shared.DTOs;

/// <summary>
/// DTO for Reading (measurement) information.
/// Matter-konform: Entspricht einem Attribute Report.
/// </summary>
/// <param name="Id">Primary key</param>
/// <param name="TenantId">Tenant-ID</param>
/// <param name="NodeId">Node ID (FK)</param>
/// <param name="SensorTypeId">Sensor type ID (e.g., "temperature")</param>
/// <param name="SensorTypeName">Sensor type display name</param>
/// <param name="Value">Measurement value</param>
/// <param name="Unit">Unit of measurement</param>
/// <param name="Timestamp">Timestamp of measurement</param>
/// <param name="Location">Location (inherited from Node)</param>
/// <param name="IsSyncedToCloud">Whether synced to cloud</param>
public record ReadingDto(
    long Id,
    Guid TenantId,
    Guid NodeId,
    string SensorTypeId,
    string SensorTypeName,
    double Value,
    string Unit,
    DateTime Timestamp,
    LocationDto? Location,
    bool IsSyncedToCloud
);

/// <summary>
/// DTO for creating a Reading (from sensor/node).
/// Simple API for ESP32: nodeId as String, auto-completes unit from SensorType.
/// </summary>
/// <param name="NodeId">Node identifier (e.g., "wetterstation-garten-01")</param>
/// <param name="Type">Sensor type ID (e.g., "temperature")</param>
/// <param name="Value">Measurement value</param>
/// <param name="HubId">Optional Hub identifier for auto-registration</param>
/// <param name="Timestamp">Optional timestamp (default: now)</param>
public record CreateReadingDto(
    string NodeId,
    string Type,
    double Value,
    string? HubId = null,
    DateTime? Timestamp = null
);

/// <summary>
/// DTO for filtering Readings
/// </summary>
/// <param name="NodeId">Filter by Node (Guid)</param>
/// <param name="NodeIdentifier">Filter by Node identifier (string)</param>
/// <param name="HubId">Filter by Hub (Guid)</param>
/// <param name="SensorTypeId">Filter by sensor type ID</param>
/// <param name="From">Time range start</param>
/// <param name="To">Time range end</param>
/// <param name="IsSyncedToCloud">Filter by sync status</param>
/// <param name="Page">Page number (1-based)</param>
/// <param name="PageSize">Items per page</param>
public record ReadingFilterDto(
    Guid? NodeId = null,
    string? NodeIdentifier = null,
    Guid? HubId = null,
    string? SensorTypeId = null,
    DateTime? From = null,
    DateTime? To = null,
    bool? IsSyncedToCloud = null,
    int Page = 1,
    int PageSize = 50
);
