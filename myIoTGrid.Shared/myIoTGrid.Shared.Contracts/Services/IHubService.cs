using myIoTGrid.Shared.Common.DTOs;

namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service Interface for Hub (Raspberry Pi Gateway) management.
/// Single-Hub-Architecture: Only one Hub per Tenant/Installation allowed.
/// </summary>
public interface IHubService
{
    // === Single-Hub API (New) ===

    /// <summary>Returns the current Hub (Single-Hub-Architecture)</summary>
    Task<HubDto> GetCurrentHubAsync(CancellationToken ct = default);

    /// <summary>Updates the current Hub (Single-Hub-Architecture)</summary>
    Task<HubDto> UpdateCurrentHubAsync(UpdateHubDto dto, CancellationToken ct = default);

    /// <summary>Returns the current Hub status</summary>
    Task<HubStatusDto> GetStatusAsync(CancellationToken ct = default);

    /// <summary>Ensures the default Hub exists (called during startup)</summary>
    Task EnsureDefaultHubAsync(CancellationToken ct = default);

    /// <summary>Returns the default Hub for sensor registration</summary>
    Task<HubDto> GetDefaultHubAsync(CancellationToken ct = default);

    /// <summary>Returns the provisioning settings for new nodes (WiFi, API URL)</summary>
    Task<HubProvisioningSettingsDto> GetProvisioningSettingsAsync(CancellationToken ct = default);

    // === Legacy API (for internal use) ===

    /// <summary>Returns a Hub by ID</summary>
    Task<HubDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns a Hub by Hub identifier string</summary>
    Task<HubDto?> GetByHubIdAsync(string hubId, CancellationToken ct = default);

    /// <summary>Finds or creates a Hub by Hub identifier</summary>
    Task<HubDto> GetOrCreateByHubIdAsync(string hubId, CancellationToken ct = default);

    /// <summary>Updates a Hub by ID (internal)</summary>
    Task<HubDto?> UpdateAsync(Guid id, UpdateHubDto dto, CancellationToken ct = default);

    /// <summary>Updates the LastSeen timestamp</summary>
    Task UpdateLastSeenAsync(Guid id, CancellationToken ct = default);

    /// <summary>Sets the online status</summary>
    Task SetOnlineStatusAsync(Guid id, bool isOnline, CancellationToken ct = default);
}
