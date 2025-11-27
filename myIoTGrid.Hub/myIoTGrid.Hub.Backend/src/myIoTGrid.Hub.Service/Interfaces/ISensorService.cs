using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Interfaces;

/// <summary>
/// Service Interface for Sensor (ESP32/LoRa32 Device) management
/// </summary>
public interface ISensorService
{
    /// <summary>Returns all Sensors for a Hub</summary>
    Task<IEnumerable<SensorDto>> GetByHubAsync(Guid hubId, CancellationToken ct = default);

    /// <summary>Returns a Sensor by ID</summary>
    Task<SensorDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns a Sensor by Sensor identifier string within a Hub</summary>
    Task<SensorDto?> GetBySensorIdAsync(Guid hubId, string sensorId, CancellationToken ct = default);

    /// <summary>Finds or creates a Sensor (auto-registration)</summary>
    Task<SensorDto> GetOrCreateBySensorIdAsync(Guid hubId, string sensorId, CancellationToken ct = default);

    /// <summary>Creates a new Sensor</summary>
    Task<SensorDto> CreateAsync(CreateSensorDto dto, CancellationToken ct = default);

    /// <summary>Updates a Sensor</summary>
    Task<SensorDto?> UpdateAsync(Guid id, UpdateSensorDto dto, CancellationToken ct = default);

    /// <summary>Updates the LastSeen timestamp</summary>
    Task UpdateLastSeenAsync(Guid id, CancellationToken ct = default);

    /// <summary>Sets the online status</summary>
    Task SetOnlineStatusAsync(Guid id, bool isOnline, CancellationToken ct = default);

    /// <summary>Updates the Sensor status (online, lastSeen, battery)</summary>
    Task UpdateStatusAsync(Guid id, SensorStatusDto status, CancellationToken ct = default);
}
