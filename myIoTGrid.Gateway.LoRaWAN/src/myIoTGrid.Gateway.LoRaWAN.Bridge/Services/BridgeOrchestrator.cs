using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using myIoTGrid.Gateway.LoRaWAN.Bridge.Decoders;
using myIoTGrid.Gateway.LoRaWAN.Bridge.Models.ChirpStack;
using myIoTGrid.Gateway.LoRaWAN.Bridge.Models.MyIoTGrid;

namespace myIoTGrid.Gateway.LoRaWAN.Bridge.Services;

/// <summary>
/// Bridge Orchestrator
/// Verbindet ChirpStack Subscriber mit myIoTGrid Publisher
/// Dekodiert Payloads und transformiert Events
/// </summary>
public class BridgeOrchestrator : BackgroundService
{
    private readonly ILogger<BridgeOrchestrator> _logger;
    private readonly ChirpStackSubscriber _subscriber;
    private readonly MyIoTGridPublisher _publisher;
    private readonly IPayloadDecoder _decoder;

    /// <summary>
    /// Cache für DevEUI -> NodeId Mapping
    /// Generiert deterministische GUIDs aus DevEUI
    /// </summary>
    private readonly ConcurrentDictionary<string, Guid> _nodeIdCache = new();

    /// <summary>
    /// Cache für DevEUI+SensorType -> SensorId Mapping
    /// </summary>
    private readonly ConcurrentDictionary<string, Guid> _sensorIdCache = new();

    /// <summary>
    /// Statistiken
    /// </summary>
    public DateTime StartedAt { get; } = DateTime.UtcNow;

