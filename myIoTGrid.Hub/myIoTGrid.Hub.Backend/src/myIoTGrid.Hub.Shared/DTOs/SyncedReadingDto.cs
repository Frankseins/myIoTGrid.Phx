namespace myIoTGrid.Hub.Shared.DTOs;

/// <summary>
/// DTO for SyncedReading information.
/// Readings synchronized from Cloud.
/// </summary>
/// <param name="Id">Primary key</param>
/// <param name="SyncedNodeId">FK to SyncedNode</param>
/// <param name="SensorTypeId">Sensor type ID (e.g., "temperature")</param>
/// <param name="SensorTypeName">Sensor type display name</param>
/// <param name="Value">Measurement value</param>
/// <param name="Unit">Unit of measurement</param>
/// <param name="Timestamp">Original timestamp of the measurement</param>
/// <param name="SyncedAt">When synced to this Hub</param>
public record SyncedReadingDto(
    long Id,
    Guid SyncedNodeId,
    string SensorTypeId,
    string SensorTypeName,
    double Value,
    string Unit,
    DateTime Timestamp,
    DateTime SyncedAt
);

/// <summary>
/// DTO for creating a SyncedReading (from Cloud sync)
/// </summary>
/// <param name="SyncedNodeId">FK to SyncedNode</param>
/// <param name="SensorTypeId">Sensor type ID</param>
/// <param name="Value">Measurement value</param>
/// <param name="Timestamp">Original timestamp</param>
public record CreateSyncedReadingDto(
    Guid SyncedNodeId,
    string SensorTypeId,
    double Value,
    DateTime Timestamp
);
