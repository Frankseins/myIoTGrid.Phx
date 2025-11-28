namespace myIoTGrid.Hub.Domain.Entities;

/// <summary>
/// Single measurement value from a Node.
/// Matter-konform: Entspricht einem Attribute Report.
/// Uses long Id for high-performance time-series storage (no ITenantEntity interface due to different PK type).
/// </summary>
public class Reading
{
    /// <summary>Primary key (auto-increment for performance)</summary>
    public long Id { get; set; }

    /// <summary>Tenant-ID for Multi-Tenant Support</summary>
    public Guid TenantId { get; set; }

    /// <summary>FK to the Node</summary>
    public Guid NodeId { get; set; }

    /// <summary>FK to the SensorType (e.g., "temperature")</summary>
    public string SensorTypeId { get; set; } = string.Empty;

    /// <summary>Measurement value</summary>
    public double Value { get; set; }

    /// <summary>UTC timestamp of the measurement</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Whether this reading was synced to cloud</summary>
    public bool IsSyncedToCloud { get; set; }

    // Navigation Properties
    public Node? Node { get; set; }
    public SensorType? SensorType { get; set; }
}
