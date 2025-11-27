using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service Interface for Hub (Raspberry Pi Gateway) management
/// </summary>
public interface IHubService
{
    /// <summary>Returns all Hubs for the current Tenant</summary>
    Task<IEnumerable<HubDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns a Hub by ID</summary>
    Task<HubDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns a Hub by Hub identifier string</summary>
    Task<HubDto?> GetByHubIdAsync(string hubId, CancellationToken ct = default);

    /// <summary>Finds or creates a Hub by Hub identifier</summary>
    Task<HubDto> GetOrCreateByHubIdAsync(string hubId, CancellationToken ct = default);

    /// <summary>Creates a new Hub</summary>
    Task<HubDto> CreateAsync(CreateHubDto dto, CancellationToken ct = default);

    /// <summary>Updates a Hub</summary>
    Task<HubDto?> UpdateAsync(Guid id, UpdateHubDto dto, CancellationToken ct = default);

    /// <summary>Updates the LastSeen timestamp</summary>
    Task UpdateLastSeenAsync(Guid id, CancellationToken ct = default);

    /// <summary>Sets the online status</summary>
    Task SetOnlineStatusAsync(Guid id, bool isOnline, CancellationToken ct = default);
}
