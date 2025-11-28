using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service Interface for SyncedReading management.
/// Readings synchronized from Cloud.
/// </summary>
public interface ISyncedReadingService
{
    /// <summary>Returns SyncedReadings for a SyncedNode</summary>
    Task<IEnumerable<SyncedReadingDto>> GetBySyncedNodeAsync(Guid syncedNodeId, CancellationToken ct = default);

    /// <summary>Returns a SyncedReading by ID</summary>
    Task<SyncedReadingDto?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>Creates a new SyncedReading</summary>
    Task<SyncedReadingDto> CreateAsync(CreateSyncedReadingDto dto, CancellationToken ct = default);

    /// <summary>Creates multiple SyncedReadings (batch from Cloud sync)</summary>
    Task<IEnumerable<SyncedReadingDto>> CreateBatchAsync(IEnumerable<CreateSyncedReadingDto> dtos, CancellationToken ct = default);

    /// <summary>Returns the latest SyncedReadings per SyncedNode</summary>
    Task<IEnumerable<SyncedReadingDto>> GetLatestBySyncedNodeAsync(Guid syncedNodeId, CancellationToken ct = default);

    /// <summary>Deletes old SyncedReadings (retention policy)</summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken ct = default);
}
