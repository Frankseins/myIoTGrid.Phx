using myIoTGrid.Shared.Common.DTOs;
using myIoTGrid.Shared.Common.DTOs.Common;

namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service Interface for Reading (Measurement) management.
/// Matter-konform: Entspricht Attribute Reports.
/// </summary>
public interface IReadingService
{
    /// <summary>Creates a new Reading</summary>
    Task<ReadingDto> CreateAsync(CreateReadingDto dto, CancellationToken ct = default);

    /// <summary>Creates a new Reading from sensor device (firmware format)</summary>
    Task<ReadingDto> CreateFromSensorAsync(CreateSensorReadingDto dto, CancellationToken ct = default);

    /// <summary>Creates multiple Readings in batch (Sprint OS-01: Offline Storage sync)</summary>
    Task<BatchReadingsResultDto> CreateBatchAsync(CreateBatchReadingsDto dto, CancellationToken ct = default);

    /// <summary>Returns a Reading by ID</summary>
    Task<ReadingDto?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>Returns Readings for a Node</summary>
    Task<IEnumerable<ReadingDto>> GetByNodeAsync(Guid nodeId, ReadingFilterDto? filter = null, CancellationToken ct = default);

    /// <summary>Returns Readings filtered and paginated (legacy)</summary>
    Task<PaginatedResultDto<ReadingDto>> GetFilteredAsync(ReadingFilterDto filter, CancellationToken ct = default);

    /// <summary>Returns Readings with paging, sorting, and filtering</summary>
    Task<PagedResultDto<ReadingDto>> GetPagedAsync(QueryParamsDto queryParams, CancellationToken ct = default);

    /// <summary>Returns the latest Readings per Node</summary>
    Task<IEnumerable<ReadingDto>> GetLatestByNodeAsync(Guid nodeId, CancellationToken ct = default);

    /// <summary>Returns the latest Readings of all Nodes</summary>
    Task<IEnumerable<ReadingDto>> GetLatestAsync(CancellationToken ct = default);

    /// <summary>Returns unsynced Readings (for cloud sync)</summary>
    Task<IEnumerable<ReadingDto>> GetUnsyncedAsync(int limit = 100, CancellationToken ct = default);

    /// <summary>Marks Readings as synced to cloud</summary>
    Task MarkAsSyncedAsync(IEnumerable<long> ids, CancellationToken ct = default);

    /// <summary>Deletes Readings within a date range, optionally filtered by sensor and measurement type</summary>
    Task<DeleteReadingsResultDto> DeleteRangeAsync(DeleteReadingsRangeDto dto, CancellationToken ct = default);
}
