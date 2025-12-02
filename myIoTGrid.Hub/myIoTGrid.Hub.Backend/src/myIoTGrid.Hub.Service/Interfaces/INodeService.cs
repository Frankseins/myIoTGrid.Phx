using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.DTOs.Common;

namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service Interface for Node (ESP32/LoRa32 Device) management.
/// Matter-konform: Entspricht einem Matter Node.
/// </summary>
public interface INodeService
{
    /// <summary>Returns all Nodes</summary>
    Task<IEnumerable<NodeDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns all Nodes for a Hub</summary>
    Task<IEnumerable<NodeDto>> GetByHubAsync(Guid hubId, CancellationToken ct = default);

    /// <summary>Returns Nodes with paging, sorting, and filtering</summary>
    Task<PagedResultDto<NodeDto>> GetPagedAsync(QueryParamsDto queryParams, CancellationToken ct = default);

    /// <summary>Returns a Node by ID</summary>
    Task<NodeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns a Node by Node identifier string within a Hub</summary>
    Task<NodeDto?> GetByNodeIdAsync(Guid hubId, string nodeId, CancellationToken ct = default);

    /// <summary>Finds or creates a Node (auto-registration)</summary>
    Task<NodeDto> GetOrCreateByNodeIdAsync(Guid hubId, string nodeId, CancellationToken ct = default);

    /// <summary>Creates a new Node</summary>
    Task<NodeDto> CreateAsync(CreateNodeDto dto, CancellationToken ct = default);

    /// <summary>Updates a Node</summary>
    Task<NodeDto?> UpdateAsync(Guid id, UpdateNodeDto dto, CancellationToken ct = default);

    /// <summary>Updates the LastSeen timestamp</summary>
    Task UpdateLastSeenAsync(Guid id, CancellationToken ct = default);

    /// <summary>Sets the online status</summary>
    Task SetOnlineStatusAsync(Guid id, bool isOnline, CancellationToken ct = default);

    /// <summary>Updates the Node status (online, lastSeen, battery)</summary>
    Task UpdateStatusAsync(Guid id, NodeStatusDto status, CancellationToken ct = default);

    /// <summary>Auto-Register: Creates node if not exists, updates if exists</summary>
    Task<NodeDto> RegisterOrUpdateAsync(CreateNodeDto dto, CancellationToken ct = default);

    /// <summary>Auto-Register with status: Creates node if not exists, updates if exists. Returns (NodeDto, isNew)</summary>
    Task<(NodeDto Node, bool IsNew)> RegisterOrUpdateWithStatusAsync(CreateNodeDto dto, string? firmwareVersion = null, CancellationToken ct = default);

    /// <summary>Deletes a Node</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
