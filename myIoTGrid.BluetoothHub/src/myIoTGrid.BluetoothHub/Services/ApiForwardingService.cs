using System.Net.Http.Json;
using myIoTGrid.BluetoothHub.Models;

namespace myIoTGrid.BluetoothHub.Services;

public class ApiForwardingService
{
    private readonly ILogger<ApiForwardingService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HubConfiguration _config;
    private readonly Queue<SensorData> _offlineQueue = new();
    private readonly object _queueLock = new();
    private const int MaxQueueSize = 1000;

    public ApiForwardingService(
        ILogger<ApiForwardingService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _config = configuration.GetSection("BluetoothHub").Get<HubConfiguration>()
            ?? throw new InvalidOperationException("BluetoothHub configuration missing");
    }

    public async Task<bool> ForwardToApiAsync(SensorData data, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            _logger.LogDebug("Forwarding data from {nodeId} to API", data.NodeId);

            var response = await client.PostAsJsonAsync(
                "/api/readings",
                data,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Successfully forwarded data from {nodeId} (Status: {statusCode})",
                    data.NodeId, (int)response.StatusCode);
                return true;
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "API returned {statusCode} for {nodeId}: {content}",
                    (int)response.StatusCode, data.NodeId, content);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error forwarding data from {nodeId} to API", data.NodeId);
            return false;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            _logger.LogDebug("Forward operation cancelled for {nodeId}", data.NodeId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error forwarding data from {nodeId} to API", data.NodeId);
            return false;
        }
    }

    public async Task<bool> ForwardWithRetryAsync(
        SensorData data,
        CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        var delays = new[] { 1000, 2000, 5000 }; // ms - exponential-ish backoff

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;

            var success = await ForwardToApiAsync(data, cancellationToken);

            if (success)
            {
                // Process offline queue after successful send
                await ProcessOfflineQueueAsync(cancellationToken);
                return true;
            }

            if (attempt < maxRetries - 1)
            {
                _logger.LogInformation(
                    "Retry {attempt}/{maxRetries} for {nodeId} after {delay}ms",
                    attempt + 1, maxRetries, data.NodeId, delays[attempt]);

                try
                {
                    await Task.Delay(delays[attempt], cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            }
        }

        // All retries failed - queue for later
        QueueOfflineData(data);
        return false;
    }

    private void QueueOfflineData(SensorData data)
    {
        lock (_queueLock)
        {
            if (_offlineQueue.Count >= MaxQueueSize)
            {
                var dropped = _offlineQueue.Dequeue();
                _logger.LogWarning(
                    "Offline queue full ({size} items), dropping oldest data from {nodeId}",
                    MaxQueueSize, dropped.NodeId);
            }

            _offlineQueue.Enqueue(data);
            _logger.LogInformation(
                "Queued data from {nodeId} for offline delivery. Queue size: {size}",
                data.NodeId, _offlineQueue.Count);
        }
    }

    private async Task ProcessOfflineQueueAsync(CancellationToken cancellationToken)
    {
        int queueCount;
        lock (_queueLock)
        {
            queueCount = _offlineQueue.Count;
        }

        if (queueCount == 0)
            return;

        _logger.LogInformation("Processing offline queue ({count} items)", queueCount);

        var processedCount = 0;
        var failedCount = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            SensorData? data;
            lock (_queueLock)
            {
                if (_offlineQueue.Count == 0)
                    break;
                data = _offlineQueue.Peek();
            }

            var success = await ForwardToApiAsync(data, cancellationToken);

            if (success)
            {
                lock (_queueLock)
                {
                    if (_offlineQueue.Count > 0)
                        _offlineQueue.Dequeue();
                }
                processedCount++;
            }
            else
            {
                failedCount++;
                break; // Stop if API is still unavailable
            }

            // Rate limit: 100ms between requests to avoid overwhelming the API
            try
            {
                await Task.Delay(100, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        int remainingCount;
        lock (_queueLock)
        {
            remainingCount = _offlineQueue.Count;
        }

        _logger.LogInformation(
            "Offline queue processed: {processed} succeeded, {failed} failed, {remaining} remaining",
            processedCount, failedCount, remainingCount);
    }

    public async Task<bool> CheckApiHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync("/health", cancellationToken);

            var isHealthy = response.IsSuccessStatusCode;
            _logger.LogDebug("API health check: {status}", isHealthy ? "healthy" : "unhealthy");

            return isHealthy;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(ex, "API health check failed - connection error");
            return false;
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("API health check cancelled");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error during API health check");
            return false;
        }
    }

    public int GetQueueSize()
    {
        lock (_queueLock)
        {
            return _offlineQueue.Count;
        }
    }

    public void ClearQueue()
    {
        lock (_queueLock)
        {
            var count = _offlineQueue.Count;
            _offlineQueue.Clear();
            _logger.LogInformation("Cleared offline queue ({count} items dropped)", count);
        }
    }
}
