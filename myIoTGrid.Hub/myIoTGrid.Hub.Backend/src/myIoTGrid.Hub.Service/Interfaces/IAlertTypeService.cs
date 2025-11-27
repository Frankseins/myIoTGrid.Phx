using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service Interface für AlertType-Verwaltung
/// </summary>
public interface IAlertTypeService
{
    /// <summary>Gibt alle AlertTypes zurück</summary>
    Task<IEnumerable<AlertTypeDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Gibt einen AlertType anhand der ID zurück</summary>
    Task<AlertTypeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Findet einen AlertType anhand des Codes</summary>
    Task<AlertTypeDto?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Erstellt einen neuen AlertType</summary>
    Task<AlertTypeDto> CreateAsync(CreateAlertTypeDto dto, CancellationToken ct = default);

    /// <summary>Synchronisiert AlertTypes von der Cloud (Placeholder)</summary>
    Task SyncFromCloudAsync(CancellationToken ct = default);

    /// <summary>Seeding der Default AlertTypes</summary>
    Task SeedDefaultTypesAsync(CancellationToken ct = default);
}
