using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Mqtt;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.Constants;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Interface.Mqtt;

/// <summary>
/// Handler für MQTT-Nachrichten mit Sensordaten.
/// Verarbeitet Nachrichten auf dem Topic: myiotgrid/{tenantId}/sensordata
/// </summary>
public partial class SensorDataMqttHandler : IMqttMessageHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SensorDataMqttHandler> _logger;

    // Regex für das Topic-Matching: myiotgrid/{tenantId}/sensordata
    [GeneratedRegex(@"^myiotgrid/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})/sensordata$")]
    private static partial Regex SensorDataTopicRegex();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SensorDataMqttHandler(
        IServiceProvider serviceProvider,
        ILogger<SensorDataMqttHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool CanHandle(string topic)
    {
        return SensorDataTopicRegex().IsMatch(topic);
    }

    /// <inheritdoc />
    public async Task<bool> HandleMessageAsync(string topic, string payload, CancellationToken ct = default)
    {
        try
        {
            // TenantId aus Topic extrahieren
            var tenantId = ExtractTenantIdFromTopic(topic);
            if (tenantId == null)
            {
                _logger.LogWarning("Konnte TenantId nicht aus Topic extrahieren: {Topic}", topic);
                return false;
            }

            // Payload deserialisieren
            var dto = JsonSerializer.Deserialize<CreateSensorDataDto>(payload, JsonOptions);
            if (dto == null)
            {
                _logger.LogWarning("Konnte Payload nicht deserialisieren: {Payload}", payload);
                return false;
            }

            // Validierung
            if (string.IsNullOrWhiteSpace(dto.HubId))
            {
                _logger.LogWarning("HubId fehlt im Payload");
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.SensorType))
            {
                _logger.LogWarning("SensorType fehlt im Payload");
                return false;
            }

            // Service aus DI Container holen (Scoped Service)
            using var scope = _serviceProvider.CreateScope();
            var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
            var sensorDataService = scope.ServiceProvider.GetRequiredService<ISensorDataService>();

            // TenantId setzen für den Scope
            tenantService.SetCurrentTenantId(tenantId.Value);

            // SensorData erstellen
            var sensorData = await sensorDataService.CreateAsync(dto, ct);

            _logger.LogDebug(
                "MQTT SensorData verarbeitet: {SensorType}={Value} von {HubId} (Tenant: {TenantId})",
                dto.SensorType,
                dto.Value,
                dto.HubId,
                tenantId);

            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON-Fehler beim Verarbeiten der MQTT-Nachricht: {Payload}", payload);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Verarbeiten der MQTT-Nachricht: {Topic}", topic);
            return false;
        }
    }

    /// <summary>
    /// Extrahiert die TenantId aus dem Topic.
    /// Topic-Format: myiotgrid/{tenantId}/sensordata
    /// </summary>
    private Guid? ExtractTenantIdFromTopic(string topic)
    {
        var match = SensorDataTopicRegex().Match(topic);
        if (!match.Success || match.Groups.Count < 2)
        {
            return null;
        }

        if (Guid.TryParse(match.Groups[1].Value, out var tenantId))
        {
            return tenantId;
        }

        return null;
    }
}
