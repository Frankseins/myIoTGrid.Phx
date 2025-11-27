using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Domain.Enums;
using myIoTGrid.Hub.Domain.Interfaces;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for Sensor (ESP32/LoRa32 Device) management
/// </summary>
public class SensorService : ISensorService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly ILogger<SensorService> _logger;

    public SensorService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        ISignalRNotificationService signalRNotificationService,
        ILogger<SensorService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _signalRNotificationService = signalRNotificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SensorDto>> GetByHubAsync(Guid hubId, CancellationToken ct = default)
    {
        var sensors = await _context.Sensors
            .AsNoTracking()
            .Where(s => s.HubId == hubId)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return sensors.ToDtos();
    }

    /// <inheritdoc />
    public async Task<SensorDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        return sensor?.ToDto();
    }

    /// <inheritdoc />
    public async Task<SensorDto?> GetBySensorIdAsync(Guid hubId, string sensorId, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.HubId == hubId && s.SensorId == sensorId, ct);

        return sensor?.ToDto();
    }

    /// <inheritdoc />
    public async Task<SensorDto> GetOrCreateBySensorIdAsync(Guid hubId, string sensorId, CancellationToken ct = default)
    {
        var existingSensor = await _context.Sensors
            .FirstOrDefaultAsync(s => s.HubId == hubId && s.SensorId == sensorId, ct);

        if (existingSensor != null)
        {
            // Update LastSeen and Online status
            existingSensor.LastSeen = DateTime.UtcNow;
            existingSensor.IsOnline = true;
            await _unitOfWork.SaveChangesAsync(ct);

            return existingSensor.ToDto();
        }

        // Create new Sensor (Auto-Registration)
        var newSensor = new Sensor
        {
            Id = Guid.NewGuid(),
            HubId = hubId,
            SensorId = sensorId,
            Name = GenerateNameFromSensorId(sensorId),
            Protocol = Protocol.WLAN,
            SensorTypes = new List<string>(),
            IsOnline = true,
            LastSeen = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Sensors.Add(newSensor);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Sensor auto-registered: {SensorId} ({Name}) for Hub {HubId}",
            newSensor.SensorId, newSensor.Name, hubId);

        return newSensor.ToDto();
    }

    /// <inheritdoc />
    public async Task<SensorDto> CreateAsync(CreateSensorDto dto, CancellationToken ct = default)
    {
        var hubId = dto.HubId ?? throw new InvalidOperationException("HubId is required");

        // Check if SensorId already exists within the Hub
        var exists = await _context.Sensors
            .AsNoTracking()
            .AnyAsync(s => s.HubId == hubId && s.SensorId == dto.SensorId, ct);

        if (exists)
        {
            throw new InvalidOperationException($"Sensor with SensorId '{dto.SensorId}' already exists for this Hub.");
        }

        var sensor = dto.ToEntity(hubId);

        _context.Sensors.Add(sensor);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Sensor created: {SensorId} ({Name})", sensor.SensorId, sensor.Name);

        return sensor.ToDto();
    }

    /// <inheritdoc />
    public async Task<SensorDto?> UpdateAsync(Guid id, UpdateSensorDto dto, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (sensor == null)
        {
            _logger.LogWarning("Sensor not found: {SensorId}", id);
            return null;
        }

        sensor.ApplyUpdate(dto);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Sensor updated: {SensorId}", id);

        return sensor.ToDto();
    }

    /// <inheritdoc />
    public async Task UpdateLastSeenAsync(Guid id, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (sensor == null) return;

        sensor.LastSeen = DateTime.UtcNow;
        sensor.IsOnline = true;
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task SetOnlineStatusAsync(Guid id, bool isOnline, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors
            .Include(s => s.Hub)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (sensor == null) return;

        var wasOnline = sensor.IsOnline;
        sensor.IsOnline = isOnline;

        if (!isOnline)
        {
            _logger.LogWarning("Sensor offline: {SensorId} ({Name})", sensor.SensorId, sensor.Name);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task UpdateStatusAsync(Guid id, SensorStatusDto status, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (sensor == null) return;

        sensor.ApplyStatus(status);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Generates a name from the SensorId
    /// </summary>
    private static string GenerateNameFromSensorId(string sensorId)
    {
        if (string.IsNullOrWhiteSpace(sensorId)) return "Unknown Sensor";

        // "sensor-wohnzimmer-01" -> "Sensor Wohnzimmer 01"
        var parts = sensorId.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        var formattedParts = parts.Select(s =>
            s.Length > 0 ? char.ToUpper(s[0]) + s[1..].ToLower() : s);

        return string.Join(" ", formattedParts);
    }
}
