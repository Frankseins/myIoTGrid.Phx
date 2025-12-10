using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using myIoTGrid.Shared.Common.DTOs;
using myIoTGrid.Shared.Contracts.Services;

namespace myIoTGrid.Hub.Service.Adapters;

/// <summary>
/// MQTT Adapter for LoRaWAN Gateway Integration
/// Receives readings from myIoTGrid.Gateway.LoRaWAN.Bridge
/// and stores them via ReadingService
/// </summary>
public class MqttLoRaWanAdapter : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MqttLoRaWanAdapter> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;

    /// <summary>
    /// Connection State
    /// </summary>
    public bool IsConnected => _client.IsConnected;

    /// <summary>
    /// Statistics
    /// </summary>
    public long ReadingsReceived { get; private set; }
    public long NodesJoinedReceived { get; private set; }
    public long Errors { get; private set; }
    public DateTime? LastReadingAt { get; private set; }
    public DateTime StartedAt { get; } = DateTime.UtcNow;

    public MqttLoRaWanAdapter(
        IServiceProvider serviceProvider,
        ILogger<MqttLoRaWanAdapter> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;

        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        var mqttServer = _configuration["LoRaWAN:MqttServer"] ?? "tcp://localhost:1884";
        var clientId = _configuration["LoRaWAN:ClientId"] ?? "myiotgrid-hub";

        // Parse server URL
        var serverUri = new Uri(mqttServer.Replace("tcp://", "mqtt://"));

        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(serverUri.Host, serverUri.Port > 0 ? serverUri.Port : 1884)
            .WithClientId($"{clientId}-lorawan-{Guid.NewGuid():N}")
            .WithCleanSession(false)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
            .WithTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue("LoRaWAN:Enabled", true);
        if (!enabled)
        {
            _logger.LogInformation("LoRaWAN MQTT Adapter is disabled");
            return;
        }

        _logger.LogInformation("LoRaWAN MQTT Adapter starting...");

        // Setup disconnect handler for reconnection
        _client.DisconnectedAsync += async e =>
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            _logger.LogWarning(
                "LoRaWAN MQTT disconnected: {Reason}. Reconnecting in 5s...",
                e.Reason);

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            try
            {
                await ConnectAndSubscribeAsync(stoppingToken);
                _logger.LogInformation("LoRaWAN MQTT reconnected successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoRaWAN MQTT reconnect failed");
            }
        };

        // Setup message handler
        _client.ApplicationMessageReceivedAsync += HandleMessageAsync;

        // Initial connect with retry
        var connected = false;
        var retryCount = 0;
        const int maxRetries = 5;

        while (!connected && !stoppingToken.IsCancellationRequested && retryCount < maxRetries)
        {
            try
            {
                await ConnectAndSubscribeAsync(stoppingToken);
                connected = true;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex,
                    "LoRaWAN MQTT initial connection failed (attempt {Attempt}/{Max}). Retrying in 10s...",
                    retryCount, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        if (!connected)
        {
            _logger.LogError("LoRaWAN MQTT Adapter could not connect after {Max} attempts. Continuing without LoRaWAN support.",
                maxRetries);
        }

        // Keep service running
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
    }

    private async Task ConnectAndSubscribeAsync(CancellationToken ct)
    {
        var mqttServer = _configuration["LoRaWAN:MqttServer"] ?? "tcp://localhost:1884";

        // Connect
        await _client.ConnectAsync(_options, ct);
        _logger.LogInformation("LoRaWAN MQTT connected to {Server}", mqttServer);

        // Subscribe to reading topics
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(f => f
                .WithTopic("myiotgrid/readings/#")
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce))
            .WithTopicFilter(f => f
                .WithTopic("myiotgrid/nodes/joined")
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce))
            .WithTopicFilter(f => f
                .WithTopic("myiotgrid/status/gateway-lorawan")
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce))
            .Build();

        await _client.SubscribeAsync(subscribeOptions, ct);

        _logger.LogInformation(
            "LoRaWAN MQTT subscribed to myiotgrid/readings/#, myiotgrid/nodes/joined, myiotgrid/status/gateway-lorawan");
    }

    private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

            _logger.LogDebug("LoRaWAN MQTT message received on {Topic}", topic);

            if (topic.StartsWith("myiotgrid/readings/"))
            {
                await HandleReadingAsync(topic, payload);
            }
            else if (topic == "myiotgrid/nodes/joined")
            {
                await HandleNodeJoinedAsync(payload);
            }
            else if (topic == "myiotgrid/status/gateway-lorawan")
            {
                HandleGatewayStatus(payload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing LoRaWAN MQTT message");
            Errors++;
        }
    }

    private async Task HandleReadingAsync(string topic, string payload)
    {
        try
        {
            var message = JsonSerializer.Deserialize<LoRaWanReadingMessage>(payload);
            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize reading message from {Topic}", topic);
                Errors++;
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var readingService = scope.ServiceProvider.GetRequiredService<IReadingService>();

            // Convert LoRaWAN message to CreateSensorReadingDto
            var dto = new CreateSensorReadingDto(
                DeviceId: message.NodeId.ToString(),
                Type: message.SensorType,
                Value: message.Value,
                Unit: message.Unit,
                Timestamp: new DateTimeOffset(message.Timestamp).ToUnixTimeSeconds()
            );

            var reading = await readingService.CreateFromSensorAsync(dto);

            ReadingsReceived++;
            LastReadingAt = DateTime.UtcNow;

            _logger.LogDebug(
                "LoRaWAN reading saved: {NodeId}/{SensorType} = {Value} {Unit}",
                message.NodeId, message.SensorType, message.Value, message.Unit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling LoRaWAN reading");
            Errors++;
        }
    }

    private async Task HandleNodeJoinedAsync(string payload)
    {
        try
        {
            var message = JsonSerializer.Deserialize<LoRaWanNodeJoinedMessage>(payload);
            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize node joined message");
                Errors++;
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var nodeService = scope.ServiceProvider.GetRequiredService<INodeService>();

            // Check if node already exists
            var existingNode = await nodeService.GetByIdAsync(message.NodeId);
            if (existingNode != null)
            {
                _logger.LogInformation(
                    "LoRaWAN node {NodeId} already exists, skipping registration",
                    message.NodeId);
                return;
            }

            // Note: Node auto-registration happens when the first reading is received
            // via ReadingService.CreateFromSensorAsync which calls NodeService.GetOrCreateByNodeIdAsync
            // So we just log the join event here

            NodesJoinedReceived++;

            _logger.LogInformation(
                "LoRaWAN node joined: {NodeId} ({DevEui}) -> {DevAddr}",
                message.NodeId, message.DevEui, message.DevAddr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling LoRaWAN node joined");
            Errors++;
        }
    }

    private void HandleGatewayStatus(string payload)
    {
        try
        {
            var message = JsonSerializer.Deserialize<LoRaWanGatewayStatusMessage>(payload);
            if (message == null) return;

            _logger.LogInformation(
                "LoRaWAN Gateway status: {Status} (ChirpStack: {ChirpStack}, MyIoTGrid: {MyIoTGrid})",
                message.Status, message.ConnectedToChirpStack, message.ConnectedToMyIoTGrid);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error parsing gateway status");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("LoRaWAN MQTT Adapter stopping...");

        if (_client.IsConnected)
        {
            await _client.DisconnectAsync(cancellationToken: cancellationToken);
        }

        _client.Dispose();

        await base.StopAsync(cancellationToken);
    }
}

#region DTO Classes for LoRaWAN Messages

/// <summary>
/// Reading message from LoRaWAN Gateway Bridge
/// </summary>
internal class LoRaWanReadingMessage
{
    [JsonPropertyName("nodeId")]
    public Guid NodeId { get; set; }

    [JsonPropertyName("sensorId")]
    public Guid SensorId { get; set; }

    [JsonPropertyName("sensorType")]
    public string SensorType { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public double Value { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Node joined message from LoRaWAN Gateway Bridge
/// </summary>
internal class LoRaWanNodeJoinedMessage
{
    [JsonPropertyName("nodeId")]
    public Guid NodeId { get; set; }

    [JsonPropertyName("devEui")]
    public string DevEui { get; set; } = string.Empty;

    [JsonPropertyName("devAddr")]
    public string? DevAddr { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = "LoRaWAN";

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Gateway status message from LoRaWAN Gateway Bridge
/// </summary>
internal class LoRaWanGatewayStatusMessage
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("connectedToChirpStack")]
    public bool ConnectedToChirpStack { get; set; }

    [JsonPropertyName("connectedToMyIoTGrid")]
    public bool ConnectedToMyIoTGrid { get; set; }
}

#endregion
