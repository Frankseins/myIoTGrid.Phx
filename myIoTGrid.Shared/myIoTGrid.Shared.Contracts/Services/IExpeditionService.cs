using myIoTGrid.Shared.Common.DTOs;

namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service Interface for Expedition (GPS tracking session) management.
/// Allows users to save, organize and analyze their GPS routes.
/// </summary>
public interface IExpeditionService
{
    /// <summary>Creates a new Expedition</summary>
    Task<ExpeditionDto> CreateAsync(CreateExpeditionDto dto, CancellationToken ct = default);

    /// <summary>Returns an Expedition by ID</summary>
    Task<ExpeditionDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all Expeditions with optional filters</summary>
    Task<List<ExpeditionDto>> GetAllAsync(ExpeditionFilterDto? filter = null, CancellationToken ct = default);

    /// <summary>Returns Expeditions for a specific Node</summary>
    Task<List<ExpeditionDto>> GetByNodeAsync(Guid nodeId, CancellationToken ct = default);

    /// <summary>Updates an Expedition</summary>
    Task<ExpeditionDto?> UpdateAsync(Guid id, UpdateExpeditionDto dto, CancellationToken ct = default);

    /// <summary>Deletes an Expedition</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Calculates and returns detailed statistics for an Expedition</summary>
    Task<ExpeditionStatsDto?> GetStatisticsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Recalculates statistics for an Expedition (after GPS data changes)</summary>
    Task<ExpeditionDto?> RecalculateStatisticsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Updates the status of an Expedition</summary>
    Task<ExpeditionDto?> UpdateStatusAsync(Guid id, Common.Enums.ExpeditionStatusDto status, CancellationToken ct = default);

    /// <summary>Returns GPS data (points with measurements) for an Expedition</summary>
    Task<ExpeditionGpsDataDto?> GetGpsDataAsync(Guid id, CancellationToken ct = default);
}
