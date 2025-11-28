using myIoTGrid.Hub.Domain.Interfaces;

namespace myIoTGrid.Hub.Domain.Entities;

/// <summary>
/// Represents a physical sensor chip on a Node (DHT22, BME280, SCD40).
/// Matter-konform: Entspricht einem Matter Endpoint.
/// One Node can have multiple Sensors.
/// </summary>
public class Sensor : IEntity
{
    /// <summary>Primary key (internal)</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the Node this sensor belongs to</summary>
    public Guid NodeId { get; set; }

    /// <summary>FK to the SensorType (e.g., "temperature")</summary>
    public string SensorTypeId { get; set; } = string.Empty;

    /// <summary>Matter Endpoint ID (1, 2, 3...)</summary>
    public int EndpointId { get; set; }

    /// <summary>Optional display name for this sensor</summary>
    public string? Name { get; set; }

    /// <summary>Is this sensor active?</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>When the sensor was registered</summary>
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public Node? Node { get; set; }
    public SensorType? SensorType { get; set; }
}
