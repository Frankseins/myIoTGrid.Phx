namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service interface for seeding default data (v3.0)
/// </summary>
public interface ISeedDataService
{
    /// <summary>
    /// Seeds all default data (Tenant, Hub, Sensors, AlertTypes)
    /// </summary>
    Task SeedAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Ensures the default tenant exists
    /// </summary>
    Task SeedTenantAsync(CancellationToken ct = default);

    /// <summary>
    /// Seeds default sensors (standard templates like BME280, DHT22, etc.)
    /// </summary>
    Task SeedSensorsAsync(CancellationToken ct = default);

    /// <summary>
    /// Seeds default alert types
    /// </summary>
    Task SeedAlertTypesAsync(CancellationToken ct = default);

    /// <summary>
    /// Ensures the default Hub exists (Single-Hub-Architecture)
    /// </summary>
    Task SeedHubAsync(CancellationToken ct = default);
}
