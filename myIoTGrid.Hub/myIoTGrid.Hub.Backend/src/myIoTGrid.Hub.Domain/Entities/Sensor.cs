using myIoTGrid.Hub.Domain.Enums;
using myIoTGrid.Hub.Domain.Interfaces;
using myIoTGrid.Hub.Domain.ValueObjects;

namespace myIoTGrid.Hub.Domain.Entities;

/// <summary>
/// Represents a physical sensor device (ESP32, LoRa32).
/// One Hub can manage multiple Sensors.
/// </summary>
public class Sensor : IEntity
{
    /// <summary>Primary key (internal)</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the Hub managing this sensor</summary>
    public Guid HubId { get; set; }

    /// <summary>Unique identifier for the sensor (e.g., "sensor-wohnzimmer-01")</summary>
    public string SensorId { get; set; } = string.Empty;

    /// <summary>Display name (e.g., "Wohnzimmer Sensor")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Communication protocol (WLAN, LoRaWAN)</summary>
    public Protocol Protocol { get; set; } = Protocol.WLAN;

    /// <summary>Physical location of the sensor</summary>
    public Location? Location { get; set; }

    /// <summary>
    /// List of sensor types this device can measure
    /// e.g., ["temperature", "humidity", "pressure"]
    /// </summary>
    public List<string> SensorTypes { get; set; } = new();

    /// <summary>Last data received from sensor</summary>
    public DateTime? LastSeen { get; set; }

    /// <summary>Current online status</summary>
    public bool IsOnline { get; set; }

    /// <summary>Firmware version (if reported)</summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>Battery level in percent (for battery-powered sensors)</summary>
    public int? BatteryLevel { get; set; }

    /// <summary>When the sensor was first registered</summary>
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public Hub? Hub { get; set; }
    public ICollection<SensorData> SensorData { get; set; } = new List<SensorData>();
}
