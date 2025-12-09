using Microsoft.Extensions.Logging;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for seeding default data (v3.0)
/// </summary>
public class SeedDataService : ISeedDataService
{
    private readonly ITenantService _tenantService;
    private readonly IAlertTypeService _alertTypeService;
    private readonly ISensorService _sensorService;
    private readonly IHubService _hubService;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(
        ITenantService tenantService,
        IAlertTypeService alertTypeService,
        ISensorService sensorService,
        IHubService hubService,
        ILogger<SeedDataService> logger)
    {
        _tenantService = tenantService;
        _alertTypeService = alertTypeService;
        _sensorService = sensorService;
        _hubService = hubService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SeedAllAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting seed data process...");

        await SeedTenantAsync(ct);
        await SeedHubAsync(ct);
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
    public async Task SeedAlertTypesAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Seeding default alert types...");
        await _alertTypeService.SeedDefaultTypesAsync(ct);
        _logger.LogInformation("Default alert types seeded");
    }

    /// <inheritdoc />
    public async Task SeedSensorsAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Seeding default sensors (standard templates)...");
        await _sensorService.SeedDefaultSensorsAsync(ct);
        _logger.LogInformation("Default sensors seeded");
    }

    /// <inheritdoc />
    public async Task SeedHubAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Ensuring default Hub exists (Single-Hub-Architecture)...");
        await _hubService.EnsureDefaultHubAsync(ct);
        _logger.LogInformation("Default Hub ensured");
    }
}
