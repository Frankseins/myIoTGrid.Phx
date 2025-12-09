using Microsoft.Extensions.Logging;

namespace myIoTGrid.Hub.Interface.Hubs;

/// <summary>
/// SignalR Hub für Echtzeit-Updates zu Sensordaten, Alerts und Hub-Status.
/// Clients können sich zu spezifischen Gruppen verbinden um nur relevante Updates zu erhalten.
/// </summary>
public class SensorHub : Microsoft.AspNetCore.SignalR.Hub
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<SensorHub> _logger;

    public SensorHub(ITenantService tenantService, ILogger<SensorHub> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Wird aufgerufen wenn ein Client sich verbindet.
    /// Der Client wird automatisch der Tenant-Gruppe zugewiesen.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var tenantGroup = GetTenantGroupName(tenantId);

        await Groups.AddToGroupAsync(Context.ConnectionId, tenantGroup);

        _logger.LogInformation(
            "Client verbunden: {ConnectionId} (Tenant: {TenantId})",
            Context.ConnectionId,
            tenantId);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Wird aufgerufen wenn ein Client die Verbindung trennt.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(
                exception,
                "Client-Verbindung unterbrochen: {ConnectionId}",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation(
                "Client getrennt: {ConnectionId}",
                Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Fügt den Client einer spezifischen Hub-Gruppe hinzu.
    /// So erhält der Client nur Updates für diesen spezifischen Hub.
    /// </summary>
    /// <param name="hubId">Die ID des Hubs</param>
    public async Task JoinHubGroup(string hubId)
    {
        if (string.IsNullOrWhiteSpace(hubId))
        {
            _logger.LogWarning("JoinHubGroup mit leerer hubId aufgerufen");
            return;
        }

        var groupName = GetHubGroupName(hubId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug(
            "Client {ConnectionId} hat Hub-Gruppe beigetreten: {HubId}",
            Context.ConnectionId,
            hubId);
    }

    /// <summary>
    /// Entfernt den Client aus einer spezifischen Hub-Gruppe.
    /// </summary>
    /// <param name="hubId">Die ID des Hubs</param>
    public async Task LeaveHubGroup(string hubId)
    {
        if (string.IsNullOrWhiteSpace(hubId))
        {
            _logger.LogWarning("LeaveHubGroup mit leerer hubId aufgerufen");
            return;
        }

        var groupName = GetHubGroupName(hubId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug(
            "Client {ConnectionId} hat Hub-Gruppe verlassen: {HubId}",
            Context.ConnectionId,
            hubId);
    }

    /// <summary>
    /// Fügt den Client einer spezifischen Node-Gruppe hinzu (ESP32/LoRa32 Devices).
    /// So erhält der Client nur Updates für diesen spezifischen Node.
    /// </summary>
    /// <param name="nodeId">Die ID des Nodes</param>
    public async Task JoinNodeGroup(Guid nodeId)
    {
        if (nodeId == Guid.Empty)
        {
            _logger.LogWarning("JoinNodeGroup mit leerer nodeId aufgerufen");
            return;
        }

        var groupName = GetNodeGroupName(nodeId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug(
            "Client {ConnectionId} hat Node-Gruppe beigetreten: {NodeId}",
            Context.ConnectionId,
            nodeId);
    }

    /// <summary>
    /// Entfernt den Client aus einer spezifischen Node-Gruppe.
    /// </summary>
    /// <param name="nodeId">Die ID des Nodes</param>
    public async Task LeaveNodeGroup(Guid nodeId)
    {
        if (nodeId == Guid.Empty)
        {
            _logger.LogWarning("LeaveNodeGroup mit leerer nodeId aufgerufen");
            return;
        }

        var groupName = GetNodeGroupName(nodeId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug(
            "Client {ConnectionId} hat Node-Gruppe verlassen: {NodeId}",
            Context.ConnectionId,
            nodeId);
    }

    /// <summary>
    /// Fügt den Client einer Alert-Level-Gruppe hinzu.
    /// So erhält der Client nur Alerts ab einem bestimmten Level.
    /// </summary>
    /// <param name="level">Alert-Level (0=Ok, 1=Info, 2=Warning, 3=Critical)</param>
    public async Task JoinAlertGroup(int level)
    {
        if (level < 0 || level > 3)
        {
            _logger.LogWarning("JoinAlertGroup mit ungültigem Level aufgerufen: {Level}", level);
            return;
        }

        var groupName = GetAlertGroupName(level);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug(
            "Client {ConnectionId} hat Alert-Gruppe beigetreten: Level {Level}",
            Context.ConnectionId,
            level);
    }

    /// <summary>
    /// Entfernt den Client aus einer Alert-Level-Gruppe.
    /// </summary>
    /// <param name="level">Alert-Level (0=Ok, 1=Info, 2=Warning, 3=Critical)</param>
    public async Task LeaveAlertGroup(int level)
    {
        if (level < 0 || level > 3)
        {
            _logger.LogWarning("LeaveAlertGroup mit ungültigem Level aufgerufen: {Level}", level);
            return;
        }

        var groupName = GetAlertGroupName(level);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug(
            "Client {ConnectionId} hat Alert-Gruppe verlassen: Level {Level}",
            Context.ConnectionId,
            level);
    }

    // === Remote Debug System (Sprint 8) ===

    /// <summary>
    /// Fügt den Client einer Debug-Log-Gruppe für einen Node hinzu.
    /// So erhält der Client Live-Debug-Logs für diesen spezifischen Node.
    /// </summary>
    /// <param name="nodeId">Die ID des Nodes</param>
    public async Task JoinDebugGroup(Guid nodeId)
    {
        if (nodeId == Guid.Empty)
        {
            _logger.LogWarning("JoinDebugGroup mit leerer nodeId aufgerufen");
            return;
        }

        var groupName = GetDebugGroupName(nodeId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug(
            "Client {ConnectionId} hat Debug-Gruppe beigetreten: {NodeId}",
            Context.ConnectionId,
            nodeId);
    }

    /// <summary>
    /// Entfernt den Client aus einer Debug-Log-Gruppe.
    /// </summary>
    /// <param name="nodeId">Die ID des Nodes</param>
    public async Task LeaveDebugGroup(Guid nodeId)
    {
        if (nodeId == Guid.Empty)
        {
            _logger.LogWarning("LeaveDebugGroup mit leerer nodeId aufgerufen");
            return;
        }

        var groupName = GetDebugGroupName(nodeId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug(
            "Client {ConnectionId} hat Debug-Gruppe verlassen: {NodeId}",
            Context.ConnectionId,
            nodeId);
    }

    #region Group Name Helpers

    /// <summary>
    /// Generiert den Gruppennamen für einen Tenant.
    /// Format: "tenant:{tenantId}"
    /// </summary>
    public static string GetTenantGroupName(Guid tenantId) => $"tenant:{tenantId}";

    /// <summary>
    /// Generiert den Gruppennamen für einen Hub (Raspberry Pi).
    /// Format: "hub:{hubId}"
    /// </summary>
    public static string GetHubGroupName(string hubId) => $"hub:{hubId}";

    /// <summary>
    /// Generiert den Gruppennamen für einen Node (ESP32/LoRa32).
    /// Format: "node:{nodeId}"
    /// </summary>
    public static string GetNodeGroupName(Guid nodeId) => $"node:{nodeId}";

    /// <summary>
    /// Generiert den Gruppennamen für ein Alert-Level.
    /// Format: "alerts:{level}"
    /// </summary>
    public static string GetAlertGroupName(int level) => $"alerts:{level}";

    /// <summary>
    /// Generiert den Gruppennamen für Debug-Logs eines Nodes.
    /// Format: "debug:{nodeId}"
    /// </summary>
    public static string GetDebugGroupName(Guid nodeId) => $"debug:{nodeId}";

    #endregion
}
