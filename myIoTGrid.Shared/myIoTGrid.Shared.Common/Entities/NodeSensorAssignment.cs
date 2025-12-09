using myIoTGrid.Shared.Common.Interfaces;

namespace myIoTGrid.Shared.Common.Entities;

/// <summary>
/// Assignment of a Sensor to a Node with specific pin configuration.
/// This is the "assignment" level - physical hardware binding.
/// Matter-konform: Corresponds to a Matter Endpoint.
/// Can override Sensor defaults (pins, interval).
/// Two-tier model: Sensor -> NodeSensorAssignment
/// </summary>
public class NodeSensorAssignment : IEntity
{
    /// <summary>Primary key</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the Node this sensor is installed on</summary>
    public Guid NodeId { get; set; }

    /// <summary>FK to the Sensor instance</summary>
    public Guid SensorId { get; set; }

    /// <summary>Matter Endpoint ID (1, 2, 3... unique per Node)</summary>
    public int EndpointId { get; set; }

    /// <summary>Optional alias for this assignment (e.g., "Aussentemperatur")</summary>
    public string? Alias { get; set; }

    // === Pin Overrides (null = use Sensor defaults) ===

    /// <summary>Override I2C address</summary>
    public string? I2CAddressOverride { get; set; }

    /// <summary>Override SDA pin</summary>
    public int? SdaPinOverride { get; set; }

    /// <summary>Override SCL pin</summary>
    public int? SclPinOverride { get; set; }

    /// <summary>Override OneWire pin</summary>
    public int? OneWirePinOverride { get; set; }

    /// <summary>Override analog pin</summary>
    public int? AnalogPinOverride { get; set; }

    /// <summary>Override digital pin</summary>
    public int? DigitalPinOverride { get; set; }

    /// <summary>Override trigger pin (ultrasonic)</summary>
    public int? TriggerPinOverride { get; set; }

    /// <summary>Override echo pin (ultrasonic)</summary>
    public int? EchoPinOverride { get; set; }

    /// <summary>Override baud rate for UART sensors</summary>
    public int? BaudRateOverride { get; set; }

    // === Timing Override ===

    /// <summary>Override measurement interval (null = use Sensor default)</summary>
    public int? IntervalSecondsOverride { get; set; }

    // === Status ===

    /// <summary>Is this assignment active?</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Last time this sensor reported data</summary>
    public DateTime? LastSeenAt { get; set; }

    /// <summary>When the sensor was assigned to this node</summary>
    public DateTime AssignedAt { get; set; }

    // === Navigation Properties ===

    /// <summary>Node this sensor is installed on</summary>
    public Node Node { get; set; } = null!;

    /// <summary>Sensor instance</summary>
    public Sensor Sensor { get; set; } = null!;

    /// <summary>Readings from this assignment</summary>
    public ICollection<Reading> Readings { get; set; } = new List<Reading>();
}
