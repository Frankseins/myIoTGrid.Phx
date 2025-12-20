using myIoTGrid.BluetoothHub.Services;

namespace myIoTGrid.BluetoothHub;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly BluetoothScannerService _scanner;
    private readonly DeviceConnectionManager _connectionManager;
    private readonly SensorDataProcessor _sensorDataProcessor;
    private readonly ApiForwardingService _apiForwardingService;

    public Worker(
        ILogger<Worker> logger,
        IConfiguration configuration,
        BluetoothScannerService scanner,
        DeviceConnectionManager connectionManager,
        SensorDataProcessor sensorDataProcessor,
        ApiForwardingService apiForwardingService)
    {
        _logger = logger;
        _configuration = configuration;
        _scanner = scanner;
        _connectionManager = connectionManager;
        _sensorDataProcessor = sensorDataProcessor;
        _apiForwardingService = apiForwardingService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Bluetooth Hub Service starting at: {time}", DateTimeOffset.Now);

        var hubId = _configuration["BluetoothHub:HubId"] ?? "hub-unknown";
        var apiUrl = _configuration["BluetoothHub:ApiBaseUrl"] ?? "http://localhost:5000";
        var scanInterval = _configuration.GetValue<int>("BluetoothHub:ScanInterval", 30000);

        _logger.LogInformation("Hub ID: {hubId}", hubId);
        _logger.LogInformation("API URL: {apiUrl}", apiUrl);
        _logger.LogInformation("Scan Interval: {scanInterval}ms", scanInterval);

        // Check API health on startup
        var apiHealthy = await _apiForwardingService.CheckApiHealthAsync(stoppingToken);
        _logger.LogInformation("API Health Check: {status}", apiHealthy ? "OK" : "FAILED");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Starting BLE scan cycle");

                var devices = await _scanner.ScanForDevicesAsync(stoppingToken);
                _logger.LogInformation("Found {count} BLE devices", devices.Count);

                foreach (var device in devices)
                {
                    if (_scanner.IsDeviceRegistered(device.Name, device.NodeId) &&
                        !_connectionManager.IsDeviceConnected(device.NodeId))
                    {
                        _logger.LogInformation("Attempting to connect to registered device: {deviceName}", device.Name);
                        await _connectionManager.ConnectToDeviceAsync(device, stoppingToken);
                    }
                }

                // Log current connection status
                var connectedCount = _connectionManager.GetConnectedDevices().Count;
                var queueSize = _apiForwardingService.GetQueueSize();

                _logger.LogInformation(
                    "Status: {connected} connected devices, {queued} items in offline queue",
                    connectedCount, queueSize);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in main loop");
            }

            await Task.Delay(scanInterval, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bluetooth Hub Service stopping");

        // Disconnect all devices
        var connectedDevices = _connectionManager.GetConnectedDevices();
        foreach (var deviceId in connectedDevices.Keys.ToList())
        {
            await _connectionManager.DisconnectDeviceAsync(deviceId);
        }

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Bluetooth Hub Service stopped");
    }
}
