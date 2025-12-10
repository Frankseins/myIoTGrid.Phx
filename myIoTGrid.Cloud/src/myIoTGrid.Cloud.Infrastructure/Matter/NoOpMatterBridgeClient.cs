using Microsoft.Extensions.Logging;

namespace myIoTGrid.Cloud.Infrastructure.Matter;

/// <summary>
/// No-operation implementation of IMatterBridgeClient for Cloud.
/// Matter Bridge is only available on Hub, not in Cloud.
/// </summary>
public class NoOpMatterBridgeClient : IMatterBridgeClient
{
    private readonly ILogger<NoOpMatterBridgeClient> _logger;

    public NoOpMatterBridgeClient(ILogger<NoOpMatterBridgeClient> logger)
    {
        _logger = logger;
    }

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Matter Bridge not available in Cloud");
        return Task.FromResult(false);
    }

    public Task<MatterBridgeStatus?> GetStatusAsync(CancellationToken ct = default)
    {
        return Task.FromResult<MatterBridgeStatus?>(null);
    }

    public Task<bool> RegisterDeviceAsync(
        string sensorId,
        string name,
        string type,
        string? location = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Matter Bridge not available in Cloud, skipping device registration for {SensorId}", sensorId);
        return Task.FromResult(false);
    }

    public Task<bool> UpdateDeviceValueAsync(
        string sensorId,
        string sensorType,
        double value,
        CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }

    public Task<bool> RemoveDeviceAsync(string sensorId, CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }

    public Task<bool> SetContactSensorStateAsync(
        string sensorId,
        bool isOpen,
        CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }

    public Task<MatterCommissionInfo?> GetCommissionInfoAsync(CancellationToken ct = default)
    {
        return Task.FromResult<MatterCommissionInfo?>(null);
    }

    public Task<MatterQrCodeInfo?> GenerateQrCodeAsync(CancellationToken ct = default)
    {
        return Task.FromResult<MatterQrCodeInfo?>(null);
    }
}
