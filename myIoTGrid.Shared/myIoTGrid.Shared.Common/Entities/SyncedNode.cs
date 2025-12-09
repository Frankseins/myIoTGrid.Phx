using myIoTGrid.Shared.Common.Enums;
using myIoTGrid.Shared.Common.Interfaces;
using myIoTGrid.Shared.Common.ValueObjects;

namespace myIoTGrid.Shared.Common.Entities;

/// <summary>
/// Node synchronized from Cloud (DirectNode, VirtualNode, OtherHub).
/// These are nodes that are not directly connected to this Hub.
/// </summary>
public class SyncedNode : IEntity
{
    /// <summary>Primary key (internal)</summary>
    public Guid Id { get; set; }

    /// <summary>ID in the Cloud</summary>
    public Guid CloudNodeId { get; set; }

    /// <summary>Device identifier</summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>Display name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Source type: Direct, Virtual, OtherHub</summary>
    public SyncedNodeSource Source { get; set; }

    /// <summary>Source details (e.g., "DWD Köln", "Hub: Office")</summary>
    public string? SourceDetails { get; set; }

    /// <summary>Physical location</summary>
    public Location? Location { get; set; }

    /// <summary>Online status</summary>
    public bool IsOnline { get; set; }

    /// <summary>Last sync timestamp</summary>
    public DateTime LastSyncAt { get; set; }

    /// <summary>When this synced node was first created locally</summary>
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public ICollection<SyncedReading> SyncedReadings { get; set; } = new List<SyncedReading>();
}
