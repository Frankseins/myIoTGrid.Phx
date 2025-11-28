using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service Interface for Reading (Measurement) management.
/// Matter-konform: Entspricht Attribute Reports.
/// </summary>
public interface IReadingService
{
    /// <summary>Creates a new Reading</summary>
    Task<ReadingDto> CreateAsync(CreateReadingDto dto, CancellationToken ct = default);

    /// <summary>Returns a Reading by ID</summary>
    Task<ReadingDto?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>Returns Readings for a Node</summary>
    Task<IEnumerable<ReadingDto>> GetByNodeAsync(Guid nodeId, ReadingFilterDto? filter = null, CancellationToken ct = default);

    /// <summary>Returns Readings filtered and paginated</summary>
    Task<PaginatedResultDto<ReadingDto>> GetFilteredAsync(ReadingFilterDto filter, CancellationToken ct = default);

    /// <summary>Returns the latest Readings per Node</summary>
    Task<IEnumerable<ReadingDto>> GetLatestByNodeAsync(Guid nodeId, CancellationToken ct = default);

    /// <summary>Returns the latest Readings of all Nodes</summary>
    Task<IEnumerable<ReadingDto>> GetLatestAsync(CancellationToken ct = default);

    /// <summary>Returns unsynced Readings (for cloud sync)</summary>
    Task<IEnumerable<ReadingDto>> GetUnsyncedAsync(int limit = 100, CancellationToken ct = default);

    /// <summary>Marks Readings as synced to cloud</summary>
    Task MarkAsSyncedAsync(IEnumerable<long> ids, CancellationToken ct = default);
}
