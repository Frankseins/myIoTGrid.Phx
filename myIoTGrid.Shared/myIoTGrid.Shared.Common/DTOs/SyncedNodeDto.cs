using myIoTGrid.Shared.Common.Enums;

namespace myIoTGrid.Shared.Common.DTOs;

/// <summary>
/// DTO for SyncedNode information.
/// Nodes synchronized from Cloud (DirectNode, VirtualNode, OtherHub).
/// </summary>
/// <param name="Id">Primary key</param>
/// <param name="CloudNodeId">ID in the Cloud</param>
/// <param name="NodeId">Device identifier</param>
/// <param name="Name">Display name</param>
/// <param name="Source">Source type: Direct, Virtual, OtherHub</param>
/// <param name="SourceDetails">Source details (e.g., "DWD Köln")</param>
/// <param name="Location">Physical location</param>
/// <param name="IsOnline">Online status</param>
/// <param name="LastSyncAt">Last sync timestamp</param>
/// <param name="CreatedAt">Creation timestamp</param>
public record SyncedNodeDto(
    Guid Id,
    Guid CloudNodeId,
    string NodeId,
    string Name,
    SyncedNodeSourceDto Source,
    string? SourceDetails,
    LocationDto? Location,
    bool IsOnline,
    DateTime LastSyncAt,
    DateTime CreatedAt
);

/// <summary>
/// DTO for creating a SyncedNode (from Cloud sync)
/// </summary>
/// <param name="CloudNodeId">ID in the Cloud</param>
/// <param name="NodeId">Device identifier</param>
/// <param name="Name">Display name</param>
/// <param name="Source">Source type</param>
/// <param name="SourceDetails">Source details</param>
/// <param name="Location">Physical location</param>
/// <param name="IsOnline">Online status</param>
public record CreateSyncedNodeDto(
    Guid CloudNodeId,
    string NodeId,
    string Name,
    SyncedNodeSourceDto Source,
    string? SourceDetails = null,
    LocationDto? Location = null,
    bool IsOnline = false
);
