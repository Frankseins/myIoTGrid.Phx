using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service Interface for unified Node view.
/// Combines local Nodes and synced Nodes into a single view.
/// </summary>
public interface IUnifiedNodeService
{
    /// <summary>Returns all Nodes (local + synced) as unified view</summary>
    Task<IEnumerable<UnifiedNodeDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns all Nodes for a Hub (local + synced)</summary>
    Task<IEnumerable<UnifiedNodeDto>> GetByHubAsync(Guid hubId, CancellationToken ct = default);

    /// <summary>Returns a unified Node by ID (checks both local and synced)</summary>
    Task<UnifiedNodeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns latest readings for all Nodes (local + synced)</summary>
    Task<IEnumerable<UnifiedReadingDto>> GetLatestReadingsAsync(CancellationToken ct = default);

    /// <summary>Returns latest readings for a specific Node (local or synced)</summary>
    Task<IEnumerable<UnifiedReadingDto>> GetLatestReadingsByNodeAsync(Guid nodeId, CancellationToken ct = default);
}
