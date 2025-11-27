using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service Interface for SensorData management
/// </summary>
public interface ISensorDataService
{
    /// <summary>Creates a new measurement</summary>
    Task<SensorDataDto> CreateAsync(CreateSensorDataDto dto, CancellationToken ct = default);

    /// <summary>Returns a measurement by ID</summary>
    Task<SensorDataDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns measurements filtered and paginated</summary>
    Task<PaginatedResultDto<SensorDataDto>> GetFilteredAsync(SensorDataFilterDto filter, CancellationToken ct = default);

    /// <summary>Returns the latest measurements per Sensor</summary>
    Task<IEnumerable<SensorDataDto>> GetLatestByHubAsync(Guid sensorId, CancellationToken ct = default);

    /// <summary>Returns the latest measurements of all Sensors</summary>
    Task<IEnumerable<SensorDataDto>> GetLatestAsync(CancellationToken ct = default);

    /// <summary>Marks measurements as synced to cloud</summary>
    Task MarkAsSyncedAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}
