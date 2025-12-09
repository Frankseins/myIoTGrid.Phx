using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace myIoTGrid.Hub.Interface.BackgroundServices;

/// <summary>
/// Background service that listens for UDP discovery broadcasts from sensors.
/// Responds with hub information so sensors can automatically find and connect to the hub.
/// </summary>
public class DiscoveryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscoveryOptions _options;
    private readonly ILogger<DiscoveryService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public DiscoveryService(
        IServiceScopeFactory scopeFactory,
        IOptions<DiscoveryOptions> options,
        ILogger<DiscoveryService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Discovery service is disabled");
            return;
        }

        _logger.LogInformation(
            "DiscoveryService started. Listening on UDP port {Port}",
            _options.Port);

        using var udpClient = CreateUdpClient();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReceiveAndRespondAsync(udpClient, stoppingToken);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                // Normal timeout, continue listening
            }
            catch (OperationCanceledException)
            {
                // Service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in discovery service");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("DiscoveryService stopped");
    }

    private UdpClient CreateUdpClient()
    {
        var udpClient = new UdpClient(_options.Port);
        udpClient.Client.ReceiveTimeout = _options.ReceiveTimeoutMs;

        // Enable broadcast
        udpClient.EnableBroadcast = true;

        // Bind to specific interface if configured
        if (!string.IsNullOrEmpty(_options.NetworkInterface))
        {
            _logger.LogInformation(
                "Binding to network interface: {Interface}",
                _options.NetworkInterface);
        }

        return udpClient;
    }

    private async Task ReceiveAndRespondAsync(UdpClient udpClient, CancellationToken ct)
    {
        try
        {
            var result = await udpClient.ReceiveAsync(ct);
            var message = Encoding.UTF8.GetString(result.Buffer);

            if (_options.LogDiscoveryRequests)
            {
                _logger.LogDebug(
                    "Received UDP message from {RemoteEndPoint}: {Message}",
                    result.RemoteEndPoint,
                    message);
            }

            // Try to parse the discovery request
            DiscoveryRequestDto? request;
            try
            {
                request = JsonSerializer.Deserialize<DiscoveryRequestDto>(message, _jsonOptions);
            }
            catch (JsonException)
            {
                _logger.LogWarning(
                    "Invalid JSON in discovery request from {RemoteEndPoint}",
                    result.RemoteEndPoint);
                return;
            }

            if (request == null || !request.IsValid)
            {
                _logger.LogWarning(
                    "Invalid discovery request from {RemoteEndPoint}: MessageType={MessageType}",
                    result.RemoteEndPoint,
                    request?.MessageType ?? "null");
                return;
            }

            _logger.LogInformation(
                "Discovery request from sensor: Serial={Serial}, FW={FirmwareVersion}, HW={HardwareType}, IP={RemoteIP}",
                request.Serial,
                request.FirmwareVersion,
                request.HardwareType,
                result.RemoteEndPoint.Address);

            // Build and send response
            var response = await BuildResponseAsync(result.RemoteEndPoint.Address, ct);
            var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);

            await udpClient.SendAsync(responseBytes, result.RemoteEndPoint, ct);

            _logger.LogInformation(
                "Sent discovery response to {RemoteEndPoint}: ApiUrl={ApiUrl}",
                result.RemoteEndPoint,
                response.ApiUrl);
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
        {
            // Rethrow timeout to be handled by caller
            throw;
        }
    }

    private async Task<DiscoveryResponseDto> BuildResponseAsync(IPAddress senderIp, CancellationToken ct)
    {
        // Get hub information
        string hubId = _options.HubId ?? "default-hub";
        string hubName = _options.HubName ?? "myIoTGrid Hub";

        // Try to get hub info from database
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var hubService = scope.ServiceProvider.GetRequiredService<IHubService>();
            var hub = await hubService.GetCurrentHubAsync(ct);

            hubId = hub.HubId;
            hubName = hub.Name;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get hub info from database, using defaults");
        }

        // Determine the IP address to advertise
        var advertiseIp = _options.AdvertiseIp ?? GetBestLocalIp(senderIp);

        // Build the API URL
        var apiUrl = $"{_options.Protocol}://{advertiseIp}:{_options.ApiPort}";

        return DiscoveryResponseDto.Create(hubId, hubName, apiUrl);
    }

    /// <summary>
    /// Finds the best local IP address to advertise to the sensor.
    /// Prefers an IP in the same subnet as the sender.
    /// </summary>
    private string GetBestLocalIp(IPAddress senderIp)
    {
        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .ToList();

            // Get all local IPv4 addresses
            var localAddresses = networkInterfaces
                .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)
                .ToList();

            // Try to find an address in the same subnet as the sender
            foreach (var addr in localAddresses)
            {
                if (IsInSameSubnet(senderIp, addr.Address, addr.IPv4Mask))
                {
                    _logger.LogDebug(
                        "Selected local IP {LocalIp} (same subnet as {SenderIp})",
                        addr.Address,
                        senderIp);
                    return addr.Address.ToString();
                }
            }

            // Fallback: Return the first non-loopback IPv4 address
            var fallbackAddr = localAddresses.FirstOrDefault()?.Address;
            if (fallbackAddr != null)
            {
                _logger.LogDebug(
                    "Using fallback local IP {LocalIp}",
                    fallbackAddr);
                return fallbackAddr.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error determining local IP address");
        }

        // Last resort: Return localhost
        _logger.LogWarning("Could not determine local IP, using localhost");
        return "127.0.0.1";
    }

    private static bool IsInSameSubnet(IPAddress address1, IPAddress address2, IPAddress subnetMask)
    {
        var addr1Bytes = address1.GetAddressBytes();
        var addr2Bytes = address2.GetAddressBytes();
        var maskBytes = subnetMask.GetAddressBytes();

        if (addr1Bytes.Length != addr2Bytes.Length || addr1Bytes.Length != maskBytes.Length)
            return false;

        for (int i = 0; i < addr1Bytes.Length; i++)
        {
            if ((addr1Bytes[i] & maskBytes[i]) != (addr2Bytes[i] & maskBytes[i]))
                return false;
        }

        return true;
    }
}