    public BridgeOrchestrator(
        ILogger<BridgeOrchestrator> logger,
        ChirpStackSubscriber subscriber,
        MyIoTGridPublisher publisher,
        IPayloadDecoder decoder)
    {
        _logger = logger;
        _subscriber = subscriber;
        _publisher = publisher;
        _decoder = decoder;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Bridge Orchestrator starting...");

        // Wire up event handlers
        _subscriber.OnUplinkReceived += async (sender, uplink) =>
        {
            await HandleUplinkAsync(uplink, stoppingToken);
        };

        _subscriber.OnJoinReceived += async (sender, join) =>
        {
            await HandleJoinAsync(join, stoppingToken);
        };

        // Periodic status update
        var statusTimer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        try
        {
            while (await statusTimer.WaitForNextTickAsync(stoppingToken))
            {
                await PublishStatusAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
    }

    private async Task HandleUplinkAsync(UplinkEvent uplink, CancellationToken ct)
    {
        try
        {
            var devEui = uplink.DeviceInfo?.DevEui ?? "unknown";
            var data = uplink.GetDecodedData();

            if (data == null || data.Length == 0)
            {
                _logger.LogWarning("Empty payload from device {DevEui}", devEui);
                return;
            }

            // Check if decoder can handle this payload
            if (!_decoder.CanDecode(data, uplink.FPort))
            {
                _logger.LogWarning(
                    "Decoder {Decoder} cannot decode payload from {DevEui} (FPort={FPort}, Length={Length})",
                    _decoder.Name, devEui, uplink.FPort, data.Length);
                return;
            }

            // Decode payload
            var readings = _decoder.Decode(data, devEui, uplink.FPort);
            var nodeId = GetOrCreateNodeId(devEui);

            foreach (var reading in readings)
            {
                var sensorId = GetOrCreateSensorId(devEui, reading.Type);

                var message = new ReadingMessage
                {
                    NodeId = nodeId,
                    SensorId = sensorId,
                    SensorType = reading.Type,
                    Value = reading.Value,
                    Unit = reading.Unit,
                    Timestamp = reading.Timestamp,
                    Metadata = new Dictionary<string, string>
                    {
                        ["devEui"] = devEui,
                        ["devAddr"] = uplink.DevAddr ?? "",
                        ["fCnt"] = uplink.FCnt.ToString(),
                        ["fPort"] = uplink.FPort.ToString(),
                        ["rssi"] = uplink.RxInfo?.FirstOrDefault()?.Rssi.ToString() ?? "",
                        ["snr"] = uplink.RxInfo?.FirstOrDefault()?.Snr.ToString() ?? "",
                        ["gatewayId"] = uplink.RxInfo?.FirstOrDefault()?.GatewayId ?? "",
                        ["decoder"] = _decoder.Name
                    }
                };

                await _publisher.PublishReadingAsync(message, ct);

                _logger.LogInformation(
                    "Reading published: {DevEui}/{SensorType} = {Value} {Unit}",
                    devEui, reading.Type, reading.Value, reading.Unit);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling uplink");
        }
    }

    private async Task HandleJoinAsync(JoinEvent join, CancellationToken ct)
    {
        try
        {
            var devEui = join.DeviceInfo?.DevEui ?? "unknown";
            var nodeId = GetOrCreateNodeId(devEui);

            var message = new NodeJoinedMessage
            {
                NodeId = nodeId,
                DevEui = devEui,
                DevAddr = join.DevAddr,
                Name = join.DeviceInfo?.DeviceName ?? $"LoRa-{devEui[^8..]}",
                Protocol = "LoRaWAN",
                Timestamp = join.Time ?? DateTime.UtcNow,
                Metadata = new Dictionary<string, string>
                {
                    ["tenantId"] = join.DeviceInfo?.TenantId ?? "",
                    ["applicationId"] = join.DeviceInfo?.ApplicationId ?? "",
                    ["applicationName"] = join.DeviceInfo?.ApplicationName ?? "",
                    ["deviceProfileId"] = join.DeviceInfo?.DeviceProfileId ?? "",
                    ["deviceProfileName"] = join.DeviceInfo?.DeviceProfileName ?? ""
                }
            };

            await _publisher.PublishNodeJoinedAsync(message, ct);

            _logger.LogInformation(
                "Node joined: {NodeId} ({DevEui}) -> {DevAddr}",
                nodeId, devEui, join.DevAddr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling join");
        }
    }

    private async Task PublishStatusAsync(CancellationToken ct)
    {
        var status = new StatusMessage
        {
            Status = "online",
            Version = "1.0.0",
            Timestamp = DateTime.UtcNow,
            ConnectedToChirpStack = _subscriber.IsConnected,
            ConnectedToMyIoTGrid = _publisher.IsConnected,
            Statistics = new BridgeStatistics
            {
                UplinksReceived = _subscriber.UplinksReceived,
                ReadingsPublished = _publisher.ReadingsPublished,
                JoinsReceived = _subscriber.JoinsReceived,
                Errors = _subscriber.Errors + _publisher.Errors,
                LastUplinkAt = _subscriber.LastUplinkAt,
                StartedAt = StartedAt
            }
        };

        await _publisher.PublishStatusAsync(status, ct);
    }

    /// <summary>
    /// Generiert eine deterministische GUID aus DevEUI
    /// Gleiche DevEUI -> Gleiche NodeId
    /// </summary>
    private Guid GetOrCreateNodeId(string devEui)
    {
        return _nodeIdCache.GetOrAdd(devEui, key =>
        {
            // Namespace GUID für myIoTGrid LoRaWAN Nodes
            var namespaceId = new Guid("6ba7b810-9dad-11d1-80b4-00c04fd430c8"); // UUID v5 DNS namespace

            return CreateDeterministicGuid(namespaceId, $"myiotgrid:lorawan:node:{key}");
        });
    }

    /// <summary>
    /// Generiert eine deterministische GUID aus DevEUI + SensorType
    /// </summary>
    private Guid GetOrCreateSensorId(string devEui, string sensorType)
    {
        var key = $"{devEui}:{sensorType}";

        return _sensorIdCache.GetOrAdd(key, _ =>
        {
            var namespaceId = new Guid("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
            return CreateDeterministicGuid(namespaceId, $"myiotgrid:lorawan:sensor:{key}");
        });
    }

    /// <summary>
    /// Erstellt eine UUID v5 (SHA-1 basiert, deterministisch)
    /// </summary>
    private static Guid CreateDeterministicGuid(Guid namespaceId, string name)
    {
        var namespaceBytes = namespaceId.ToByteArray();
        SwapByteOrder(namespaceBytes);

        var nameBytes = Encoding.UTF8.GetBytes(name);

        var data = new byte[namespaceBytes.Length + nameBytes.Length];
        namespaceBytes.CopyTo(data, 0);
        nameBytes.CopyTo(data, namespaceBytes.Length);

        var hash = SHA1.HashData(data);

        var result = new byte[16];
        Array.Copy(hash, result, 16);

        // Version 5
        result[6] = (byte)((result[6] & 0x0F) | 0x50);

        // Variant RFC4122
        result[8] = (byte)((result[8] & 0x3F) | 0x80);

        SwapByteOrder(result);

        return new Guid(result);
    }

    private static void SwapByteOrder(byte[] guid)
    {
        // Swap time_low
        (guid[0], guid[3]) = (guid[3], guid[0]);
        (guid[1], guid[2]) = (guid[2], guid[1]);

        // Swap time_mid
        (guid[4], guid[5]) = (guid[5], guid[4]);

        // Swap time_hi_and_version
        (guid[6], guid[7]) = (guid[7], guid[6]);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bridge Orchestrator stopping...");
        await base.StopAsync(cancellationToken);
    }
}
