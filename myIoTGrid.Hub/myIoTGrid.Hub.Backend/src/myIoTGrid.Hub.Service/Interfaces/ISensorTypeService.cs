using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service Interface für SensorType-Verwaltung
/// </summary>
public interface ISensorTypeService
{
    /// <summary>Gibt alle SensorTypes zurück</summary>
    Task<IEnumerable<SensorTypeDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Gibt einen SensorType anhand der ID zurück</summary>
    Task<SensorTypeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Findet einen SensorType anhand des Codes</summary>
    Task<SensorTypeDto?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Erstellt einen neuen SensorType</summary>
    Task<SensorTypeDto> CreateAsync(CreateSensorTypeDto dto, CancellationToken ct = default);

    /// <summary>Synchronisiert SensorTypes von der Cloud (Placeholder)</summary>
    Task SyncFromCloudAsync(CancellationToken ct = default);

    /// <summary>Seeding der Default SensorTypes</summary>
    Task SeedDefaultTypesAsync(CancellationToken ct = default);
}
