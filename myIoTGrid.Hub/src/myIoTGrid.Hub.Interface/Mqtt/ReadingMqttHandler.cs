using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Mqtt;

namespace myIoTGrid.Hub.Interface.Mqtt;

/// <summary>
/// Handler für MQTT-Nachrichten mit Readings (Messwerten).
/// Verarbeitet Nachrichten auf dem Topic: myiotgrid/{tenantId}/readings
/// Matter-konform: Entspricht Attribute Reports.
/// </summary>
public partial class ReadingMqttHandler : IMqttMessageHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReadingMqttHandler> _logger;

    // Regex für das Topic-Matching: myiotgrid/{tenantId}/readings
    [GeneratedRegex(@"^myiotgrid/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})/readings$")]
    private static partial Regex ReadingTopicRegex();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ReadingMqttHandler(
        IServiceProvider serviceProvider,
        ILogger<ReadingMqttHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool CanHandle(string topic)
    {
        return ReadingTopicRegex().IsMatch(topic);
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
            var dto = JsonSerializer.Deserialize<CreateReadingDto>(payload, JsonOptions);
            if (dto == null)
            {
                _logger.LogWarning("Konnte Payload nicht deserialisieren: {Payload}", payload);
                return false;
            }

            // Validierung
            if (string.IsNullOrWhiteSpace(dto.NodeId))
            {
                _logger.LogWarning("NodeId fehlt im Payload");
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.MeasurementType))
            {
                _logger.LogWarning("MeasurementType fehlt im Payload");
                return false;
            }

            if (dto.EndpointId <= 0)
            {
                _logger.LogWarning("EndpointId fehlt oder ungültig im Payload");
                return false;
            }

            // Service aus DI Container holen (Scoped Service)
            using var scope = _serviceProvider.CreateScope();
            var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
            var readingService = scope.ServiceProvider.GetRequiredService<IReadingService>();

            // TenantId setzen für den Scope
            tenantService.SetCurrentTenantId(tenantId.Value);

            // Reading erstellen
            var reading = await readingService.CreateAsync(dto, ct);

            _logger.LogDebug(
                "MQTT Reading verarbeitet: Endpoint={EndpointId} {MeasurementType}={RawValue} von Node {NodeId} (Tenant: {TenantId})",
                dto.EndpointId,
                dto.MeasurementType,
                dto.RawValue,
                dto.NodeId,
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
    /// Topic-Format: myiotgrid/{tenantId}/readings
    /// </summary>
    private Guid? ExtractTenantIdFromTopic(string topic)
    {
        var match = ReadingTopicRegex().Match(topic);
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
