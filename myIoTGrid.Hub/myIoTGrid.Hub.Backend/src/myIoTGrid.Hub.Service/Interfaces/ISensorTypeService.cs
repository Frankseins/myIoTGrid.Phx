using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service Interface for SensorType management.
/// Matter-konform: Entspricht Matter Clusters.
/// </summary>
public interface ISensorTypeService
{
    /// <summary>Returns all SensorTypes</summary>
    Task<IEnumerable<SensorTypeDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns all SensorTypes (cached for 1 hour)</summary>
    Task<IEnumerable<SensorTypeDto>> GetAllCachedAsync(CancellationToken ct = default);

    /// <summary>Returns a SensorType by TypeId</summary>
    Task<SensorTypeDto?> GetByTypeIdAsync(string typeId, CancellationToken ct = default);

    /// <summary>Returns SensorTypes by category</summary>
    Task<IEnumerable<SensorTypeDto>> GetByCategoryAsync(string category, CancellationToken ct = default);

    /// <summary>Gets the unit for a SensorType</summary>
    Task<string> GetUnitAsync(string typeId, CancellationToken ct = default);

    /// <summary>Creates a new SensorType</summary>
    Task<SensorTypeDto> CreateAsync(CreateSensorTypeDto dto, CancellationToken ct = default);

    /// <summary>Synchronizes SensorTypes from the Cloud</summary>
    Task SyncFromCloudAsync(CancellationToken ct = default);

    /// <summary>Seeds default SensorTypes with Matter Cluster IDs</summary>
    Task SeedDefaultTypesAsync(CancellationToken ct = default);
}
