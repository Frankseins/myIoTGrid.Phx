using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using myIoTGrid.Shared.Common.Constants;

namespace myIoTGrid.Hub.Infrastructure.Mqtt;

/// <summary>
/// BackgroundService für die MQTT-Verbindung.
/// Verbindet sich zum MQTT-Broker, abonniert Topics und verarbeitet eingehende Nachrichten.
/// </summary>
public class MqttClientService : BackgroundService
{
    private readonly MqttOptions _options;
    private readonly IEnumerable<IMqttMessageHandler> _handlers;
    private readonly ILogger<MqttClientService> _logger;
    private IMqttClient? _mqttClient;
    private int _reconnectAttempts;
    private bool _isConnected;

    public MqttClientService(
        IOptions<MqttOptions> options,
        IEnumerable<IMqttMessageHandler> handlers,
        ILogger<MqttClientService> logger)
    {
        _options = options.Value;
        _handlers = handlers;
        _logger = logger;
    }

    /// <summary>
    /// Gibt an, ob der Client verbunden ist
    /// </summary>
    public bool IsConnected => _isConnected && (_mqttClient?.IsConnected ?? false);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MQTT Client Service wird gestartet...");

        await ConnectAsync(stoppingToken);

        // Warten auf Abbruch
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MQTT Client Service wird gestoppt...");

        if (_mqttClient is { IsConnected: true })
        {
            await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptions
            {
                Reason = MqttClientDisconnectOptionsReason.NormalDisconnection
            }, cancellationToken);
        }

        _mqttClient?.Dispose();
        await base.StopAsync(cancellationToken);
    }

    private async Task ConnectAsync(CancellationToken ct)
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        // Event-Handler registrieren
        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _mqttClient.ConnectedAsync += OnConnectedAsync;
        _mqttClient.DisconnectedAsync += e => OnDisconnectedAsync(e, ct);

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.Host, _options.Port)
            .WithClientId($"{_options.ClientId}-{Environment.MachineName}")
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(_options.KeepAliveSeconds))
            .WithCleanSession(true);

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            options.WithCredentials(_options.Username, _options.Password);
        }

        if (_options.UseTls)
        {
            options.WithTlsOptions(o => o.UseTls(true));
        }

        await ConnectWithRetryAsync(options.Build(), ct);
    }

    private async Task ConnectWithRetryAsync(MqttClientOptions options, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _reconnectAttempts < _options.MaxReconnectAttempts)
        {
            try
            {
                _logger.LogInformation(
                    "Verbinde zu MQTT Broker {Host}:{Port}...",
                    _options.Host,
                    _options.Port);

                await _mqttClient!.ConnectAsync(options, ct);
                _reconnectAttempts = 0;
                _isConnected = true;
                return;
            }
            catch (Exception ex)
            {
                _reconnectAttempts++;
                _logger.LogWarning(
                    ex,
                    "MQTT-Verbindung fehlgeschlagen (Versuch {Attempt}/{Max}). Nächster Versuch in {Delay}s...",
                    _reconnectAttempts,
                    _options.MaxReconnectAttempts,
                    _options.ReconnectDelaySeconds);

                if (_reconnectAttempts >= _options.MaxReconnectAttempts)
                {
                    _logger.LogError("Maximale Anzahl an Reconnect-Versuchen erreicht. MQTT-Verbindung aufgegeben.");
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(_options.ReconnectDelaySeconds), ct);
            }
        }
    }

    private async Task OnConnectedAsync(MqttClientConnectedEventArgs e)
    {
        _logger.LogInformation("Mit MQTT Broker verbunden");
        _isConnected = true;

        // Topics abonnieren
        await SubscribeToTopicsAsync();
    }

    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e, CancellationToken ct)
    {
        _isConnected = false;

        if (e.Exception != null)
        {
            _logger.LogWarning(e.Exception, "MQTT-Verbindung unterbrochen");
        }
        else
        {
            _logger.LogInformation("MQTT-Verbindung getrennt. Grund: {Reason}", e.Reason);
        }

        // Nur reconnecten wenn nicht absichtlich getrennt
        if (e.Reason != MqttClientDisconnectReason.NormalDisconnection && !ct.IsCancellationRequested)
        {
            _logger.LogInformation("Reconnecting in {Delay}s...", _options.ReconnectDelaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(_options.ReconnectDelaySeconds), ct);

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_options.Host, _options.Port)
                .WithClientId($"{_options.ClientId}-{Environment.MachineName}")
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(_options.KeepAliveSeconds))
                .WithCleanSession(true)
                .Build();

            await ConnectWithRetryAsync(options, ct);
        }
    }

    private async Task SubscribeToTopicsAsync()
    {
        if (_mqttClient == null || !_mqttClient.IsConnected) return;

        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            // Alle SensorData für alle Tenants
            .WithTopicFilter($"{MqttTopics.Prefix}/+/sensordata")
            // Alle Hub-Status für alle Tenants
            .WithTopicFilter($"{MqttTopics.Prefix}/+/hubs/+/status")
            // Optional: ChirpStack LoRaWAN
            .WithTopicFilter(MqttTopics.ChirpStackUplink)
            .Build();

        await _mqttClient.SubscribeAsync(subscribeOptions);

        _logger.LogInformation(
            "Subscribed to MQTT topics: {Topics}",
            string.Join(", ", subscribeOptions.TopicFilters.Select(f => f.Topic)));
    }

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

        _logger.LogDebug("MQTT Nachricht empfangen: {Topic}", topic);

        // Handler suchen und ausführen
        foreach (var handler in _handlers)
        {
            if (handler.CanHandle(topic))
            {
                try
                {
                    var success = await handler.HandleMessageAsync(topic, payload);
                    if (success)
                    {
                        _logger.LogDebug("Nachricht erfolgreich verarbeitet von {Handler}", handler.GetType().Name);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler in Handler {Handler}", handler.GetType().Name);
                }
            }
        }

        _logger.LogDebug("Kein Handler für Topic gefunden: {Topic}", topic);
    }
}
