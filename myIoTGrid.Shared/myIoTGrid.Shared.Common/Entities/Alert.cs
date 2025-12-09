using myIoTGrid.Shared.Common.Enums;
using myIoTGrid.Shared.Common.Interfaces;

namespace myIoTGrid.Shared.Common.Entities;

/// <summary>
/// Warning/Alert (from AI analysis in cloud or local rules)
/// </summary>
public class Alert : ITenantEntity
{
    /// <summary>Primary key</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant-ID for Multi-Tenant Support</summary>
    public Guid TenantId { get; set; }

    /// <summary>Optional reference to affected Hub</summary>
    public Guid? HubId { get; set; }

    /// <summary>Optional reference to affected Node (ESP32/LoRa32 Device)</summary>
    public Guid? NodeId { get; set; }

    /// <summary>Reference to Alert Type</summary>
    public Guid AlertTypeId { get; set; }

    /// <summary>Alert level</summary>
    public AlertLevel Level { get; set; }

    /// <summary>Alert message</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Recommendation for resolution</summary>
    public string? Recommendation { get; set; }

    /// <summary>Source of the alert (local or Cloud)</summary>
    public AlertSource Source { get; set; }

    /// <summary>Creation timestamp</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Expiration timestamp (optional)</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Acknowledgment timestamp</summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>Whether the alert is still active</summary>
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public Tenant? Tenant { get; set; }
    public Hub? Hub { get; set; }
    public Node? Node { get; set; }
    public AlertType? AlertType { get; set; }
}
