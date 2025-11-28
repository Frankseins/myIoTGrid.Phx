using myIoTGrid.Hub.Domain.Interfaces;

namespace myIoTGrid.Hub.Domain.Entities;

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

    // Navigation Properties
    public Tenant? Tenant { get; set; }
    public ICollection<Node> Nodes { get; set; } = new List<Node>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
