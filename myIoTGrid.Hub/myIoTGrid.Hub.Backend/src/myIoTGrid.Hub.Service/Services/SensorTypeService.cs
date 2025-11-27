using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Domain.Interfaces;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.Constants;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service für SensorType-Verwaltung
/// </summary>
public class SensorTypeService : ISensorTypeService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SensorTypeService> _logger;

    public SensorTypeService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<SensorTypeService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SensorTypeDto>> GetAllAsync(CancellationToken ct = default)
    {
        var sensorTypes = await _context.SensorTypes
            .AsNoTracking()
            .OrderBy(st => st.Name)
            .ToListAsync(ct);

        return sensorTypes.ToDtos();
    }

    /// <inheritdoc />
    public async Task<SensorTypeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sensorType = await _context.SensorTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(st => st.Id == id, ct);

        return sensorType?.ToDto();
    }

    /// <inheritdoc />
    public async Task<SensorTypeDto?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalizedCode = code.ToLowerInvariant();

        var sensorType = await _context.SensorTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(st => st.Code == normalizedCode, ct);

        return sensorType?.ToDto();
    }

    /// <inheritdoc />
    public async Task<SensorTypeDto> CreateAsync(CreateSensorTypeDto dto, CancellationToken ct = default)
    {
        var normalizedCode = dto.Code.ToLowerInvariant();

        // Prüfen ob Code bereits existiert
        var exists = await _context.SensorTypes
            .AsNoTracking()
            .AnyAsync(st => st.Code == normalizedCode, ct);

        if (exists)
        {
            throw new InvalidOperationException($"SensorType mit Code '{normalizedCode}' existiert bereits.");
        }

        var sensorType = dto.ToEntity();

        _context.SensorTypes.Add(sensorType);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("SensorType erstellt: {Code} ({Name})", sensorType.Code, sensorType.Name);

        return sensorType.ToDto();
    }

    /// <inheritdoc />
    public async Task SyncFromCloudAsync(CancellationToken ct = default)
    {
        // Placeholder für Cloud-Synchronisation
        _logger.LogInformation("Cloud-Sync für SensorTypes ist noch nicht implementiert");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SeedDefaultTypesAsync(CancellationToken ct = default)
    {
        var existingCodes = await _context.SensorTypes
            .AsNoTracking()
            .Select(st => st.Code)
            .ToListAsync(ct);

        var defaultTypes = DefaultSensorTypes.GetAll();
        var newTypes = new List<SensorType>();

        foreach (var dto in defaultTypes)
        {
            if (!existingCodes.Contains(dto.Code))
            {
                var sensorType = new SensorType
                {
                    Id = Guid.NewGuid(),
                    Code = dto.Code,
                    Name = dto.Name,
                    Unit = dto.Unit,
                    Description = dto.Description,
                    IconName = dto.IconName,
                    IsGlobal = true,
                    CreatedAt = DateTime.UtcNow
                };
                newTypes.Add(sensorType);
            }
        }

        if (newTypes.Count > 0)
        {
            await _context.SensorTypes.AddRangeAsync(newTypes, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("{Count} Default-SensorTypes erstellt", newTypes.Count);
        }
    }
}
