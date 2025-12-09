using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using myIoTGrid.Hub.Infrastructure.Data;

namespace myIoTGrid.Hub.Interface.BackgroundServices;

/// <summary>
/// Background service that monitors Nodes (ESP32/LoRa32 devices) for offline status.
/// Matter-konform: Überwacht Matter Nodes.
/// </summary>
public class NodeMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MonitoringOptions _options;
    private readonly ILogger<NodeMonitorService> _logger;

    public NodeMonitorService(
        IServiceScopeFactory scopeFactory,
        IOptions<MonitoringOptions> options,
        ILogger<NodeMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableNodeMonitoring)
        {
            _logger.LogInformation("Node monitoring is disabled");
            return;
        }

        _logger.LogInformation(
            "NodeMonitorService started. Check interval: {Interval}s, Offline timeout: {Timeout}min",
            _options.NodeCheckIntervalSeconds,
            _options.NodeOfflineTimeoutMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckNodesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during node monitoring");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.NodeCheckIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("NodeMonitorService stopped");
    }

    private async Task CheckNodesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HubDbContext>();
        var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
        var nodeService = scope.ServiceProvider.GetRequiredService<INodeService>();
        var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();

        var threshold = DateTime.UtcNow.AddMinutes(-_options.NodeOfflineTimeoutMinutes);

        // Find nodes that have gone offline
        var offlineNodes = await dbContext.Nodes
            .Include(n => n.Hub)
            .Where(n => n.IsOnline && (n.LastSeen == null || n.LastSeen < threshold))
            .ToListAsync(ct);

        foreach (var node in offlineNodes)
        {
            _logger.LogWarning(
                "Node detected as offline: {NodeId} ({Name}), LastSeen: {LastSeen}",
                node.NodeId,
                node.Name,
                node.LastSeen);

            // Update node status
            await nodeService.SetOnlineStatusAsync(node.Id, false, ct);

            // Set tenant context for alert creation
            if (node.Hub != null)
            {
                tenantService.SetCurrentTenantId(node.Hub.TenantId);
            }

            // Create offline alert
            await CreateNodeOfflineAlertAsync(alertService, node, ct);
        }

        // Find nodes that have come back online (LastSeen updated recently, but IsOnline is false)
        var onlineNodes = await dbContext.Nodes
            .Include(n => n.Hub)
            .Where(n => !n.IsOnline && n.LastSeen != null && n.LastSeen >= threshold)
            .ToListAsync(ct);

        foreach (var node in onlineNodes)
        {
            _logger.LogInformation(
                "Node detected as back online: {NodeId} ({Name})",
                node.NodeId,
                node.Name);

            // Update node status
            await nodeService.SetOnlineStatusAsync(node.Id, true, ct);

            // Set tenant context
            if (node.Hub != null)
            {
                tenantService.SetCurrentTenantId(node.Hub.TenantId);
            }

            // Deactivate existing offline alerts for this node
            await alertService.DeactivateNodeAlertsAsync(node.Id, "node_offline", ct);
        }

        if (offlineNodes.Count > 0 || onlineNodes.Count > 0)
        {
            _logger.LogInformation(
                "Node monitoring cycle completed. Offline: {Offline}, Back online: {Online}",
                offlineNodes.Count,
                onlineNodes.Count);
        }
    }

    private async Task CreateNodeOfflineAlertAsync(IAlertService alertService, Node node, CancellationToken ct)
    {
        try
        {
            var dto = new CreateAlertDto(
                AlertTypeCode: "node_offline",
                NodeId: node.NodeId,
                HubId: node.Hub?.HubId,
                Level: AlertLevelDto.Warning,
                Message: $"Node '{node.Name}' ({node.NodeId}) is offline. Last seen: {node.LastSeen:u}",
                Recommendation: "Check node power supply, network connection, or signal strength."
            );

            await alertService.CreateLocalAlertAsync(dto, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create offline alert for node {NodeId}", node.NodeId);
        }
    }
}
