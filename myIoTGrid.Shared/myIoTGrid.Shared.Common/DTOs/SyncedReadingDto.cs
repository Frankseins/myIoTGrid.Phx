namespace myIoTGrid.Shared.Common.DTOs;

/// <summary>
/// DTO for SyncedReading information (v3.0).
/// Readings synchronized from Cloud.
/// </summary>
public record SyncedReadingDto(
    long Id,
    Guid SyncedNodeId,
    string SensorCode,
    string MeasurementType,
    double Value,
    string Unit,
    DateTime Timestamp,
    DateTime SyncedAt
);

/// <summary>
/// DTO for creating a SyncedReading (from Cloud sync)
/// </summary>
public record CreateSyncedReadingDto(
    Guid SyncedNodeId,
    string SensorCode,
    string MeasurementType,
    double Value,
    string Unit,
    DateTime Timestamp
);
