using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Domain.Interfaces;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.DTOs.Common;
using myIoTGrid.Hub.Shared.Extensions;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for Sensor instance management.
/// Concrete sensors with calibration settings.
/// </summary>
public class SensorService : ISensorService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantService _tenantService;
    private readonly ILogger<SensorService> _logger;

    public SensorService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        ITenantService tenantService,
        ILogger<SensorService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SensorDto>> GetAllAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var sensors = await _context.Sensors
            .AsNoTracking()
            .Include(s => s.SensorType)
            .Where(s => s.TenantId == tenantId)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return sensors.ToDtos();
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<SensorDto>> GetPagedAsync(QueryParamsDto queryParams, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Sensors
            .AsNoTracking()
            .Include(s => s.SensorType)
            .Where(s => s.TenantId == tenantId);

        // Global search
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var term = queryParams.Search.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(term) ||
                (s.Description != null && s.Description.ToLower().Contains(term)) ||
                (s.SerialNumber != null && s.SerialNumber.ToLower().Contains(term)) ||
                (s.SensorType != null && s.SensorType.Name.ToLower().Contains(term)) ||
                (s.SensorType != null && s.SensorType.Code.ToLower().Contains(term)));
        }

        // Filter by sensorTypeId
        if (queryParams.Filters?.TryGetValue("sensorTypeId", out var sensorTypeId) == true
            && Guid.TryParse(sensorTypeId, out var sensorTypeGuid))
        {
            query = query.Where(s => s.SensorTypeId == sensorTypeGuid);
        }

        // Filter by isActive
        if (queryParams.Filters?.TryGetValue("isActive", out var isActiveStr) == true
            && bool.TryParse(isActiveStr, out var isActive))
        {
            query = query.Where(s => s.IsActive == isActive);
        }

        // Total count before paging
        var totalRecords = await query.CountAsync(ct);

        // Sorting
        query = query.ApplySort(queryParams, "Name");

        // Paging
        query = query.ApplyPaging(queryParams);

        var items = await query.ToListAsync(ct);
        var dtos = items.ToDtos();

        return PagedResultDto<SensorDto>.Create(dtos, totalRecords, queryParams);
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
    public async Task<IEnumerable<SensorDto>> GetBySensorTypeAsync(Guid sensorTypeId, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var sensors = await _context.Sensors
            .AsNoTracking()
            .Include(s => s.SensorType)
            .Where(s => s.TenantId == tenantId && s.SensorTypeId == sensorTypeId)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return sensors.ToDtos();
    }

    /// <inheritdoc />
    public async Task<SensorDto> CreateAsync(CreateSensorDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Verify SensorType exists
        var sensorTypeExists = await _context.SensorTypes
            .AsNoTracking()
            .AnyAsync(st => st.Id == dto.SensorTypeId, ct);

        if (!sensorTypeExists)
        {
            throw new InvalidOperationException($"SensorType with Id '{dto.SensorTypeId}' not found.");
        }

        var sensor = dto.ToEntity(tenantId);

        _context.Sensors.Add(sensor);
        await _unitOfWork.SaveChangesAsync(ct);

        // Reload with SensorType
        sensor = await _context.Sensors
            .Include(s => s.SensorType)
            .FirstAsync(s => s.Id == sensor.Id, ct);

        _logger.LogInformation("Sensor created: {Name} (Type: {SensorTypeId})",
            sensor.Name, sensor.SensorTypeId);

        return sensor.ToDto();
    }

    /// <inheritdoc />
    public async Task<SensorDto> UpdateAsync(Guid id, UpdateSensorDto dto, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors
            .Include(s => s.SensorType)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (sensor == null)
        {
            throw new InvalidOperationException($"Sensor with Id '{id}' not found.");
        }

        sensor.ApplyUpdate(dto);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Sensor updated: {SensorId}", id);

        return sensor.ToDto();
    }

    /// <inheritdoc />
    public async Task<SensorDto> CalibrateAsync(Guid id, CalibrateSensorDto dto, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors
            .Include(s => s.SensorType)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (sensor == null)
        {
            throw new InvalidOperationException($"Sensor with Id '{id}' not found.");
        }

        sensor.ApplyCalibration(dto);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Sensor calibrated: {SensorId} (Offset: {Offset}, Gain: {Gain})",
            id, dto.OffsetCorrection, dto.GainCorrection);

        return sensor.ToDto();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (sensor == null)
        {
            throw new InvalidOperationException($"Sensor with Id '{id}' not found.");
        }

        // Check if sensor has assignments
        var hasAssignments = await _context.NodeSensorAssignments
            .AnyAsync(a => a.SensorId == id, ct);

        if (hasAssignments)
        {
            throw new InvalidOperationException("Cannot delete sensor with active assignments. Remove assignments first.");
        }

        _context.Sensors.Remove(sensor);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Sensor deleted: {SensorId}", id);
    }

    /// <inheritdoc />
    public async Task SeedDefaultSensorsAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Get all active SensorTypes with their Capabilities
        var sensorTypes = await _context.SensorTypes
            .AsNoTracking()
            .Include(st => st.Capabilities)
            .Where(st => st.IsActive)
            .ToListAsync(ct);

        // Get existing sensors for this tenant (to avoid duplicates)
        var existingSensorTypeIds = await _context.Sensors
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .Select(s => s.SensorTypeId)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var newSensors = new List<Domain.Entities.Sensor>();

        foreach (var sensorType in sensorTypes)
        {
            // Skip if a sensor of this type already exists
            if (existingSensorTypeIds.Contains(sensorType.Id))
            {
                continue;
            }

            // Get all active capability IDs for this sensor type
            var activeCapabilityIds = sensorType.Capabilities
                .Where(c => c.IsActive)
                .Select(c => c.Id)
                .ToList();

            var sensor = new Domain.Entities.Sensor
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SensorTypeId = sensorType.Id,
                Name = $"{sensorType.Name} #1",
                Description = $"Default sensor for {sensorType.Name}",
                IsActive = true,
                OffsetCorrection = sensorType.DefaultOffsetCorrection,
                GainCorrection = sensorType.DefaultGainCorrection,
                ActiveCapabilityIdsJson = activeCapabilityIds.Count > 0
                    ? System.Text.Json.JsonSerializer.Serialize(activeCapabilityIds)
                    : null,
                CreatedAt = now,
                UpdatedAt = now
            };

            newSensors.Add(sensor);
        }

        if (newSensors.Count > 0)
        {
            await _context.Sensors.AddRangeAsync(newSensors, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("{Count} default Sensors created (one per SensorType)", newSensors.Count);
        }
    }
}
