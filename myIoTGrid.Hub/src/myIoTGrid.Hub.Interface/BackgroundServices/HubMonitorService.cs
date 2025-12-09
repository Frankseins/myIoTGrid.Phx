using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using myIoTGrid.Hub.Infrastructure.Data;

namespace myIoTGrid.Hub.Interface.BackgroundServices;

/// <summary>
/// Background service that monitors hubs for offline status
/// </summary>
public class HubMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MonitoringOptions _options;
    private readonly ILogger<HubMonitorService> _logger;

    public HubMonitorService(
        IServiceScopeFactory scopeFactory,
        IOptions<MonitoringOptions> options,
        ILogger<HubMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableHubMonitoring)
        {
            _logger.LogInformation("Hub monitoring is disabled");
            return;
        }

        _logger.LogInformation(
            "HubMonitorService started. Check interval: {Interval}s, Offline timeout: {Timeout}min",
            _options.HubCheckIntervalSeconds,
            _options.HubOfflineTimeoutMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckHubsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during hub monitoring");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.HubCheckIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("HubMonitorService stopped");
    }

    private async Task CheckHubsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HubDbContext>();
        var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
        var hubService = scope.ServiceProvider.GetRequiredService<IHubService>();
        var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();

        // Single-Hub-Architecture: The Hub itself is always online when the API is running
        // Update LastSeen for all hubs to mark them as online
        var allHubs = await dbContext.Hubs.ToListAsync(ct);
        foreach (var hub in allHubs)
        {
            hub.LastSeen = DateTime.UtcNow;
            hub.IsOnline = true;
        }
        await dbContext.SaveChangesAsync(ct);

        var threshold = DateTime.UtcNow.AddMinutes(-_options.HubOfflineTimeoutMinutes);

        // Find hubs that have gone offline (not applicable in Single-Hub-Architecture, but kept for future multi-hub support)
        var offlineHubs = await dbContext.Hubs
            .Where(h => h.IsOnline && (h.LastSeen == null || h.LastSeen < threshold))
            .ToListAsync(ct);

        foreach (var hub in offlineHubs)
        {
            _logger.LogWarning(
                "Hub detected as offline: {HubId} ({Name}), LastSeen: {LastSeen}",
                hub.HubId,
                hub.Name,
                hub.LastSeen);

            // Set tenant context for hub update and alert creation
            tenantService.SetCurrentTenantId(hub.TenantId);

            // Update hub status
            await hubService.SetOnlineStatusAsync(hub.Id, false, ct);

            // Create offline alert
            await CreateHubOfflineAlertAsync(alertService, hub, ct);
        }

        // Find hubs that have come back online (LastSeen updated recently, but IsOnline is false)
        var onlineHubs = await dbContext.Hubs
            .Where(h => !h.IsOnline && h.LastSeen != null && h.LastSeen >= threshold)
            .ToListAsync(ct);

        foreach (var hub in onlineHubs)
        {
            _logger.LogInformation(
                "Hub detected as back online: {HubId} ({Name})",
                hub.HubId,
                hub.Name);

            // Set tenant context
            tenantService.SetCurrentTenantId(hub.TenantId);

            // Update hub status
            await hubService.SetOnlineStatusAsync(hub.Id, true, ct);

            // Deactivate existing offline alerts for this hub
            await alertService.DeactivateHubAlertsAsync(hub.Id, "hub_offline", ct);
        }

        if (offlineHubs.Count > 0 || onlineHubs.Count > 0)
        {
            _logger.LogInformation(
                "Hub monitoring cycle completed. Offline: {Offline}, Back online: {Online}",
                offlineHubs.Count,
                onlineHubs.Count);
        }
    }

    private async Task CreateHubOfflineAlertAsync(IAlertService alertService, myIoTGrid.Shared.Common.Entities.Hub hub, CancellationToken ct)
    {
        try
        {
            var dto = new CreateAlertDto(
                AlertTypeCode: "hub_offline",
                HubId: hub.HubId,
                NodeId: null,
                Level: AlertLevelDto.Critical,
                Message: $"Hub '{hub.Name}' ({hub.HubId}) is offline. Last seen: {hub.LastSeen:u}",
                Recommendation: "Check Hub power supply, network connection, or internet connectivity."
            );

            await alertService.CreateLocalAlertAsync(dto, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create offline alert for hub {HubId}", hub.HubId);
        }
    }
}
