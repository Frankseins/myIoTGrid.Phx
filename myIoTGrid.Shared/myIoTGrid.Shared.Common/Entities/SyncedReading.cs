namespace myIoTGrid.Shared.Common.Entities;

/// <summary>
/// Reading synchronized from Cloud (v3.0).
/// These are measurements from nodes not directly connected to this Hub.
/// Uses long Id for high-performance time-series storage (no IEntity interface due to different PK type).
/// </summary>
public class SyncedReading
{
    /// <summary>Primary key (auto-increment for performance)</summary>
    public long Id { get; set; }

    /// <summary>FK to SyncedNode</summary>
    public Guid SyncedNodeId { get; set; }

    /// <summary>Sensor code (e.g., "dht22", "bme280")</summary>
    public string SensorCode { get; set; } = string.Empty;

    /// <summary>Measurement type (e.g., "temperature", "humidity")</summary>
    public string MeasurementType { get; set; } = string.Empty;

    /// <summary>Measurement value</summary>
    public double Value { get; set; }

    /// <summary>Unit of measurement (e.g., "°C", "%")</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>Original timestamp of the measurement</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>When this reading was synced to this Hub</summary>
    public DateTime SyncedAt { get; set; }

    // Navigation Properties
    public SyncedNode? SyncedNode { get; set; }
}
