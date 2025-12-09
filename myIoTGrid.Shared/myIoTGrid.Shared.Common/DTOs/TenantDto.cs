namespace myIoTGrid.Shared.Common.DTOs;

/// <summary>
/// DTO für Tenant-Informationen
/// </summary>
/// <param name="Id">Primärschlüssel</param>
/// <param name="Name">Name des Tenants</param>
/// <param name="CloudApiKey">API-Key für Cloud-Synchronisation (maskiert)</param>
/// <param name="CreatedAt">Erstellungszeitpunkt</param>
/// <param name="LastSyncAt">Letzter Cloud-Sync</param>
/// <param name="IsActive">Ob der Tenant aktiv ist</param>
public record TenantDto(
    Guid Id,
    string Name,
    string? CloudApiKey,
    DateTime CreatedAt,
    DateTime? LastSyncAt,
    bool IsActive
);

/// <summary>
/// DTO zum Erstellen eines Tenants
/// </summary>
/// <param name="Name">Name des Tenants</param>
/// <param name="CloudApiKey">API-Key für Cloud-Synchronisation</param>
public record CreateTenantDto(
    string Name,
    string? CloudApiKey = null
);

/// <summary>
/// DTO zum Aktualisieren eines Tenants
/// </summary>
/// <param name="Name">Neuer Name des Tenants</param>
/// <param name="CloudApiKey">Neuer API-Key für Cloud-Synchronisation</param>
/// <param name="IsActive">Ob der Tenant aktiv ist</param>
public record UpdateTenantDto(
    string? Name = null,
    string? CloudApiKey = null,
    bool? IsActive = null
);
