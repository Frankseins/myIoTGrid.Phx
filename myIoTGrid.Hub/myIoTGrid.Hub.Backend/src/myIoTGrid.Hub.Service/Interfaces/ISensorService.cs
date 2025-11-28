using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service Interface for Sensor (Physical sensor chip: DHT22, BME280) management.
/// Matter-konform: Entspricht einem Matter Endpoint.
/// </summary>
public interface ISensorService
{
    /// <summary>Returns all Sensors for a Node</summary>
    Task<IEnumerable<SensorDto>> GetByNodeAsync(Guid nodeId, CancellationToken ct = default);

    /// <summary>Returns a Sensor by ID</summary>
    Task<SensorDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns a Sensor by SensorTypeId within a Node</summary>
    Task<SensorDto?> GetBySensorTypeAsync(Guid nodeId, string sensorTypeId, CancellationToken ct = default);

    /// <summary>Creates a new Sensor on a Node</summary>
    Task<SensorDto> CreateAsync(Guid nodeId, CreateSensorDto dto, CancellationToken ct = default);

    /// <summary>Updates a Sensor</summary>
    Task<SensorDto?> UpdateAsync(Guid id, UpdateSensorDto dto, CancellationToken ct = default);

    /// <summary>Deletes a Sensor</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates or updates sensors for a node based on sensor type IDs</summary>
    Task<IEnumerable<SensorDto>> SyncSensorsAsync(Guid nodeId, IEnumerable<string> sensorTypeIds, CancellationToken ct = default);
}
