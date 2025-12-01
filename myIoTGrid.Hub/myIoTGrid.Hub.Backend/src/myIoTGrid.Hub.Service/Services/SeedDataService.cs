using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Service.Interfaces;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for seeding default data
/// </summary>
public class SeedDataService : ISeedDataService
{
    private readonly ITenantService _tenantService;
    private readonly ISensorTypeService _sensorTypeService;
    private readonly IAlertTypeService _alertTypeService;
    private readonly ISensorService _sensorService;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(
        ITenantService tenantService,
        ISensorTypeService sensorTypeService,
        IAlertTypeService alertTypeService,
        ISensorService sensorService,
        ILogger<SeedDataService> logger)
    {
        _tenantService = tenantService;
        _sensorTypeService = sensorTypeService;
        _alertTypeService = alertTypeService;
        _sensorService = sensorService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SeedAllAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting seed data process...");

        await SeedTenantAsync(ct);
        await SeedSensorTypesAsync(ct);
        await SeedAlertTypesAsync(ct);
        await SeedSensorsAsync(ct);

        _logger.LogInformation("Seed data process completed");
    }

    /// <inheritdoc />
    public async Task SeedTenantAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Seeding default tenant...");
        await _tenantService.EnsureDefaultTenantAsync(ct);
        _logger.LogInformation("Default tenant ensured");
    }

    /// <inheritdoc />
    public async Task SeedSensorTypesAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Seeding default sensor types...");
        await _sensorTypeService.SeedDefaultTypesAsync(ct);
        _logger.LogInformation("Default sensor types seeded");
    }

    /// <inheritdoc />
    public async Task SeedAlertTypesAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Seeding default alert types...");
        await _alertTypeService.SeedDefaultTypesAsync(ct);
        _logger.LogInformation("Default alert types seeded");
    }

    /// <inheritdoc />
    public async Task SeedSensorsAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Seeding default sensors (one per SensorType)...");
        await _sensorService.SeedDefaultSensorsAsync(ct);
        _logger.LogInformation("Default sensors seeded");
    }
}
