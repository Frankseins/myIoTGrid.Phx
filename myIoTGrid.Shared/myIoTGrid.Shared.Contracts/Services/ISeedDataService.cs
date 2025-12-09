namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service for seeding default data (v3.0).
/// </summary>
public interface ISeedDataService
{
    Task SeedAllAsync(CancellationToken ct = default);
    Task SeedTenantAsync(CancellationToken ct = default);
    Task SeedAlertTypesAsync(CancellationToken ct = default);
    Task SeedSensorsAsync(CancellationToken ct = default);
    Task SeedHubAsync(CancellationToken ct = default);
}
