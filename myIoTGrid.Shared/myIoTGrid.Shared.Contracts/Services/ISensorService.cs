using myIoTGrid.Shared.Common.DTOs;
using myIoTGrid.Shared.Common.DTOs.Common;

namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service Interface for Sensor management (v3.0).
/// Complete sensor definition with hardware configuration and calibration.
/// Two-tier model: Sensor -> NodeSensorAssignment
/// </summary>
public interface ISensorService
{
    /// <summary>Returns all Sensors for the current tenant</summary>
    Task<IEnumerable<SensorDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns Sensors with server-side paging, sorting, and filtering</summary>
    Task<PagedResultDto<SensorDto>> GetPagedAsync(QueryParamsDto queryParams, CancellationToken ct = default);

    /// <summary>Returns a Sensor by Id</summary>
    Task<SensorDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns a Sensor by Code</summary>
    Task<SensorDto?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Returns Sensors by Category</summary>
    Task<IEnumerable<SensorDto>> GetByCategoryAsync(string category, CancellationToken ct = default);

    /// <summary>Returns capabilities for a Sensor</summary>
    Task<IEnumerable<SensorCapabilityDto>> GetCapabilitiesAsync(Guid sensorId, CancellationToken ct = default);

    /// <summary>Creates a new Sensor</summary>
    Task<SensorDto> CreateAsync(CreateSensorDto dto, CancellationToken ct = default);

    /// <summary>Updates a Sensor</summary>
    Task<SensorDto> UpdateAsync(Guid id, UpdateSensorDto dto, CancellationToken ct = default);

    /// <summary>Calibrates a Sensor</summary>
    Task<SensorDto> CalibrateAsync(Guid id, CalibrateSensorDto dto, CancellationToken ct = default);

    /// <summary>Deletes a Sensor</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Seeds default sensors (standard templates like BME280, DHT22, etc.)</summary>
    Task SeedDefaultSensorsAsync(CancellationToken ct = default);
}
