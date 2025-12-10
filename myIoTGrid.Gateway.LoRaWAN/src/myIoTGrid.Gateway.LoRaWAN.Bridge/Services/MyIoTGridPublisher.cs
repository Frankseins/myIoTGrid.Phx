using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using myIoTGrid.Gateway.LoRaWAN.Bridge.Models.MyIoTGrid;

namespace myIoTGrid.Gateway.LoRaWAN.Bridge.Services;

/// <summary>
/// myIoTGrid MQTT Publisher
/// Publiziert transformierte Readings an myIoTGrid.Hub via MQTT
/// </summary>
public class MyIoTGridPublisher : BackgroundService
{
    private readonly ILogger<MyIoTGridPublisher> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;

    /// <summary>
    /// Connection State
    /// </summary>
    public bool IsConnected => _client.IsConnected;

    /// <summary>
    /// Statistiken
    /// </summary>
    public long ReadingsPublished { get; private set; }
    public long NodesJoinedPublished { get; private set; }
    public long Errors { get; private set; }

    public MyIoTGridPublisher(
        ILogger<MyIoTGridPublisher> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        var mqttServer = _configuration["MyIoTGrid:MqttServer"] ?? "tcp://mosquitto:1884";
        var clientId = _configuration["MyIoTGrid:ClientId"] ?? "myiotgrid-gateway-lorawan";

        // Parse server URL
        var serverUri = new Uri(mqttServer.Replace("tcp://", "mqtt://"));

        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(serverUri.Host, serverUri.Port > 0 ? serverUri.Port : 1884)
            .WithClientId($"{clientId}-publisher-{Guid.NewGuid():N}")
            .WithCleanSession(true)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
            .WithTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("myIoTGrid Publisher starting...");

        // Setup disconnect handler for reconnection
        _client.DisconnectedAsync += async e =>
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            _logger.LogWarning(
                "myIoTGrid MQTT disconnected: {Reason}. Reconnecting in 5s...",
                e.Reason);

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            try
            {
                await ConnectAsync(stoppingToken);
                _logger.LogInformation("myIoTGrid MQTT reconnected successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "myIoTGrid MQTT reconnect failed");
            }
        };

        // Initial connect
        await ConnectAsync(stoppingToken);

        // Publish online status
        await PublishStatusAsync(new StatusMessage
        {
            Status = "online",
            Version = "1.0.0",
            Timestamp = DateTime.UtcNow,
            ConnectedToChirpStack = false,
            ConnectedToMyIoTGrid = true
        }, stoppingToken);

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

    private async Task ConnectAsync(CancellationToken ct)
    {
        var mqttServer = _configuration["MyIoTGrid:MqttServer"] ?? "tcp://mosquitto:1884";

        await _client.ConnectAsync(_options, ct);
        _logger.LogInformation("myIoTGrid MQTT connected to {Server}", mqttServer);
    }

    /// <summary>
    /// Publiziert ein Sensor Reading
    /// </summary>
    public async Task PublishReadingAsync(ReadingMessage message, CancellationToken ct = default)
    {
        try
        {
            var topic = $"myiotgrid/readings/{message.NodeId}/{message.SensorType}";
            var payload = JsonSerializer.Serialize(message);

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(false)
                .Build();

            await _client.PublishAsync(mqttMessage, ct);
            ReadingsPublished++;

            _logger.LogDebug(
                "Reading published to {Topic}: {Type} = {Value} {Unit}",
                topic, message.SensorType, message.Value, message.Unit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing reading");
            Errors++;
        }
    }

    /// <summary>
    /// Publiziert eine Node Joined Nachricht
    /// </summary>
    public async Task PublishNodeJoinedAsync(NodeJoinedMessage message, CancellationToken ct = default)
    {
        try
        {
            var topic = "myiotgrid/nodes/joined";
            var payload = JsonSerializer.Serialize(message);

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(true) // Retain für neue Subscriber
                .Build();

            await _client.PublishAsync(mqttMessage, ct);
            NodesJoinedPublished++;

            _logger.LogInformation(
                "Node joined published: {NodeId} ({DevEui})",
                message.NodeId, message.DevEui);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing node joined");
            Errors++;
        }
    }

    /// <summary>
    /// Publiziert den Gateway Status
    /// </summary>
    public async Task PublishStatusAsync(StatusMessage message, CancellationToken ct = default)
    {
        try
        {
            var topic = "myiotgrid/status/gateway-lorawan";
            var payload = JsonSerializer.Serialize(message);

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(true)
                .Build();

            await _client.PublishAsync(mqttMessage, ct);

            _logger.LogDebug("Status published: {Status}", message.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing status");
            Errors++;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("myIoTGrid Publisher stopping...");

        // Publish offline status
        if (_client.IsConnected)
        {
            try
            {
                await PublishStatusAsync(new StatusMessage
                {
                    Status = "offline",
                    Version = "1.0.0",
                    Timestamp = DateTime.UtcNow,
                    ConnectedToChirpStack = false,
                    ConnectedToMyIoTGrid = false
                }, cancellationToken);
            }
            catch
            {
                // Ignore errors during shutdown
            }

            await _client.DisconnectAsync(cancellationToken: cancellationToken);
        }

        _client.Dispose();

        await base.StopAsync(cancellationToken);
    }
}
