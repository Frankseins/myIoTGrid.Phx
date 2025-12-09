using myIoTGrid.Shared.Common.DTOs;

namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service Interface for Alert management
/// </summary>
public interface IAlertService
{
    /// <summary>Creates an Alert (received from Cloud)</summary>
    Task<AlertDto> CreateFromCloudAsync(CreateAlertDto dto, CancellationToken ct = default);

    /// <summary>Creates a local Alert</summary>
    Task<AlertDto> CreateLocalAlertAsync(CreateAlertDto dto, CancellationToken ct = default);

    /// <summary>Returns an Alert by ID</summary>
    Task<AlertDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all active Alerts for the current Tenant</summary>
    Task<IEnumerable<AlertDto>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>Returns Alerts filtered and paginated</summary>
    Task<PaginatedResultDto<AlertDto>> GetFilteredAsync(AlertFilterDto filter, CancellationToken ct = default);

    /// <summary>Acknowledges an Alert</summary>
    Task<AlertDto?> AcknowledgeAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates a Node-Offline Alert for a Node</summary>
    Task CreateNodeOfflineAlertAsync(Guid nodeId, CancellationToken ct = default);

    /// <summary>Creates a Hub-Offline Alert for a Hub</summary>
    Task CreateHubOfflineAlertAsync(Guid hubId, CancellationToken ct = default);

    /// <summary>Deactivates all Alerts of a specific type for a Node</summary>
    Task DeactivateNodeAlertsAsync(Guid nodeId, string alertTypeCode, CancellationToken ct = default);

    /// <summary>Deactivates all Alerts of a specific type for a Hub</summary>
    Task DeactivateHubAlertsAsync(Guid hubId, string alertTypeCode, CancellationToken ct = default);
}
