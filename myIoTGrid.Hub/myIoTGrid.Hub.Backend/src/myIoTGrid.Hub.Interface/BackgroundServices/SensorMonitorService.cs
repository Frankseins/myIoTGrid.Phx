using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.Enums;
using myIoTGrid.Hub.Shared.Options;

namespace myIoTGrid.Hub.Interface.BackgroundServices;

/// <summary>
/// Background service that monitors sensors for offline status
/// </summary>
public class SensorMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MonitoringOptions _options;
    private readonly ILogger<SensorMonitorService> _logger;

    public SensorMonitorService(
        IServiceScopeFactory scopeFactory,
        IOptions<MonitoringOptions> options,
        ILogger<SensorMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableSensorMonitoring)
        {
            _logger.LogInformation("Sensor monitoring is disabled");
            return;
        }

        _logger.LogInformation(
            "SensorMonitorService started. Check interval: {Interval}s, Offline timeout: {Timeout}min",
            _options.SensorCheckIntervalSeconds,
            _options.SensorOfflineTimeoutMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckSensorsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during sensor monitoring");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.SensorCheckIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("SensorMonitorService stopped");
    }

    private async Task CheckSensorsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HubDbContext>();
        var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
        var sensorService = scope.ServiceProvider.GetRequiredService<ISensorService>();
        var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();

        var threshold = DateTime.UtcNow.AddMinutes(-_options.SensorOfflineTimeoutMinutes);

        // Find sensors that have gone offline
        var offlineSensors = await dbContext.Sensors
            .Include(s => s.Hub)
            .Where(s => s.IsOnline && (s.LastSeen == null || s.LastSeen < threshold))
            .ToListAsync(ct);

        foreach (var sensor in offlineSensors)
        {
            _logger.LogWarning(
                "Sensor detected as offline: {SensorId} ({Name}), LastSeen: {LastSeen}",
                sensor.SensorId,
                sensor.Name,
                sensor.LastSeen);

            // Update sensor status
            await sensorService.SetOnlineStatusAsync(sensor.Id, false, ct);

            // Set tenant context for alert creation
            if (sensor.Hub != null)
            {
                tenantService.SetCurrentTenantId(sensor.Hub.TenantId);
            }

            // Create offline alert
            await CreateSensorOfflineAlertAsync(alertService, sensor, ct);
        }

        // Find sensors that have come back online (LastSeen updated recently, but IsOnline is false)
        var onlineSensors = await dbContext.Sensors
            .Include(s => s.Hub)
            .Where(s => !s.IsOnline && s.LastSeen != null && s.LastSeen >= threshold)
            .ToListAsync(ct);

        foreach (var sensor in onlineSensors)
        {
            _logger.LogInformation(
                "Sensor detected as back online: {SensorId} ({Name})",
                sensor.SensorId,
                sensor.Name);

            // Update sensor status
            await sensorService.SetOnlineStatusAsync(sensor.Id, true, ct);

            // Set tenant context
            if (sensor.Hub != null)
            {
                tenantService.SetCurrentTenantId(sensor.Hub.TenantId);
            }

            // Deactivate existing offline alerts for this sensor
            await alertService.DeactivateAlertsAsync(sensor.Id, "sensor_offline", ct);
        }

        if (offlineSensors.Count > 0 || onlineSensors.Count > 0)
        {
            _logger.LogInformation(
                "Sensor monitoring cycle completed. Offline: {Offline}, Back online: {Online}",
                offlineSensors.Count,
                onlineSensors.Count);
        }
    }

    private async Task CreateSensorOfflineAlertAsync(IAlertService alertService, Sensor sensor, CancellationToken ct)
    {
        try
        {
            var dto = new CreateAlertDto(
                AlertTypeCode: "sensor_offline",
                SensorId: sensor.SensorId,
                HubId: sensor.Hub?.HubId,
                Level: AlertLevelDto.Warning,
                Message: $"Sensor '{sensor.Name}' ({sensor.SensorId}) is offline. Last seen: {sensor.LastSeen:u}",
                Recommendation: "Check sensor power supply, network connection, or signal strength."
            );

            await alertService.CreateLocalAlertAsync(dto, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create offline alert for sensor {SensorId}", sensor.SensorId);
        }
    }
}
