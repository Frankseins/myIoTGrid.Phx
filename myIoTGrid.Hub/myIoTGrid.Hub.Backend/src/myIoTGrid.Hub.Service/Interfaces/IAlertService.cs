using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Interfaces;

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

    /// <summary>Creates a Hub-Offline Alert for a Sensor</summary>
    Task CreateHubOfflineAlertAsync(Guid sensorId, CancellationToken ct = default);

    /// <summary>Deactivates all Alerts of a specific type for a Sensor</summary>
    Task DeactivateAlertsAsync(Guid sensorId, string alertTypeCode, CancellationToken ct = default);

    /// <summary>Deactivates all Alerts of a specific type for a Hub</summary>
    Task DeactivateHubAlertsAsync(Guid hubId, string alertTypeCode, CancellationToken ct = default);
}
