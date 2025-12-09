using myIoTGrid.Shared.Common.DTOs;

namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service for sending SignalR notifications.
/// </summary>
public interface ISignalRNotificationService
{
    Task NotifyNewReadingAsync(ReadingDto reading, CancellationToken ct = default);
    Task NotifyNodeStatusChangedAsync(Guid tenantId, NodeDto node, CancellationToken ct = default);
    Task NotifyNodeRegisteredAsync(Guid hubId, NodeDto node, CancellationToken ct = default);
    Task NotifyHubStatusChangedAsync(Guid tenantId, HubDto hub, CancellationToken ct = default);
    Task NotifyAlertCreatedAsync(Guid tenantId, AlertDto alert, CancellationToken ct = default);
    Task NotifyAlertAcknowledgedAsync(Guid tenantId, AlertDto alert, CancellationToken ct = default);
    Task NotifyAlertReceivedAsync(Guid tenantId, AlertDto alert, CancellationToken ct = default);
    Task NotifyDebugLogReceivedAsync(NodeDebugLogDto log, CancellationToken ct = default);
    Task NotifyDebugConfigChangedAsync(NodeDebugConfigurationDto config, CancellationToken ct = default);
}
