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
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<SensorHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(
        Microsoft.AspNetCore.SignalR.IHubContext<SensorHub> hubContext,
        ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotifyNewSensorDataAsync(Guid tenantId, SensorDataDto sensorData, CancellationToken ct = default)
    {
        var groupName = SensorHub.GetTenantGroupName(tenantId);

        await _hubContext.Clients.Group(groupName)
            .SendAsync("NewSensorData", sensorData, ct);

        _logger.LogDebug(
            "SignalR NewSensorData gesendet: {SensorType}={Value} (Tenant: {TenantId})",
            sensorData.SensorTypeCode,
            sensorData.Value,
            tenantId);
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
        var deviceGroup = SensorHub.GetDeviceGroupName(hub.HubId);

        // Sende an Tenant-Gruppe
        await _hubContext.Clients.Group(tenantGroup)
            .SendAsync("HubStatusChanged", hub, ct);

        // Sende zusätzlich an Device-Gruppe (für Device-spezifische Abonnenten)
        await _hubContext.Clients.Group(deviceGroup)
            .SendAsync("HubStatusChanged", hub, ct);

        _logger.LogInformation(
            "SignalR HubStatusChanged gesendet: {HubId} - Online: {IsOnline} (Tenant: {TenantId})",
            hub.HubId,
            hub.IsOnline,
            tenantId);
    }

    /// <inheritdoc />
    public async Task NotifySensorStatusChangedAsync(Guid tenantId, SensorDto sensor, CancellationToken ct = default)
    {
        var tenantGroup = SensorHub.GetTenantGroupName(tenantId);
        var deviceGroup = SensorHub.GetDeviceGroupName(sensor.SensorId);

        // Sende an Tenant-Gruppe
        await _hubContext.Clients.Group(tenantGroup)
            .SendAsync("SensorStatusChanged", sensor, ct);

        // Sende zusätzlich an Device-Gruppe (für Device-spezifische Abonnenten)
        await _hubContext.Clients.Group(deviceGroup)
            .SendAsync("SensorStatusChanged", sensor, ct);

        _logger.LogInformation(
            "SignalR SensorStatusChanged gesendet: {SensorId} - Online: {IsOnline} (Tenant: {TenantId})",
            sensor.SensorId,
            sensor.IsOnline,
            tenantId);
    }
}
