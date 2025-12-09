using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Infrastructure.Matter;

namespace myIoTGrid.Hub.Interface.BackgroundServices;

/// <summary>
/// Background service that initializes Matter Bridge with existing nodes and sensors (v3.0).
/// Matter-konform: Registriert Matter Nodes und Endpoints.
/// Two-tier model: Uses Sensor.Code instead of SensorType.Code
/// </summary>
public class MatterBridgeService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMatterBridgeClient _matterBridgeClient;
    private readonly MatterBridgeOptions _options;
    private readonly ILogger<MatterBridgeService> _logger;

    public MatterBridgeService(
        IServiceScopeFactory scopeFactory,
        IMatterBridgeClient matterBridgeClient,
        IOptions<MatterBridgeOptions> options,
        ILogger<MatterBridgeService> logger)
    {
        _scopeFactory = scopeFactory;
        _matterBridgeClient = matterBridgeClient;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Matter Bridge integration is disabled");
            return;
        }

        _logger.LogInformation("Matter Bridge Service starting...");

        // Wait for Matter Bridge to be available
        await WaitForMatterBridgeAsync(stoppingToken);

        if (stoppingToken.IsCancellationRequested) return;

        // Register existing nodes with Matter Bridge
        await RegisterExistingNodesAsync(stoppingToken);

        _logger.LogInformation("Matter Bridge Service initialized");

        // Keep service running and periodically check for new nodes
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

                // Periodically sync nodes (in case any were missed)
                await SyncNodesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Matter Bridge sync loop");
            }
        }
    }

    private async Task WaitForMatterBridgeAsync(CancellationToken ct)
    {
        var retries = 0;
        const int maxRetries = 30;
        const int delaySeconds = 2;

        while (!ct.IsCancellationRequested && retries < maxRetries)
        {
            if (await _matterBridgeClient.IsAvailableAsync(ct))
            {
                _logger.LogInformation("Matter Bridge is available");
                return;
            }

            retries++;
            _logger.LogDebug("Waiting for Matter Bridge... ({Retry}/{MaxRetries})", retries, maxRetries);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
        }

        if (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Matter Bridge not available after {MaxRetries} retries", maxRetries);
        }
    }

    private async Task RegisterExistingNodesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HubDbContext>();

        try
        {
            // Get all nodes with their Hub and SensorAssignments (v3.0: direct Sensor reference)
            var nodes = await context.Nodes
                .Include(n => n.Hub)
                .Include(n => n.SensorAssignments)
                    .ThenInclude(a => a.Sensor)
                .AsNoTracking()
                .ToListAsync(ct);

            _logger.LogInformation("Registering {Count} nodes with Matter Bridge", nodes.Count);

            foreach (var node in nodes)
            {
                // Each assignment on the node represents a Matter Endpoint
                foreach (var assignment in node.SensorAssignments)
                {
                    // v3.0: Use Sensor.Code directly instead of SensorType.Code
                    var sensorCode = assignment.Sensor?.Code ?? string.Empty;

                    if (!MatterDeviceMapping.IsSupportedSensorType(sensorCode))
                    {
                        continue;
                    }

                    if (!_options.EnabledSensorTypes.Contains(sensorCode, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var matterType = MatterDeviceMapping.GetMatterDeviceType(sensorCode);
                    if (matterType == null) continue;

                    var deviceId = MatterDeviceMapping.GenerateMatterDeviceId(node.NodeId, sensorCode);
                    var displayName = MatterDeviceMapping.CreateDeviceDisplayName(
                        node.Name,
                        node.Location?.Name,
                        sensorCode
                    );

                    await _matterBridgeClient.RegisterDeviceAsync(
                        deviceId,
                        displayName,
                        matterType,
                        node.Location?.Name,
                        ct
                    );
                }
            }

            _logger.LogInformation("Node registration completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering existing nodes with Matter Bridge");
        }
    }

    private async Task SyncNodesAsync(CancellationToken ct)
    {
        if (!await _matterBridgeClient.IsAvailableAsync(ct))
        {
            _logger.LogDebug("Matter Bridge not available, skipping sync");
            return;
        }

        var status = await _matterBridgeClient.GetStatusAsync(ct);
        if (status == null) return;

        _logger.LogDebug("Matter Bridge status: {DeviceCount} devices registered", status.DeviceCount);

        // Here we could compare registered devices with DB and sync any missing ones
        // For now, we just log the status
    }
}
