namespace myIoTGrid.Shared.Common.Entities;

/// <summary>
/// Single measurement value from a NodeSensorAssignment.
/// Matter-konform: Corresponds to an Attribute Report.
/// Uses long Id for high-performance time-series storage.
/// Stores both raw and calibrated values.
/// </summary>
public class Reading
{
    /// <summary>Primary key (auto-increment for performance)</summary>
    public long Id { get; set; }

    /// <summary>Tenant ID for multi-tenant support</summary>
    public Guid TenantId { get; set; }

    /// <summary>FK to the Node</summary>
    public Guid NodeId { get; set; }

    /// <summary>FK to the NodeSensorAssignment (nullable for direct sensor readings)</summary>
    public Guid? AssignmentId { get; set; }

    /// <summary>Type of measurement (e.g., "temperature", "humidity")</summary>
    public string MeasurementType { get; set; } = string.Empty;

    /// <summary>Raw value from sensor (before calibration)</summary>
    public double RawValue { get; set; }

    /// <summary>Calibrated measurement value</summary>
    public double Value { get; set; }

    /// <summary>Unit of measurement (e.g., "°C", "%")</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the measurement</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Whether this reading was synced to cloud</summary>
    public bool IsSyncedToCloud { get; set; }

    // === Navigation Properties ===

    /// <summary>Node that recorded this reading</summary>
    public Node? Node { get; set; }

    /// <summary>Sensor assignment that produced this reading</summary>
    public NodeSensorAssignment? Assignment { get; set; }
}
