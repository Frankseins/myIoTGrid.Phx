using myIoTGrid.Shared.Common.Enums;

namespace myIoTGrid.Shared.Common.DTOs;

/// <summary>
/// Unified view of all nodes (Local + Direct + Virtual + OtherHub).
/// Combines local nodes and synced nodes into a single view.
/// </summary>
/// <param name="Id">Unique identifier (local ID or synced ID)</param>
/// <param name="NodeId">Device identifier</param>
/// <param name="Name">Display name</param>
/// <param name="Source">Source of the node</param>
/// <param name="SourceDetails">Additional source details</param>
/// <param name="Sensors">List of sensors (only for local nodes)</param>
/// <param name="Location">Physical location</param>
/// <param name="IsOnline">Online status</param>
/// <param name="LastSeen">Last contact/sync timestamp</param>
/// <param name="LatestReadings">Latest readings from this node</param>
public record UnifiedNodeDto(
    Guid Id,
    string NodeId,
    string Name,
    UnifiedNodeSourceDto Source,
    string? SourceDetails,
    IEnumerable<SensorDto>? Sensors,
    LocationDto? Location,
    bool IsOnline,
    DateTime? LastSeen,
    IEnumerable<UnifiedReadingDto>? LatestReadings
);

/// <summary>
/// Unified reading from any source (local or synced)
/// </summary>
/// <param name="SensorTypeId">Sensor type ID</param>
/// <param name="SensorTypeName">Sensor type display name</param>
/// <param name="Value">Measurement value</param>
/// <param name="Unit">Unit of measurement</param>
/// <param name="Timestamp">Timestamp of measurement</param>
/// <param name="Source">Source of the reading</param>
public record UnifiedReadingDto(
    string SensorTypeId,
    string SensorTypeName,
    double Value,
    string Unit,
    DateTime Timestamp,
    UnifiedNodeSourceDto Source
);
