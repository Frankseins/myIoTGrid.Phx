using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for Node Debug Log management (Sprint 8: Remote Debug System).
/// </summary>
public class NodeDebugLogService : INodeDebugLogService
{
    private readonly HubDbContext _context;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly ILogger<NodeDebugLogService> _logger;

    public NodeDebugLogService(
        HubDbContext context,
        ISignalRNotificationService signalRNotificationService,
        ILogger<NodeDebugLogService> logger)
    {
        _context = context;
        _signalRNotificationService = signalRNotificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PaginatedResultDto<NodeDebugLogDto>> GetLogsAsync(DebugLogFilterDto filter, CancellationToken ct = default)
    {
        var query = _context.NodeDebugLogs
            .AsNoTracking()
            .Where(l => l.NodeId == filter.NodeId);

        // Apply filters
        if (filter.MinLevel.HasValue)
        {
            var minLevel = filter.MinLevel.Value.ToEntity();
            query = query.Where(l => l.Level >= minLevel);
        }

        if (filter.Category.HasValue)
        {
            var category = filter.Category.Value.ToEntity();
            query = query.Where(l => l.Category == category);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(l => l.ReceivedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(l => l.ReceivedAt <= filter.ToDate.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(ct);

        // Apply paging and sorting
        var logs = await query
            .OrderByDescending(l => l.ReceivedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return new PaginatedResultDto<NodeDebugLogDto>(
            Items: logs.Select(l => l.ToDto()).ToList(),
            TotalCount: totalCount,
            Page: filter.PageNumber,
            PageSize: filter.PageSize
        );
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NodeDebugLogDto>> GetRecentLogsAsync(Guid nodeId, int count = 50, CancellationToken ct = default)
    {
        var logs = await _context.NodeDebugLogs
            .AsNoTracking()
            .Where(l => l.NodeId == nodeId)
            .OrderByDescending(l => l.ReceivedAt)
            .Take(count)
            .ToListAsync(ct);

        return logs.Select(l => l.ToDto());
    }

    /// <inheritdoc />
    public async Task<int> CreateBatchAsync(string serialNumber, IEnumerable<CreateNodeDebugLogDto> logs, CancellationToken ct = default)
    {
        // Find node by serial number (can be MAC address, NodeId, or GUID Id)
        var upperSerial = serialNumber.ToUpperInvariant();

        // Try to parse as GUID for direct Id lookup
        Guid? guidId = Guid.TryParse(serialNumber, out var parsedGuid) ? parsedGuid : null;

        var node = await _context.Nodes
            .FirstOrDefaultAsync(n =>
                n.MacAddress == upperSerial ||
                n.NodeId == serialNumber ||
                (guidId.HasValue && n.Id == guidId.Value), ct);

        if (node == null)
        {
            _logger.LogWarning("Node not found for serial number: {SerialNumber}", serialNumber);
            return 0;
        }

        // Check if remote logging is enabled
        if (!node.EnableRemoteLogging)
        {
            _logger.LogDebug("Remote logging disabled for node: {NodeId}", node.NodeId);
            return 0;
        }

        var entities = logs.Select(l => l.ToEntity(node.Id)).ToList();

        _context.NodeDebugLogs.AddRange(entities);
        await _context.SaveChangesAsync(ct);

        _logger.LogDebug("Created {Count} debug logs for node {NodeId}", entities.Count, node.NodeId);

        // Notify via SignalR for live view
        foreach (var entity in entities)
        {
            await _signalRNotificationService.NotifyDebugLogReceivedAsync(entity.ToDto(), ct);
        }

        return entities.Count;
    }

    /// <inheritdoc />
    public async Task<NodeDebugConfigurationDto?> GetDebugConfigurationAsync(Guid nodeId, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == nodeId, ct);

        return node?.ToDebugConfigDto();
    }

    /// <inheritdoc />
    public async Task<NodeDebugConfigurationDto?> GetDebugConfigurationBySerialAsync(string serialNumber, CancellationToken ct = default)
    {
        // Search by both MacAddress and NodeId (ESP32 sends NodeId as serialNumber)
        var upperSerial = serialNumber.ToUpperInvariant();
        var node = await _context.Nodes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.MacAddress == upperSerial || n.NodeId == serialNumber, ct);

        return node?.ToDebugConfigDto();
    }

    /// <inheritdoc />
    public async Task<NodeDebugConfigurationDto?> SetDebugLevelAsync(Guid nodeId, SetNodeDebugLevelDto dto, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .FirstOrDefaultAsync(n => n.Id == nodeId, ct);

        if (node == null)
        {
            return null;
        }

        node.DebugLevel = dto.DebugLevel.ToEntity();
        node.EnableRemoteLogging = dto.EnableRemoteLogging;
        node.LastDebugChange = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Set debug level {Level} for node {NodeId}, remote logging: {RemoteLogging}",
            dto.DebugLevel, node.NodeId, dto.EnableRemoteLogging);

        // Notify via SignalR
        await _signalRNotificationService.NotifyDebugConfigChangedAsync(node.ToDebugConfigDto(), ct);

        return node.ToDebugConfigDto();
    }

    /// <inheritdoc />
    public async Task<NodeDebugConfigurationDto?> SetDebugLevelBySerialAsync(string serialNumber, SetNodeDebugLevelDto dto, CancellationToken ct = default)
    {
        // Search by both MacAddress and NodeId (ESP32 sends NodeId as serialNumber)
        var upperSerial = serialNumber.ToUpperInvariant();
        var node = await _context.Nodes
            .FirstOrDefaultAsync(n => n.MacAddress == upperSerial || n.NodeId == serialNumber, ct);

        if (node == null)
        {
            return null;
        }

        return await SetDebugLevelAsync(node.Id, dto, ct);
    }

    /// <inheritdoc />
    public async Task<NodeErrorStatisticsDto?> GetErrorStatisticsAsync(Guid nodeId, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == nodeId, ct);

        if (node == null)
        {
            return null;
        }

        var stats = await _context.NodeDebugLogs
            .Where(l => l.NodeId == nodeId)
            .GroupBy(l => 1)
            .Select(g => new
            {
                TotalCount = g.Count(),
                ErrorCount = g.Count(l => l.Category == LogCategory.Error),
                WarningCount = g.Count(l => l.Level == DebugLevel.Normal),
                InfoCount = g.Count(l => l.Level == DebugLevel.Debug)
            })
            .FirstOrDefaultAsync(ct);

        var lastError = await _context.NodeDebugLogs
            .Where(l => l.NodeId == nodeId && l.Category == LogCategory.Error)
            .OrderByDescending(l => l.ReceivedAt)
            .FirstOrDefaultAsync(ct);

        var errorsByCategory = await _context.NodeDebugLogs
            .Where(l => l.NodeId == nodeId && l.Category == LogCategory.Error)
            .GroupBy(l => l.Category.ToString())
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count, ct);

        return new NodeErrorStatisticsDto(
            NodeId: nodeId,
            NodeName: node.Name,
            TotalLogs: stats?.TotalCount ?? 0,
            ErrorCount: stats?.ErrorCount ?? 0,
            WarningCount: stats?.WarningCount ?? 0,
            InfoCount: stats?.InfoCount ?? 0,
            ErrorsByCategory: errorsByCategory,
            LastErrorAt: lastError?.ReceivedAt,
            LastErrorMessage: lastError?.Message
        );
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NodeErrorStatisticsDto>> GetAllErrorStatisticsAsync(CancellationToken ct = default)
    {
        var nodes = await _context.Nodes
            .AsNoTracking()
            .ToListAsync(ct);

        var results = new List<NodeErrorStatisticsDto>();

        foreach (var node in nodes)
        {
            var stats = await GetErrorStatisticsAsync(node.Id, ct);
            if (stats != null)
            {
                results.Add(stats);
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<DebugLogCleanupResultDto> CleanupLogsAsync(DateTime before, CancellationToken ct = default)
    {
        var logsToDelete = await _context.NodeDebugLogs
            .Where(l => l.ReceivedAt < before)
            .ToListAsync(ct);

        var count = logsToDelete.Count;

        _context.NodeDebugLogs.RemoveRange(logsToDelete);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Cleaned up {Count} debug logs older than {Before}", count, before);

        return new DebugLogCleanupResultDto(
            DeletedCount: count,
            CleanupBefore: before
        );
    }

    /// <inheritdoc />
    public async Task<int> ClearLogsAsync(Guid nodeId, CancellationToken ct = default)
    {
        var logsToDelete = await _context.NodeDebugLogs
            .Where(l => l.NodeId == nodeId)
            .ToListAsync(ct);

        var count = logsToDelete.Count;

        _context.NodeDebugLogs.RemoveRange(logsToDelete);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Cleared {Count} debug logs for node {NodeId}", count, nodeId);

        return count;
    }
}
