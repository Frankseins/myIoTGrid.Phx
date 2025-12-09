using myIoTGrid.Shared.Common.DTOs;

namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service Interface für Tenant-Verwaltung
/// </summary>
public interface ITenantService
{
    /// <summary>Gibt die aktuelle Tenant-ID zurück</summary>
    Guid GetCurrentTenantId();

    /// <summary>Setzt die aktuelle Tenant-ID</summary>
    void SetCurrentTenantId(Guid tenantId);

    /// <summary>Stellt sicher, dass der Default-Tenant existiert</summary>
    Task EnsureDefaultTenantAsync(CancellationToken ct = default);

    /// <summary>Gibt den Tenant anhand der ID zurück</summary>
    Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Gibt alle Tenants zurück</summary>
    Task<IEnumerable<TenantDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Erstellt einen neuen Tenant</summary>
    Task<TenantDto> CreateAsync(CreateTenantDto dto, CancellationToken ct = default);

    /// <summary>Aktualisiert einen Tenant</summary>
    Task<TenantDto?> UpdateAsync(Guid id, UpdateTenantDto dto, CancellationToken ct = default);
}
