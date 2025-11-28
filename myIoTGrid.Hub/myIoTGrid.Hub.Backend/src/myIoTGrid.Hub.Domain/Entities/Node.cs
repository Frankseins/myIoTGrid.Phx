using myIoTGrid.Hub.Domain.Enums;
using myIoTGrid.Hub.Domain.Interfaces;
using myIoTGrid.Hub.Domain.ValueObjects;

namespace myIoTGrid.Hub.Domain.Entities;

/// <summary>
/// Represents a physical IoT device (ESP32, LoRa32).
/// Matter-konform: Entspricht einem Matter Node.
/// One Hub can manage multiple Nodes.
/// </summary>
public class Node : IEntity
{
    /// <summary>Primary key (internal)</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the Hub managing this node</summary>
    public Guid HubId { get; set; }

    /// <summary>Unique device identifier (e.g., "wetterstation-garten-01")</summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>Display name (e.g., "Wetterstation Garten")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Communication protocol (WLAN, LoRaWAN)</summary>
    public Protocol Protocol { get; set; } = Protocol.WLAN;

    /// <summary>Physical location of the node</summary>
    public Location? Location { get; set; }

    /// <summary>Firmware version (if reported)</summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>Battery level 0-100% (for battery-powered devices)</summary>
    public int? BatteryLevel { get; set; }

    /// <summary>Last contact timestamp</summary>
    public DateTime? LastSeen { get; set; }

    /// <summary>Online status</summary>
    public bool IsOnline { get; set; }

    /// <summary>When the node was first registered</summary>
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public Hub? Hub { get; set; }
    public ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
    public ICollection<Reading> Readings { get; set; } = new List<Reading>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
