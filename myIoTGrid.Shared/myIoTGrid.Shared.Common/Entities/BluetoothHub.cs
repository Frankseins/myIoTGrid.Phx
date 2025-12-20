using myIoTGrid.Shared.Common.Interfaces;

namespace myIoTGrid.Shared.Common.Entities;

/// <summary>
/// Represents a Bluetooth Hub Gateway (e.g., Raspberry Pi with BLE adapter).
/// Receives sensor data from ESP32 devices via Bluetooth Low Energy
/// and forwards it to the main Hub API.
/// </summary>
public class BluetoothHub : IEntity
{
    /// <summary>Primary key (internal)</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the parent Hub (Raspberry Pi Gateway)</summary>
    public Guid HubId { get; set; }

    /// <summary>Display name (e.g., "Bluetooth Gateway Pi 5")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>MAC address of the Bluetooth adapter</summary>
    public string? MacAddress { get; set; }

    /// <summary>Current status: Active, Inactive, Error</summary>
    public string Status { get; set; } = "Inactive";

    /// <summary>Last contact timestamp</summary>
    public DateTime? LastSeen { get; set; }

    /// <summary>When the Bluetooth Hub was registered</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the Bluetooth Hub was last updated</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // === Navigation Properties ===

    /// <summary>Parent Hub (Raspberry Pi Gateway)</summary>
    public Hub? Hub { get; set; }

    /// <summary>Nodes connected via this Bluetooth Hub</summary>
    public ICollection<Node> Nodes { get; set; } = new List<Node>();
}
