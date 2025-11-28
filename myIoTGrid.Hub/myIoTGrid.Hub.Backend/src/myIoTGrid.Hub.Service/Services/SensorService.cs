using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Domain.Interfaces;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for Sensor (Physical sensor chip: DHT22, BME280) management.
/// Matter-konform: Entspricht einem Matter Endpoint.
/// </summary>
public class SensorService : ISensorService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SensorService> _logger;

    public SensorService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<SensorService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SensorDto>> GetByNodeAsync(Guid nodeId, CancellationToken ct = default)
    {
        var sensors = await _context.Sensors
            .AsNoTracking()
            .Include(s => s.SensorType)
            .Where(s => s.NodeId == nodeId)
            .OrderBy(s => s.EndpointId)
            .ToListAsync(ct);

        return sensors.ToDtos();
    }

    /// <inheritdoc />
    public async Task<SensorDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors
            .AsNoTracking()
            .Include(s => s.SensorType)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        return sensor?.ToDto();
    }

    /// <inheritdoc />
    public async Task<SensorDto?> GetBySensorTypeAsync(Guid nodeId, string sensorTypeId, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors
            .AsNoTracking()
            .Include(s => s.SensorType)
            .FirstOrDefaultAsync(s => s.NodeId == nodeId && s.SensorTypeId == sensorTypeId.ToLowerInvariant(), ct);

        return sensor?.ToDto();
    }

    /// <inheritdoc />
    public async Task<SensorDto> CreateAsync(Guid nodeId, CreateSensorDto dto, CancellationToken ct = default)
    {
        // Check if EndpointId already exists on this Node
        var exists = await _context.Sensors
            .AsNoTracking()
            .AnyAsync(s => s.NodeId == nodeId && s.EndpointId == dto.EndpointId, ct);

        if (exists)
        {
            throw new InvalidOperationException($"Sensor with EndpointId '{dto.EndpointId}' already exists on this Node.");
        }

        var sensor = dto.ToEntity(nodeId);

        _context.Sensors.Add(sensor);
        await _unitOfWork.SaveChangesAsync(ct);

        // Reload with SensorType
        sensor = await _context.Sensors
            .Include(s => s.SensorType)
            .FirstAsync(s => s.Id == sensor.Id, ct);

        _logger.LogInformation("Sensor created: {SensorTypeId} (Endpoint {EndpointId}) on Node {NodeId}",
            dto.SensorTypeId, dto.EndpointId, nodeId);

        return sensor.ToDto();
    }

    /// <inheritdoc />
    public async Task<SensorDto?> UpdateAsync(Guid id, UpdateSensorDto dto, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors
            .Include(s => s.SensorType)
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
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (sensor == null) return false;

        _context.Sensors.Remove(sensor);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Sensor deleted: {SensorId}", id);
        return true;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SensorDto>> SyncSensorsAsync(Guid nodeId, IEnumerable<string> sensorTypeIds, CancellationToken ct = default)
    {
        var typeIdList = sensorTypeIds.Select(id => id.ToLowerInvariant()).ToList();

        // Get existing sensors for this node
        var existingSensors = await _context.Sensors
            .Where(s => s.NodeId == nodeId)
            .ToListAsync(ct);

        var existingTypeIds = existingSensors.Select(s => s.SensorTypeId).ToHashSet();

        // Find next EndpointId
        var maxEndpointId = existingSensors.Count > 0
            ? existingSensors.Max(s => s.EndpointId)
            : 0;

        // Add missing sensors
        var newSensors = new List<Sensor>();
        foreach (var typeId in typeIdList.Where(id => !existingTypeIds.Contains(id)))
        {
            maxEndpointId++;
            newSensors.Add(new Sensor
            {
                Id = Guid.NewGuid(),
                NodeId = nodeId,
                SensorTypeId = typeId,
                EndpointId = maxEndpointId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (newSensors.Count > 0)
        {
            _context.Sensors.AddRange(newSensors);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Synced {Count} new sensors to Node {NodeId}", newSensors.Count, nodeId);
        }

        // Return all sensors for the node
        return await GetByNodeAsync(nodeId, ct);
    }
}
