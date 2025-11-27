namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service interface for seeding default data
/// </summary>
public interface ISeedDataService
{
    /// <summary>
    /// Seeds all default data (Tenant, SensorTypes, AlertTypes)
    /// </summary>
    Task SeedAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Ensures the default tenant exists
    /// </summary>
    Task SeedTenantAsync(CancellationToken ct = default);

    /// <summary>
    /// Seeds default sensor types
    /// </summary>
    Task SeedSensorTypesAsync(CancellationToken ct = default);

    /// <summary>
    /// Seeds default alert types
    /// </summary>
    Task SeedAlertTypesAsync(CancellationToken ct = default);
}
