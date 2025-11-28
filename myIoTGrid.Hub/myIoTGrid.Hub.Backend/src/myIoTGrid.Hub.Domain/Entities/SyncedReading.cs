namespace myIoTGrid.Hub.Domain.Entities;

/// <summary>
/// Reading synchronized from Cloud.
/// These are measurements from nodes not directly connected to this Hub.
/// Uses long Id for high-performance time-series storage (no IEntity interface due to different PK type).
/// </summary>
public class SyncedReading
{
    /// <summary>Primary key (auto-increment for performance)</summary>
    public long Id { get; set; }

    /// <summary>FK to SyncedNode</summary>
    public Guid SyncedNodeId { get; set; }

    /// <summary>FK to SensorType (e.g., "temperature")</summary>
    public string SensorTypeId { get; set; } = string.Empty;

    /// <summary>Measurement value</summary>
    public double Value { get; set; }

    /// <summary>Original timestamp of the measurement</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>When this reading was synced to this Hub</summary>
    public DateTime SyncedAt { get; set; }

    // Navigation Properties
    public SyncedNode? SyncedNode { get; set; }
    public SensorType? SensorType { get; set; }
}
