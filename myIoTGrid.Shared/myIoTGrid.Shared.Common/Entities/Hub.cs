using myIoTGrid.Shared.Common.Interfaces;

namespace myIoTGrid.Shared.Common.Entities;

/// <summary>
/// Represents a Raspberry Pi Hub that manages multiple sensors.
/// One Tenant can have multiple Hubs.
/// </summary>
public class Hub : ITenantEntity
{
    /// <summary>Primary key (internal)</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant-ID for Multi-Tenant Support</summary>
    public Guid TenantId { get; set; }

    /// <summary>Unique identifier for the hub (e.g., "hub-home-01")</summary>
    public string HubId { get; set; } = string.Empty;

    /// <summary>Display name (e.g., "Zuhause", "Office")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description</summary>
    public string? Description { get; set; }

    /// <summary>Last heartbeat from the hub</summary>
    public DateTime? LastSeen { get; set; }

    /// <summary>Current online status</summary>
    public bool IsOnline { get; set; }

    /// <summary>When the hub was registered</summary>
    public DateTime CreatedAt { get; set; }

    // Default Provisioning Settings (for BLE setup of new nodes)
    /// <summary>Default WiFi SSID for new node provisioning</summary>
    public string? DefaultWifiSsid { get; set; }

    /// <summary>Default WiFi Password for new node provisioning (stored encrypted)</summary>
    public string? DefaultWifiPassword { get; set; }

    /// <summary>API URL for nodes to connect to (e.g., "http://192.168.1.100:5002")</summary>
    public string? ApiUrl { get; set; }

    /// <summary>API Port (default 5002 for HTTP, 5001 for HTTPS)</summary>
    public int ApiPort { get; set; } = 5002;

    // Navigation Properties
    public Tenant? Tenant { get; set; }
    public ICollection<Node> Nodes { get; set; } = new List<Node>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
