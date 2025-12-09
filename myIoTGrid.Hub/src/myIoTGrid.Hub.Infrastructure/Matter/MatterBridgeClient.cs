using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace myIoTGrid.Hub.Infrastructure.Matter;

/// <summary>
/// HTTP client implementation for communicating with the Matter Bridge
/// </summary>
public class MatterBridgeClient : IMatterBridgeClient
{
    private readonly HttpClient _httpClient;
    private readonly MatterBridgeOptions _options;
    private readonly ILogger<MatterBridgeClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public MatterBridgeClient(
        HttpClient httpClient,
        IOptions<MatterBridgeOptions> options,
        ILogger<MatterBridgeClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return false;
        }

        try
        {
            var response = await _httpClient.GetAsync("/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Matter Bridge not available");
            return false;
        }
    }

    public async Task<MatterBridgeStatus?> GetStatusAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        try
        {
            var response = await _httpClient.GetAsync("/status", ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<StatusResponse>(_jsonOptions, ct);
            if (result == null) return null;

            return new MatterBridgeStatus(
                result.IsStarted,
                result.DeviceCount,
                result.Devices.Select(d => new MatterDeviceInfo(d.SensorId, d.Name, d.Type, d.Location)).ToList(),
                result.PairingCode,
                result.Discriminator
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Matter Bridge status");
            return null;
        }
    }

    public async Task<bool> RegisterDeviceAsync(
        string sensorId,
        string name,
        string type,
        string? location = null,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Matter Bridge is disabled, skipping device registration");
            return false;
        }

        try
        {
            var request = new
            {
                sensorId,
                name,
                type,
                location
            };

            var response = await ExecuteWithRetryAsync(async () =>
                await _httpClient.PostAsJsonAsync("/devices", request, _jsonOptions, ct), ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Registered device {SensorId} ({Type}) with Matter Bridge",
                    sensorId, type);
                return true;
            }

            _logger.LogWarning("Failed to register device {SensorId}: {StatusCode}",
                sensorId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device {SensorId} with Matter Bridge", sensorId);
            return false;
        }
    }

    public async Task<bool> UpdateDeviceValueAsync(
        string sensorId,
        string sensorType,
        double value,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return false;
        }

        // Check if this sensor type is enabled
        if (!_options.EnabledSensorTypes.Contains(sensorType, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Sensor type {SensorType} not enabled for Matter, skipping update", sensorType);
            return false;
        }

        try
        {
            var request = new
            {
                sensorType,
                value
            };

            var response = await ExecuteWithRetryAsync(async () =>
                await _httpClient.PutAsJsonAsync($"/devices/{sensorId}", request, _jsonOptions, ct), ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Updated Matter device {SensorId} {SensorType}: {Value}",
                    sensorId, sensorType, value);
                return true;
            }

            _logger.LogWarning("Failed to update Matter device {SensorId}: {StatusCode}",
                sensorId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Matter device {SensorId}", sensorId);
            return false;
        }
    }

    public async Task<bool> RemoveDeviceAsync(string sensorId, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return false;
        }

        try
        {
            var response = await ExecuteWithRetryAsync(async () =>
                await _httpClient.DeleteAsync($"/devices/{sensorId}", ct), ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Removed device {SensorId} from Matter Bridge", sensorId);
                return true;
            }

            _logger.LogWarning("Failed to remove device {SensorId}: {StatusCode}",
                sensorId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing device {SensorId} from Matter Bridge", sensorId);
            return false;
        }
    }

    public async Task<bool> SetContactSensorStateAsync(
        string sensorId,
        bool isOpen,
        CancellationToken ct = default)
    {
        if (!_options.Enabled || !_options.EnableAlertSensors)
        {
            return false;
        }

        try
        {
            var request = new { isOpen };

            var response = await ExecuteWithRetryAsync(async () =>
                await _httpClient.PutAsJsonAsync($"/devices/{sensorId}/contact", request, _jsonOptions, ct), ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Set contact sensor {SensorId} state to {State}",
                    sensorId, isOpen ? "OPEN" : "CLOSED");
                return true;
            }

            _logger.LogWarning("Failed to set contact sensor {SensorId} state: {StatusCode}",
                sensorId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting contact sensor {SensorId} state", sensorId);
            return false;
        }
    }

    public async Task<MatterCommissionInfo?> GetCommissionInfoAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        try
        {
            var response = await _httpClient.GetAsync("/commission", ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CommissionResponse>(_jsonOptions, ct);
            if (result == null) return null;

            return new MatterCommissionInfo(
                result.PairingCode,
                result.Discriminator,
                result.ManualPairingCode,
                result.QrCodeData
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Matter commission info");
            return null;
        }
    }

    public async Task<MatterQrCodeInfo?> GenerateQrCodeAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        try
        {
            var response = await _httpClient.PostAsync("/commission/qr", null, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<QrCodeResponse>(_jsonOptions, ct);
            if (result == null) return null;

            return new MatterQrCodeInfo(
                result.QrCodeData,
                result.QrCodeImage,
                result.ManualPairingCode
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate Matter QR code");
            return null;
        }
    }

    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<Task<HttpResponseMessage>> action,
        CancellationToken ct)
    {
        var retries = 0;
        while (true)
        {
            try
            {
                return await action();
            }
            catch (HttpRequestException) when (retries < _options.RetryCount)
            {
                retries++;
                _logger.LogDebug("Retry {Retry}/{MaxRetries} after HTTP error", retries, _options.RetryCount);
                await Task.Delay(_options.RetryDelayMilliseconds, ct);
            }
            catch (TaskCanceledException) when (retries < _options.RetryCount && !ct.IsCancellationRequested)
            {
                retries++;
                _logger.LogDebug("Retry {Retry}/{MaxRetries} after timeout", retries, _options.RetryCount);
                await Task.Delay(_options.RetryDelayMilliseconds, ct);
            }
        }
    }

    // Response DTOs for JSON deserialization
    private record StatusResponse(
        bool IsStarted,
        int DeviceCount,
        List<DeviceResponse> Devices,
        int PairingCode,
        int Discriminator
    );

    private record DeviceResponse(
        string SensorId,
        string Name,
        string Type,
        string? Location
    );

    private record CommissionResponse(
        int PairingCode,
        int Discriminator,
        string ManualPairingCode,
        string QrCodeData
    );

    private record QrCodeResponse(
        string QrCodeData,
        string QrCodeImage,
        string ManualPairingCode
    );
}
