using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using myIoTGrid.Hub.Infrastructure.Data;

namespace myIoTGrid.Hub.Interface.BackgroundServices;

/// <summary>
/// Background service for cleaning up old debug logs (Sprint 8: Remote Debug System).
/// Removes logs older than the configured retention period and enforces per-node limits.
/// </summary>
public class DebugLogCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MonitoringOptions _options;
    private readonly ILogger<DebugLogCleanupService> _logger;

    public DebugLogCleanupService(
        IServiceScopeFactory scopeFactory,
        IOptions<MonitoringOptions> options,
        ILogger<DebugLogCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableDebugLogCleanup)
        {
            _logger.LogInformation("Debug log cleanup service is disabled");
            return;
        }

        _logger.LogInformation(
            "DebugLogCleanupService started. Interval: {Interval}h, Retention: {Retention} days, MaxPerNode: {MaxPerNode}",
            _options.DebugLogCleanupIntervalHours,
            _options.DebugLogRetentionDays,
            _options.MaxDebugLogsPerNode);

        // Initial delay to avoid running during startup
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupDebugLogsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during debug log cleanup");
            }

            await Task.Delay(TimeSpan.FromHours(_options.DebugLogCleanupIntervalHours), stoppingToken);
        }

        _logger.LogInformation("DebugLogCleanupService stopped");
    }

    private async Task CleanupDebugLogsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HubDbContext>();

        var threshold = DateTime.UtcNow.AddDays(-_options.DebugLogRetentionDays);
        var totalDeleted = 0;

        _logger.LogInformation(
            "Starting debug log cleanup. Removing logs older than {Threshold:u}",
            threshold);

        // 1. Delete logs older than retention period
        var deletedByAge = await dbContext.NodeDebugLogs
            .Where(l => l.ReceivedAt < threshold)
            .ExecuteDeleteAsync(ct);

        totalDeleted += deletedByAge;

        _logger.LogDebug(
            "Deleted {Count} debug logs older than {Days} days",
            deletedByAge,
            _options.DebugLogRetentionDays);

        // 2. Enforce per-node log limits
        var nodeIds = await dbContext.Nodes
            .Select(n => n.Id)
            .ToListAsync(ct);

        foreach (var nodeId in nodeIds)
        {
            var logCount = await dbContext.NodeDebugLogs
                .Where(l => l.NodeId == nodeId)
                .CountAsync(ct);

            if (logCount > _options.MaxDebugLogsPerNode)
            {
                // Find the oldest logs that exceed the limit
                var excessCount = logCount - _options.MaxDebugLogsPerNode;

                var oldestLogIds = await dbContext.NodeDebugLogs
                    .Where(l => l.NodeId == nodeId)
                    .OrderBy(l => l.ReceivedAt)
                    .Take(excessCount)
                    .Select(l => l.Id)
                    .ToListAsync(ct);

                if (oldestLogIds.Count > 0)
                {
                    var deletedByLimit = await dbContext.NodeDebugLogs
                        .Where(l => oldestLogIds.Contains(l.Id))
                        .ExecuteDeleteAsync(ct);

                    totalDeleted += deletedByLimit;

                    _logger.LogDebug(
                        "Deleted {Count} excess debug logs for node {NodeId} (limit: {Limit})",
                        deletedByLimit,
                        nodeId,
                        _options.MaxDebugLogsPerNode);
                }
            }
        }

        _logger.LogInformation(
            "Debug log cleanup completed. Total deleted: {TotalDeleted} (by age: {ByAge})",
            totalDeleted,
            deletedByAge);
    }
}
