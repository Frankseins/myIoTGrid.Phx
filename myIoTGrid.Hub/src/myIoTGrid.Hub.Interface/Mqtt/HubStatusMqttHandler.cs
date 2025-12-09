using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Mqtt;

namespace myIoTGrid.Hub.Interface.Mqtt;

/// <summary>
/// Handler für MQTT-Nachrichten mit Hub-Status.
/// Verarbeitet Nachrichten auf dem Topic: myiotgrid/{tenantId}/hubs/{hubId}/status
/// </summary>
public partial class HubStatusMqttHandler : IMqttMessageHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HubStatusMqttHandler> _logger;

    // Regex für das Topic-Matching: myiotgrid/{tenantId}/hubs/{hubId}/status
    [GeneratedRegex(@"^myiotgrid/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})/hubs/([^/]+)/status$")]
    private static partial Regex HubStatusTopicRegex();

    public HubStatusMqttHandler(
        IServiceProvider serviceProvider,
        ILogger<HubStatusMqttHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool CanHandle(string topic)
    {
        return HubStatusTopicRegex().IsMatch(topic);
    }

    /// <inheritdoc />
    public async Task<bool> HandleMessageAsync(string topic, string payload, CancellationToken ct = default)
    {
        try
        {
            // TenantId und HubId aus Topic extrahieren
            var (tenantId, hubId) = ExtractFromTopic(topic);
            if (tenantId == null || string.IsNullOrWhiteSpace(hubId))
            {
                _logger.LogWarning("Konnte TenantId/HubId nicht aus Topic extrahieren: {Topic}", topic);
                return false;
            }

            // Status aus Payload parsen (erwartet: "online" oder "offline")
            var isOnline = payload.Trim().ToLowerInvariant() switch
            {
                "online" or "1" or "true" => true,
                "offline" or "0" or "false" => false,
                _ => (bool?)null
            };

            if (isOnline == null)
            {
                _logger.LogWarning("Ungültiger Status-Payload: {Payload}", payload);
                return false;
            }

            // Service aus DI Container holen (Scoped Service)
            using var scope = _serviceProvider.CreateScope();
            var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
            var hubService = scope.ServiceProvider.GetRequiredService<IHubService>();
            var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();

            // TenantId setzen für den Scope
            tenantService.SetCurrentTenantId(tenantId.Value);

            // Hub ermitteln oder erstellen
            var hubDto = await hubService.GetOrCreateByHubIdAsync(hubId, ct);

            // Status aktualisieren
            await hubService.SetOnlineStatusAsync(hubDto.Id, isOnline.Value, ct);

            // Bei Offline: Hub-Offline-Alert erstellen (für alle Sensoren des Hubs)
            if (!isOnline.Value)
            {
                // Hub-Level Alert erstellen
                await alertService.CreateHubOfflineAlertAsync(hubDto.Id, ct);
            }
            else
            {
                // Bei Online: Bestehende Hub-Offline-Alerts deaktivieren
                await alertService.DeactivateHubAlertsAsync(hubDto.Id, "hub_offline", ct);
            }

            _logger.LogInformation(
                "MQTT Hub-Status verarbeitet: {HubId} -> {Status} (Tenant: {TenantId})",
                hubId,
                isOnline.Value ? "online" : "offline",
                tenantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Verarbeiten der Hub-Status-Nachricht: {Topic}", topic);
            return false;
        }
    }

    /// <summary>
    /// Extrahiert TenantId und HubId aus dem Topic.
    /// Topic-Format: myiotgrid/{tenantId}/hubs/{hubId}/status
    /// </summary>
    private (Guid? TenantId, string? HubId) ExtractFromTopic(string topic)
    {
        var match = HubStatusTopicRegex().Match(topic);
        if (!match.Success || match.Groups.Count < 3)
        {
            return (null, null);
        }

        if (!Guid.TryParse(match.Groups[1].Value, out var tenantId))
        {
            return (null, null);
        }

        var hubId = match.Groups[2].Value;
        return (tenantId, hubId);
    }
}
