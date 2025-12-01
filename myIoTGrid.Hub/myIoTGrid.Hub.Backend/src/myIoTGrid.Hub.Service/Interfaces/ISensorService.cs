using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.DTOs.Common;

namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service Interface for Sensor instance management.
/// Concrete sensors with calibration settings.
/// </summary>
public interface ISensorService
{
    /// <summary>Returns all Sensors for the current tenant</summary>
    Task<IEnumerable<SensorDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns Sensors with server-side paging, sorting, and filtering</summary>
    Task<PagedResultDto<SensorDto>> GetPagedAsync(QueryParamsDto queryParams, CancellationToken ct = default);

    /// <summary>Returns a Sensor by Id</summary>
    Task<SensorDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns Sensors by SensorType</summary>
    Task<IEnumerable<SensorDto>> GetBySensorTypeAsync(Guid sensorTypeId, CancellationToken ct = default);

    /// <summary>Creates a new Sensor instance</summary>
    Task<SensorDto> CreateAsync(CreateSensorDto dto, CancellationToken ct = default);

    /// <summary>Updates a Sensor instance</summary>
    Task<SensorDto> UpdateAsync(Guid id, UpdateSensorDto dto, CancellationToken ct = default);

    /// <summary>Calibrates a Sensor</summary>
    Task<SensorDto> CalibrateAsync(Guid id, CalibrateSensorDto dto, CancellationToken ct = default);

    /// <summary>Deletes a Sensor</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Seeds default sensors (one per SensorType)</summary>
    Task SeedDefaultSensorsAsync(CancellationToken ct = default);
}
