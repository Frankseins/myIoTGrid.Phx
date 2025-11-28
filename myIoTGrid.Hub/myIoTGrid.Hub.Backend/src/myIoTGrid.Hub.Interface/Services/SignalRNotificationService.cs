using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Interface.Hubs;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;

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
            "SignalR NewReading gesendet: {SensorTypeId}={Value} (Tenant: {TenantId}, Node: {NodeId})",
            reading.SensorTypeId,
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
}
