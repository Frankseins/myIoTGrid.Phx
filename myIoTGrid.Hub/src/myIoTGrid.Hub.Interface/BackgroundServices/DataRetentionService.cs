using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using myIoTGrid.Hub.Infrastructure.Data;

namespace myIoTGrid.Hub.Interface.BackgroundServices;

/// <summary>
/// Background service that cleans up old reading data based on retention policy.
/// Matter-konform: Bereinigt alte Attribute Reports.
/// </summary>
public class DataRetentionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MonitoringOptions _options;
    private readonly ILogger<DataRetentionService> _logger;

    public DataRetentionService(
        IServiceScopeFactory scopeFactory,
        IOptions<MonitoringOptions> options,
        ILogger<DataRetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableDataRetention)
        {
            _logger.LogInformation("Data retention service is disabled");
            return;
        }

        _logger.LogInformation(
            "DataRetentionService started. Interval: {Interval}h, Retention: {Retention} days",
            _options.DataRetentionIntervalHours,
            _options.DataRetentionDays);

        // Initial delay to avoid running during startup
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldDataAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during data retention cleanup");
            }

            await Task.Delay(TimeSpan.FromHours(_options.DataRetentionIntervalHours), stoppingToken);
        }

        _logger.LogInformation("DataRetentionService stopped");
    }

    private async Task CleanupOldDataAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HubDbContext>();

        var threshold = DateTime.UtcNow.AddDays(-_options.DataRetentionDays);

        _logger.LogInformation(
            "Starting data retention cleanup. Removing data older than {Threshold:u}",
            threshold);

        // Delete old readings that have been synced to cloud
        var deletedReadings = await dbContext.Readings
            .Where(r => r.Timestamp < threshold && r.IsSyncedToCloud)
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation(
            "Deleted {Count} synced readings older than {Days} days",
            deletedReadings,
            _options.DataRetentionDays);

        // Delete old acknowledged alerts
        var deletedAlerts = await dbContext.Alerts
            .Where(a => a.CreatedAt < threshold && a.AcknowledgedAt != null)
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation(
            "Deleted {Count} acknowledged alerts older than {Days} days",
            deletedAlerts,
            _options.DataRetentionDays);

        // Delete old inactive alerts
        var deletedInactiveAlerts = await dbContext.Alerts
            .Where(a => a.CreatedAt < threshold && !a.IsActive)
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation(
            "Deleted {Count} inactive alerts older than {Days} days",
            deletedInactiveAlerts,
            _options.DataRetentionDays);

        // Vacuum the database to reclaim space (SQLite specific)
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("VACUUM", ct);
            _logger.LogDebug("Database vacuumed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to vacuum database");
        }

        _logger.LogInformation(
            "Data retention cleanup completed. Total deleted: Readings={Readings}, Alerts={Alerts}",
            deletedReadings,
            deletedAlerts + deletedInactiveAlerts);
    }
}
