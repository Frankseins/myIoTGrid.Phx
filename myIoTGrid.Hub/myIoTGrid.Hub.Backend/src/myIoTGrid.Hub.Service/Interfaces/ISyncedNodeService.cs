using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service Interface for SyncedNode management.
/// Nodes synchronized from Cloud (DirectNode, VirtualNode, OtherHub).
/// </summary>
public interface ISyncedNodeService
{
    /// <summary>Returns all SyncedNodes</summary>
    Task<IEnumerable<SyncedNodeDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns a SyncedNode by ID</summary>
    Task<SyncedNodeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns a SyncedNode by CloudNodeId</summary>
    Task<SyncedNodeDto?> GetByCloudNodeIdAsync(Guid cloudNodeId, CancellationToken ct = default);

    /// <summary>Creates or updates a SyncedNode from Cloud data</summary>
    Task<SyncedNodeDto> UpsertAsync(CreateSyncedNodeDto dto, CancellationToken ct = default);

    /// <summary>Deletes a SyncedNode</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Updates the sync timestamp</summary>
    Task UpdateLastSyncAsync(Guid id, CancellationToken ct = default);
}
