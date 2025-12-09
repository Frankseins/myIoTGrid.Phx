using myIoTGrid.Shared.Common.Interfaces;

namespace myIoTGrid.Shared.Common.Entities;

/// <summary>
/// Tenant for Multi-Tenant Support
/// </summary>
public class Tenant : IEntity
{
    /// <summary>Primary key</summary>
    public Guid Id { get; set; }

    /// <summary>Name of the Tenant</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>API-Key for Cloud synchronization</summary>
    public string? CloudApiKey { get; set; }

    /// <summary>Creation timestamp</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last Cloud sync</summary>
    public DateTime? LastSyncAt { get; set; }

    /// <summary>Whether the Tenant is active</summary>
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public ICollection<Hub> Hubs { get; set; } = new List<Hub>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
