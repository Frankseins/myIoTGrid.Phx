using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.DTOs.Common;

namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service Interface for SensorType management.
/// Hardware sensor library with default configurations.
/// </summary>
public interface ISensorTypeService
{
    /// <summary>Returns all SensorTypes</summary>
    Task<IEnumerable<SensorTypeDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns SensorTypes with paging, sorting, and filtering</summary>
    Task<PagedResultDto<SensorTypeDto>> GetPagedAsync(QueryParamsDto queryParams, CancellationToken ct = default);

    /// <summary>Returns all SensorTypes (cached for 1 hour)</summary>
    Task<IEnumerable<SensorTypeDto>> GetAllCachedAsync(CancellationToken ct = default);

    /// <summary>Returns a SensorType by Id</summary>
    Task<SensorTypeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns a SensorType by Code</summary>
    Task<SensorTypeDto?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Returns SensorTypes by category</summary>
    Task<IEnumerable<SensorTypeDto>> GetByCategoryAsync(string category, CancellationToken ct = default);

    /// <summary>Returns all capabilities for a SensorType</summary>
    Task<IEnumerable<SensorTypeCapabilityDto>> GetCapabilitiesAsync(Guid sensorTypeId, CancellationToken ct = default);

    /// <summary>Creates a new SensorType</summary>
    Task<SensorTypeDto> CreateAsync(CreateSensorTypeDto dto, CancellationToken ct = default);

    /// <summary>Updates a SensorType</summary>
    Task<SensorTypeDto> UpdateAsync(Guid id, UpdateSensorTypeDto dto, CancellationToken ct = default);

    /// <summary>Deletes a SensorType</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Synchronizes SensorTypes from the Cloud</summary>
    Task SyncFromCloudAsync(CancellationToken ct = default);

    /// <summary>Seeds default SensorTypes (Hardware Library)</summary>
    Task SeedDefaultTypesAsync(CancellationToken ct = default);
}
