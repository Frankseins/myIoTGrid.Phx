using myIoTGrid.Shared.Common.DTOs;
using myIoTGrid.Shared.Common.DTOs.Common;

namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service for Node Debug Log management (Sprint 8: Remote Debug System).
/// </summary>
public interface INodeDebugLogService
{
    Task<PaginatedResultDto<NodeDebugLogDto>> GetLogsAsync(DebugLogFilterDto filter, CancellationToken ct = default);
    Task<IEnumerable<NodeDebugLogDto>> GetRecentLogsAsync(Guid nodeId, int count = 50, CancellationToken ct = default);
    Task<int> CreateBatchAsync(string serialNumber, IEnumerable<CreateNodeDebugLogDto> logs, CancellationToken ct = default);
    Task<NodeDebugConfigurationDto?> GetDebugConfigurationAsync(Guid nodeId, CancellationToken ct = default);
    Task<NodeDebugConfigurationDto?> GetDebugConfigurationBySerialAsync(string serialNumber, CancellationToken ct = default);
    Task<NodeDebugConfigurationDto?> SetDebugLevelAsync(Guid nodeId, SetNodeDebugLevelDto dto, CancellationToken ct = default);
    Task<NodeDebugConfigurationDto?> SetDebugLevelBySerialAsync(string serialNumber, SetNodeDebugLevelDto dto, CancellationToken ct = default);
    Task<NodeErrorStatisticsDto?> GetErrorStatisticsAsync(Guid nodeId, CancellationToken ct = default);
    Task<IEnumerable<NodeErrorStatisticsDto>> GetAllErrorStatisticsAsync(CancellationToken ct = default);
    Task<DebugLogCleanupResultDto> CleanupLogsAsync(DateTime before, CancellationToken ct = default);
    Task<int> ClearLogsAsync(Guid nodeId, CancellationToken ct = default);
}
