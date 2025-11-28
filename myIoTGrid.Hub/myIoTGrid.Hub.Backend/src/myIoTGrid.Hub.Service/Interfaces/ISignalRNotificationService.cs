using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service Interface for SignalR notifications.
/// Enables sending real-time updates to connected clients.
/// </summary>
public interface ISignalRNotificationService
{
    /// <summary>
    /// Sends a new Reading to all clients in the Tenant.
    /// Event: "NewReading"
    /// </summary>
    Task NotifyNewReadingAsync(ReadingDto reading, CancellationToken ct = default);

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
    /// Sends a Node status change to all clients in the Tenant.
    /// Event: "NodeStatusChanged"
    /// </summary>
    Task NotifyNodeStatusChangedAsync(Guid tenantId, NodeDto node, CancellationToken ct = default);

    /// <summary>
    /// Sends a Node registered notification to all clients in the Hub group.
    /// Event: "NodeRegistered"
    /// </summary>
    Task NotifyNodeRegisteredAsync(Guid hubId, NodeDto node, CancellationToken ct = default);
}
