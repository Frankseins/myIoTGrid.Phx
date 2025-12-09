using myIoTGrid.Shared.Common.Interfaces;

namespace myIoTGrid.Shared.Common.Entities;

/// <summary>
/// Defines a measurement capability of a Sensor.
/// One Sensor can have multiple capabilities (e.g., BME280: temperature, humidity, pressure).
/// Matter-konform: Corresponds to a Matter Cluster.
/// </summary>
public class SensorCapability : IEntity
{
    /// <summary>Primary key</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the parent Sensor</summary>
    public Guid SensorId { get; set; }

    /// <summary>Type of measurement (e.g., "temperature", "humidity", "pressure")</summary>
    public string MeasurementType { get; set; } = string.Empty;

    /// <summary>Display name for UI (e.g., "Temperatur", "Luftfeuchtigkeit")</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Unit of measurement (e.g., "°C", "%", "hPa")</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>Minimum measurable value</summary>
    public double? MinValue { get; set; }

    /// <summary>Maximum measurable value</summary>
    public double? MaxValue { get; set; }

    /// <summary>Measurement resolution (e.g., 0.01)</summary>
    public double Resolution { get; set; } = 0.01;

    /// <summary>Measurement accuracy (e.g., 0.5 for +/-0.5C)</summary>
    public double Accuracy { get; set; } = 0.5;

    /// <summary>Matter Cluster ID (e.g., 1026 for TemperatureMeasurement)</summary>
    public uint? MatterClusterId { get; set; }

    /// <summary>Matter Cluster Name (e.g., "TemperatureMeasurement")</summary>
    public string? MatterClusterName { get; set; }

    /// <summary>Sort order for UI display</summary>
    public int SortOrder { get; set; }

    /// <summary>Is this capability active?</summary>
    public bool IsActive { get; set; } = true;

    // === Navigation Properties ===

    /// <summary>Parent sensor</summary>
    public Sensor Sensor { get; set; } = null!;
}
