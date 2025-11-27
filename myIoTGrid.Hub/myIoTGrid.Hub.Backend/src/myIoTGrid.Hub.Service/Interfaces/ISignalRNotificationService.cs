using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service Interface for SignalR notifications.
/// Enables sending real-time updates to connected clients.
/// </summary>
public interface ISignalRNotificationService
{
    /// <summary>
    /// Sends new sensor data to all clients in the Tenant.
    /// Event: "NewSensorData"
    /// </summary>
    Task NotifyNewSensorDataAsync(Guid tenantId, SensorDataDto sensorData, CancellationToken ct = default);

    /// <summary>
    /// Sends a new Alert to all clients in the Tenant.
    /// Event: "AlertReceived"
    /// </summary>
    Task NotifyAlertReceivedAsync(Guid tenantId, AlertDto alert, CancellationToken ct = default);

    /// <summary>
    /// Sends an Alert acknowledgment to all clients in the Tenant.
    /// Event: "AlertAcknowledged"
    /// </summary>
    Task NotifyAlertAcknowledgedAsync(Guid tenantId, AlertDto alert, CancellationToken ct = default);

    /// <summary>
    /// Sends a Hub status change to all clients in the Tenant.
    /// Event: "HubStatusChanged"
    /// </summary>
    Task NotifyHubStatusChangedAsync(Guid tenantId, HubDto hub, CancellationToken ct = default);

    /// <summary>
    /// Sends a Sensor status change to all clients in the Tenant.
    /// Event: "SensorStatusChanged"
    /// </summary>
    Task NotifySensorStatusChangedAsync(Guid tenantId, SensorDto sensor, CancellationToken ct = default);
}
