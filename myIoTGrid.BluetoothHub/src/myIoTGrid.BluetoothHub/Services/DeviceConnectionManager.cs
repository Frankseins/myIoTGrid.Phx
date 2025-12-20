using InTheHand.Bluetooth;
using myIoTGrid.BluetoothHub.Models;

namespace myIoTGrid.BluetoothHub.Services;

public class DeviceConnectionManager
{
    private readonly ILogger<DeviceConnectionManager> _logger;
    private readonly HubConfiguration _config;
    private readonly Dictionary<string, BluetoothDevice> _connectedDevices = new();
    private readonly Dictionary<string, CancellationTokenSource> _reconnectTasks = new();
    private readonly object _lock = new();

    public DeviceConnectionManager(
        ILogger<DeviceConnectionManager> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _config = configuration.GetSection("BluetoothHub").Get<HubConfiguration>()
            ?? throw new InvalidOperationException("BluetoothHub configuration missing");
    }

    public async Task<bool> ConnectToDeviceAsync(BleDevice device, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Connecting to device: {deviceName} ({nodeId})",
                device.Name, device.NodeId);

            // TODO: Implement actual GATT connection
            // This requires platform-specific implementation using BlueZ on Linux
            //
            // Steps for GATT connection:
            // 1. Connect to device: device.Gatt.ConnectAsync()
            // 2. Get primary service: device.Gatt.GetPrimaryServiceAsync(serviceUuid)
            // 3. Get characteristic: service.GetCharacteristicAsync(characteristicUuid)
            // 4. Subscribe to notifications: characteristic.StartNotificationsAsync()
            // 5. Handle value changed events: characteristic.ValueChanged += handler

            _logger.LogWarning("GATT connection not yet implemented - requires BlueZ D-Bus integration");
            _logger.LogDebug("Target Service UUID: {serviceUuid}", _config.ServiceUUID);
            _logger.LogDebug("Target SensorData UUID: {sensorDataUuid}", _config.SensorDataUUID);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to device {deviceName}", device.Name);
            return false;
        }
    }

    public async Task DisconnectDeviceAsync(string nodeId)
    {
        lock (_lock)
        {
            if (_connectedDevices.TryGetValue(nodeId, out var device))
            {
                _logger.LogInformation("Disconnecting from device: {nodeId}", nodeId);

                // Cancel reconnect task if exists
                if (_reconnectTasks.TryGetValue(nodeId, out var cts))
                {
                    cts.Cancel();
                    _reconnectTasks.Remove(nodeId);
                }

                _connectedDevices.Remove(nodeId);

                // TODO: Implement actual GATT disconnection
                // device.Gatt?.Disconnect();
            }
        }

        await Task.CompletedTask;
    }

    public bool IsDeviceConnected(string nodeId)
    {
        lock (_lock)
        {
            return _connectedDevices.ContainsKey(nodeId);
        }
    }

    public IReadOnlyDictionary<string, BluetoothDevice> GetConnectedDevices()
    {
        lock (_lock)
        {
            return new Dictionary<string, BluetoothDevice>(_connectedDevices);
        }
    }

    public void OnDeviceDisconnected(string nodeId, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Device disconnected: {nodeId}", nodeId);

        lock (_lock)
        {
            _connectedDevices.Remove(nodeId);
        }

        // Start reconnect task
        _ = StartReconnectTaskAsync(nodeId, cancellationToken);
    }

    private async Task StartReconnectTaskAsync(string nodeId, CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        lock (_lock)
        {
            if (_reconnectTasks.ContainsKey(nodeId))
            {
                _logger.LogDebug("Reconnect task already running for {nodeId}", nodeId);
                return;
            }
            _reconnectTasks[nodeId] = cts;
        }

        try
        {
            var delay = _config.ReconnectDelay;
            var maxDelay = 300000; // 5 minutes max
            var currentDelay = delay;
            var attempt = 0;

            while (!cts.Token.IsCancellationRequested)
            {
                attempt++;
                await Task.Delay(currentDelay, cts.Token);

                _logger.LogInformation("Reconnect attempt {attempt} for {nodeId}", attempt, nodeId);

                // TODO: Implement actual reconnect logic
                // 1. Scan for device by nodeId
                // 2. Connect if found
                // 3. If connected, break out of loop

                // Exponential backoff with jitter
                var jitter = Random.Shared.Next(0, 1000);
                currentDelay = Math.Min(currentDelay * 2 + jitter, maxDelay);

                _logger.LogDebug("Next reconnect attempt in {delay}ms", currentDelay);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Reconnect task cancelled for {nodeId}", nodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in reconnect task for {nodeId}", nodeId);
        }
        finally
        {
            lock (_lock)
            {
                _reconnectTasks.Remove(nodeId);
            }
        }
    }

    public void CancelReconnect(string nodeId)
    {
        lock (_lock)
        {
            if (_reconnectTasks.TryGetValue(nodeId, out var cts))
            {
                cts.Cancel();
                _reconnectTasks.Remove(nodeId);
                _logger.LogDebug("Cancelled reconnect task for {nodeId}", nodeId);
            }
        }
    }
}
