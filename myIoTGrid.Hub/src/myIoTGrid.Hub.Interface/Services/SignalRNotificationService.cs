using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Interface.Hubs;

namespace myIoTGrid.Hub.Interface.Services;

/// <summary>
/// Service für SignalR-Benachrichtigungen.
/// Sendet Echtzeit-Updates an verbundene Clients.
/// </summary>
public class SignalRNotificationService : ISignalRNotificationService
{
    private readonly IHubContext<SensorHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(
        IHubContext<SensorHub> hubContext,
        ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotifyNewReadingAsync(ReadingDto reading, CancellationToken ct = default)
    {
        var tenantGroup = SensorHub.GetTenantGroupName(reading.TenantId);
        var nodeGroup = SensorHub.GetNodeGroupName(reading.NodeId);

        // Sende an Tenant-Gruppe
        await _hubContext.Clients.Group(tenantGroup)
            .SendAsync("NewReading", reading, ct);

        // Sende zusätzlich an Node-Gruppe (für Node-spezifische Abonnenten)
        await _hubContext.Clients.Group(nodeGroup)
            .SendAsync("NewReading", reading, ct);

        _logger.LogDebug(
            "SignalR NewReading gesendet: {MeasurementType}={Value} (Tenant: {TenantId}, Node: {NodeId})",
            reading.MeasurementType,
            reading.Value,
            reading.TenantId,
            reading.NodeId);
    }

    /// <inheritdoc />
    public async Task NotifyAlertReceivedAsync(Guid tenantId, AlertDto alert, CancellationToken ct = default)
    {
        var tenantGroup = SensorHub.GetTenantGroupName(tenantId);
        var alertGroup = SensorHub.GetAlertGroupName((int)alert.Level);

        // Sende an Tenant-Gruppe
        await _hubContext.Clients.Group(tenantGroup)
            .SendAsync("AlertReceived", alert, ct);

        // Sende zusätzlich an Alert-Level-Gruppe (für Level-Filter im Frontend)
        await _hubContext.Clients.Group(alertGroup)
            .SendAsync("AlertReceived", alert, ct);

        _logger.LogInformation(
            "SignalR AlertReceived gesendet: {AlertType} - {Level} (Tenant: {TenantId})",
            alert.AlertTypeCode,
            alert.Level,
            tenantId);
    }

    /// <inheritdoc />
    public async Task NotifyAlertAcknowledgedAsync(Guid tenantId, AlertDto alert, CancellationToken ct = default)
    {
        var groupName = SensorHub.GetTenantGroupName(tenantId);

        await _hubContext.Clients.Group(groupName)
            .SendAsync("AlertAcknowledged", alert, ct);

        _logger.LogDebug(
            "SignalR AlertAcknowledged gesendet: {AlertId} (Tenant: {TenantId})",
            alert.Id,
            tenantId);
    }

    /// <inheritdoc />
    public async Task NotifyAlertCreatedAsync(Guid tenantId, AlertDto alert, CancellationToken ct = default)
    {
        var tenantGroup = SensorHub.GetTenantGroupName(tenantId);
        var alertGroup = SensorHub.GetAlertGroupName((int)alert.Level);

        // Sende an Tenant-Gruppe
        await _hubContext.Clients.Group(tenantGroup)
            .SendAsync("AlertCreated", alert, ct);

        // Sende zusätzlich an Alert-Level-Gruppe (für Level-Filter im Frontend)
        await _hubContext.Clients.Group(alertGroup)
            .SendAsync("AlertCreated", alert, ct);

        _logger.LogInformation(
            "SignalR AlertCreated gesendet: {AlertType} - {Level} (Tenant: {TenantId})",
            alert.AlertTypeCode,
            alert.Level,
            tenantId);
    }

    /// <inheritdoc />
    public async Task NotifyHubStatusChangedAsync(Guid tenantId, HubDto hub, CancellationToken ct = default)
    {
        var tenantGroup = SensorHub.GetTenantGroupName(tenantId);
        var hubGroup = SensorHub.GetHubGroupName(hub.HubId);

        // Sende an Tenant-Gruppe
        await _hubContext.Clients.Group(tenantGroup)
            .SendAsync("HubStatusChanged", hub, ct);

        // Sende zusätzlich an Hub-Gruppe (für Hub-spezifische Abonnenten)
        await _hubContext.Clients.Group(hubGroup)
            .SendAsync("HubStatusChanged", hub, ct);

        _logger.LogInformation(
            "SignalR HubStatusChanged gesendet: {HubId} - Online: {IsOnline} (Tenant: {TenantId})",
            hub.HubId,
            hub.IsOnline,
            tenantId);
    }

    /// <inheritdoc />
    public async Task NotifyNodeStatusChangedAsync(Guid tenantId, NodeDto node, CancellationToken ct = default)
    {
        var tenantGroup = SensorHub.GetTenantGroupName(tenantId);
        var nodeGroup = SensorHub.GetNodeGroupName(node.Id);

        // Sende an Tenant-Gruppe
        await _hubContext.Clients.Group(tenantGroup)
            .SendAsync("NodeStatusChanged", node, ct);

        // Sende zusätzlich an Node-Gruppe (für Node-spezifische Abonnenten)
        await _hubContext.Clients.Group(nodeGroup)
            .SendAsync("NodeStatusChanged", node, ct);

        _logger.LogInformation(
            "SignalR NodeStatusChanged gesendet: {NodeId} - Online: {IsOnline} (Tenant: {TenantId})",
            node.NodeId,
            node.IsOnline,
            tenantId);
    }

    /// <inheritdoc />
    public async Task NotifyNodeRegisteredAsync(Guid hubId, NodeDto node, CancellationToken ct = default)
    {
        var hubGroup = SensorHub.GetHubGroupName(hubId.ToString());

        await _hubContext.Clients.Group(hubGroup)
            .SendAsync("NodeRegistered", node, ct);

        _logger.LogInformation(
            "SignalR NodeRegistered gesendet: {NodeId} (Hub: {HubId})",
            node.NodeId,
            hubId);
    }

    // === Remote Debug System (Sprint 8) ===

    /// <inheritdoc />
    public async Task NotifyDebugLogReceivedAsync(NodeDebugLogDto log, CancellationToken ct = default)
    {
        var nodeGroup = SensorHub.GetNodeGroupName(log.NodeId);
        var debugGroup = SensorHub.GetDebugGroupName(log.NodeId);

        // Send to node-specific group
        await _hubContext.Clients.Group(nodeGroup)
            .SendAsync("DebugLogReceived", log, ct);

        // Send to debug-specific group (for live log viewer)
        await _hubContext.Clients.Group(debugGroup)
            .SendAsync("DebugLogReceived", log, ct);

        _logger.LogDebug(
            "SignalR DebugLogReceived gesendet: [{Level}] {Category} - {Message} (Node: {NodeId})",
            log.Level,
            log.Category,
            log.Message[..Math.Min(50, log.Message.Length)],
            log.NodeId);
    }

    /// <inheritdoc />
    public async Task NotifyDebugConfigChangedAsync(NodeDebugConfigurationDto config, CancellationToken ct = default)
    {
        var nodeGroup = SensorHub.GetNodeGroupName(config.NodeId);

        await _hubContext.Clients.Group(nodeGroup)
            .SendAsync("DebugConfigChanged", config, ct);

        // Also broadcast to all clients for dashboard updates
        await _hubContext.Clients.All
            .SendAsync("DebugConfigChanged", config, ct);

        _logger.LogInformation(
            "SignalR DebugConfigChanged gesendet: {NodeId} - Level: {Level}, RemoteLogging: {RemoteLogging}",
            config.NodeId,
            config.DebugLevel,
            config.EnableRemoteLogging);
    }
}
