using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using myIoTGrid.Gateway.LoRaWAN.Bridge.Models.ChirpStack;

namespace myIoTGrid.Gateway.LoRaWAN.Bridge.Services;

/// <summary>
/// ChirpStack MQTT Subscriber
/// Empfängt Uplink und Join Events von ChirpStack via MQTT
/// </summary>
public class ChirpStackSubscriber : BackgroundService
{
    private readonly ILogger<ChirpStackSubscriber> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;

    /// <summary>
    /// Event wenn ein Uplink empfangen wird
    /// </summary>
    public event EventHandler<UplinkEvent>? OnUplinkReceived;

    /// <summary>
    /// Event wenn ein Join empfangen wird
    /// </summary>
    public event EventHandler<JoinEvent>? OnJoinReceived;

    /// <summary>
    /// Connection State
    /// </summary>
    public bool IsConnected => _client.IsConnected;

    /// <summary>
    /// Statistiken
    /// </summary>
    public long UplinksReceived { get; private set; }
    public long JoinsReceived { get; private set; }
    public long Errors { get; private set; }
    public DateTime? LastUplinkAt { get; private set; }

    public ChirpStackSubscriber(
        ILogger<ChirpStackSubscriber> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        var mqttServer = _configuration["ChirpStack:MqttServer"] ?? "tcp://mosquitto:1883";
        var clientId = _configuration["ChirpStack:ClientId"] ?? "myiotgrid-bridge";

        // Parse server URL
        var serverUri = new Uri(mqttServer.Replace("tcp://", "mqtt://"));

        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(serverUri.Host, serverUri.Port > 0 ? serverUri.Port : 1883)
            .WithClientId($"{clientId}-subscriber-{Guid.NewGuid():N}")
            .WithCleanSession(true)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
            .WithTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ChirpStack Subscriber starting...");

        // Setup disconnect handler for reconnection
        _client.DisconnectedAsync += async e =>
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            _logger.LogWarning(
                "ChirpStack MQTT disconnected: {Reason}. Reconnecting in 5s...",
                e.Reason);

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            try
            {
                await ConnectAndSubscribeAsync(stoppingToken);
                _logger.LogInformation("ChirpStack MQTT reconnected successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChirpStack MQTT reconnect failed");
            }
        };

        // Setup message handler
        _client.ApplicationMessageReceivedAsync += HandleMessageAsync;

        // Initial connect
        await ConnectAndSubscribeAsync(stoppingToken);

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
        var mqttServer = _configuration["ChirpStack:MqttServer"] ?? "tcp://mosquitto:1883";

        // Connect
        await _client.ConnectAsync(_options, ct);
        _logger.LogInformation("ChirpStack MQTT connected to {Server}", mqttServer);

        // Subscribe to topics
        var uplinkTopic = _configuration["ChirpStack:Topics:Uplink"] ?? "application/+/device/+/event/up";
        var joinTopic = _configuration["ChirpStack:Topics:Join"] ?? "application/+/device/+/event/join";

        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(f => f
                .WithTopic(uplinkTopic)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce))
            .WithTopicFilter(f => f
                .WithTopic(joinTopic)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce))
            .Build();

        await _client.SubscribeAsync(subscribeOptions, ct);

        _logger.LogInformation(
            "ChirpStack MQTT subscribed to {Uplink} and {Join}",
            uplinkTopic, joinTopic);
    }

    private Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

            _logger.LogDebug("ChirpStack MQTT message received on {Topic}", topic);

            // Topic parsen: application/{appId}/device/{devEui}/event/{type}
            var parts = topic.Split('/');
            if (parts.Length < 6)
            {
                _logger.LogWarning("Invalid topic format: {Topic}", topic);
                Errors++;
                return Task.CompletedTask;
            }

            var eventType = parts[5]; // "up" oder "join"

            if (eventType == "up")
            {
                var uplink = JsonSerializer.Deserialize<UplinkEvent>(payload);
                if (uplink != null)
                {
                    var devEui = uplink.DeviceInfo?.DevEui ?? parts[3];
                    var dataLength = uplink.GetDecodedData()?.Length ?? 0;

                    _logger.LogInformation(
                        "Uplink received from {DevEui}: FCnt={FCnt}, FPort={FPort}, Data={DataLength} bytes",
                        devEui, uplink.FCnt, uplink.FPort, dataLength);

                    UplinksReceived++;
                    LastUplinkAt = DateTime.UtcNow;

                    OnUplinkReceived?.Invoke(this, uplink);
                }
            }
            else if (eventType == "join")
            {
                var join = JsonSerializer.Deserialize<JoinEvent>(payload);
                if (join != null)
                {
                    var devEui = join.DeviceInfo?.DevEui ?? parts[3];

                    _logger.LogInformation(
                        "Join received from {DevEui}, DevAddr={DevAddr}",
                        devEui, join.DevAddr);

                    JoinsReceived++;

                    OnJoinReceived?.Invoke(this, join);
                }
            }
            else
            {
                _logger.LogDebug("Ignoring event type: {EventType}", eventType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ChirpStack MQTT message");
            Errors++;
        }

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ChirpStack Subscriber stopping...");

        if (_client.IsConnected)
        {
            await _client.DisconnectAsync(cancellationToken: cancellationToken);
        }

        _client.Dispose();

        await base.StopAsync(cancellationToken);
    }
}
