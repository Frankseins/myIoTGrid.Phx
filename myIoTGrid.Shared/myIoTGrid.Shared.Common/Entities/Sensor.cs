using myIoTGrid.Shared.Common.Enums;
using myIoTGrid.Shared.Common.Interfaces;

namespace myIoTGrid.Shared.Common.Entities;

/// <summary>
/// Complete sensor definition with hardware configuration and calibration.
/// This is the unified "sensor" level - contains ALL properties (previously split between SensorType and Sensor).
/// Simplified two-tier model: Sensor -> NodeSensorAssignment
/// </summary>
public class Sensor : ITenantEntity
{
    /// <summary>Primary key</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant ID for multi-tenant support</summary>
    public Guid TenantId { get; set; }

    // === Identification ===

    /// <summary>Unique code (e.g., "bme280-wohnzimmer", "dht22-garten")</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name (e.g., "Klimasensor Wohnzimmer")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description</summary>
    public string? Description { get; set; }

    /// <summary>Serial number of this physical sensor</summary>
    public string? SerialNumber { get; set; }

    // === Hardware Info (previously in SensorType) ===

    /// <summary>Manufacturer (e.g., "Bosch", "Aosong")</summary>
    public string? Manufacturer { get; set; }

    /// <summary>Model name (e.g., "BME280", "DHT22")</summary>
    public string? Model { get; set; }

    /// <summary>Link to datasheet</summary>
    public string? DatasheetUrl { get; set; }

    // === Communication Protocol (previously in SensorType) ===

    /// <summary>How the sensor communicates (I2C, SPI, OneWire, etc.)</summary>
    public CommunicationProtocol Protocol { get; set; }

    // === Pin Configuration ===

    /// <summary>I2C address (e.g., "0x76")</summary>
    public string? I2CAddress { get; set; }

    /// <summary>SDA pin for I2C</summary>
    public int? SdaPin { get; set; }

    /// <summary>SCL pin for I2C</summary>
    public int? SclPin { get; set; }

    /// <summary>OneWire data pin</summary>
    public int? OneWirePin { get; set; }

    /// <summary>Analog input pin</summary>
    public int? AnalogPin { get; set; }

    /// <summary>Digital GPIO pin</summary>
    public int? DigitalPin { get; set; }

    /// <summary>Trigger pin for ultrasonic sensors</summary>
    public int? TriggerPin { get; set; }

    /// <summary>Echo pin for ultrasonic sensors</summary>
    public int? EchoPin { get; set; }

    // === UART Configuration ===

    /// <summary>Baud rate for UART communication (e.g., 9600, 115200)</summary>
    public int? BaudRate { get; set; }

    // === Timing Configuration (previously in SensorType) ===

    /// <summary>Measurement interval in seconds</summary>
    public int IntervalSeconds { get; set; } = 60;

    /// <summary>Minimum allowed interval in seconds</summary>
    public int MinIntervalSeconds { get; set; } = 1;

    /// <summary>Warmup time in milliseconds before first reading</summary>
    public int WarmupTimeMs { get; set; }

    // === Calibration ===

    /// <summary>Offset correction applied to raw values</summary>
    public double OffsetCorrection { get; set; }

    /// <summary>Gain/multiplier correction applied to raw values</summary>
    public double GainCorrection { get; set; } = 1.0;

    /// <summary>When this sensor was last calibrated</summary>
    public DateTime? LastCalibratedAt { get; set; }

    /// <summary>Notes about the calibration process</summary>
    public string? CalibrationNotes { get; set; }

    /// <summary>When the next calibration is due</summary>
    public DateTime? CalibrationDueAt { get; set; }

    // === Categorization (previously in SensorType) ===

    /// <summary>Category (climate, water, air, soil, location, custom)</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Material icon name for UI</summary>
    public string? Icon { get; set; }

    /// <summary>Hex color for UI (e.g., "#FF5722")</summary>
    public string? Color { get; set; }

    // === Status ===

    /// <summary>Is this sensor active?</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Creation timestamp</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last update timestamp</summary>
    public DateTime UpdatedAt { get; set; }

    // === Navigation Properties ===

    /// <summary>Tenant this sensor belongs to</summary>
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Capabilities (measurement types this sensor can provide)</summary>
    public ICollection<SensorCapability> Capabilities { get; set; } = new List<SensorCapability>();

    /// <summary>Node assignments (where this sensor is installed)</summary>
    public ICollection<NodeSensorAssignment> NodeAssignments { get; set; } = new List<NodeSensorAssignment>();
}
