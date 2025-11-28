using myIoTGrid.Hub.Domain.Interfaces;

namespace myIoTGrid.Hub.Domain.Entities;

/// <summary>
/// Type definition for measurements.
/// Matter-konform: Entspricht einem Matter Cluster.
/// Wird von Grid.Cloud synchronisiert.
/// </summary>
public class SensorType : ISyncableEntity
{
    /// <summary>Primary Key (e.g., "temperature", "humidity")</summary>
    public string TypeId { get; set; } = string.Empty;

    /// <summary>Explicit IEntity.Id implementation for interface compliance</summary>
    Guid IEntity.Id
    {
        get => Guid.Empty; // Not used - TypeId is the primary key
        set { } // No-op
    }

    /// <summary>Display name (e.g., "Temperatur")</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Matter Cluster ID (0x0402 = TemperatureMeasurement)</summary>
    public uint ClusterId { get; set; }

    /// <summary>Matter Cluster Name (e.g., "TemperatureMeasurement")</summary>
    public string? MatterClusterName { get; set; }

    /// <summary>Unit (e.g., "°C", "%", "hPa")</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>Resolution (e.g., 0.1)</summary>
    public double Resolution { get; set; } = 0.1;

    /// <summary>Minimum value</summary>
    public double? MinValue { get; set; }

    /// <summary>Maximum value</summary>
    public double? MaxValue { get; set; }

    /// <summary>Description</summary>
    public string? Description { get; set; }

    /// <summary>Is this a custom myIoTGrid type? (ClusterId >= 0xFC00)</summary>
    public bool IsCustom { get; set; }

    /// <summary>Category (weather, water, air, soil, other)</summary>
    public string Category { get; set; } = "other";

    /// <summary>Material Icon Name for UI</summary>
    public string? Icon { get; set; }

    /// <summary>Hex Color for UI (e.g., "#FF5722")</summary>
    public string? Color { get; set; }

    /// <summary>Whether this type is global (defined by Cloud)</summary>
    public bool IsGlobal { get; set; }

    /// <summary>Creation timestamp</summary>
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
    public ICollection<Reading> Readings { get; set; } = new List<Reading>();
    public ICollection<SyncedReading> SyncedReadings { get; set; } = new List<SyncedReading>();
}
