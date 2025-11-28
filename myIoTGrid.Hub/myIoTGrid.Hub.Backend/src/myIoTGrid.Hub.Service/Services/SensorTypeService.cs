using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
/// Service for SensorType management.
/// Matter-konform: Entspricht Matter Clusters.
/// </summary>
public class SensorTypeService : ISensorTypeService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SensorTypeService> _logger;

    private const string CacheKey = "SensorTypes_All";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public SensorTypeService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        IMemoryCache cache,
        ILogger<SensorTypeService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SensorTypeDto>> GetAllAsync(CancellationToken ct = default)
    {
        var sensorTypes = await _context.SensorTypes
            .AsNoTracking()
            .OrderBy(st => st.Category)
            .ThenBy(st => st.DisplayName)
            .ToListAsync(ct);

        return sensorTypes.ToDtos();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SensorTypeDto>> GetAllCachedAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out IEnumerable<SensorTypeDto>? cached) && cached != null)
        {
            return cached;
        }

        var sensorTypes = await GetAllAsync(ct);
        var sensorTypeList = sensorTypes.ToList();

        _cache.Set(CacheKey, sensorTypeList, CacheDuration);

        return sensorTypeList;
    }

    /// <inheritdoc />
    public async Task<SensorTypeDto?> GetByTypeIdAsync(string typeId, CancellationToken ct = default)
    {
        var normalizedTypeId = typeId.ToLowerInvariant();

        var sensorType = await _context.SensorTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(st => st.TypeId == normalizedTypeId, ct);

        return sensorType?.ToDto();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SensorTypeDto>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        var normalizedCategory = category.ToLowerInvariant();

        var sensorTypes = await _context.SensorTypes
            .AsNoTracking()
            .Where(st => st.Category == normalizedCategory)
            .OrderBy(st => st.DisplayName)
            .ToListAsync(ct);

        return sensorTypes.ToDtos();
    }

    /// <inheritdoc />
    public async Task<string> GetUnitAsync(string typeId, CancellationToken ct = default)
    {
        var normalizedTypeId = typeId.ToLowerInvariant();

        var unit = await _context.SensorTypes
            .AsNoTracking()
            .Where(st => st.TypeId == normalizedTypeId)
            .Select(st => st.Unit)
            .FirstOrDefaultAsync(ct);

        return unit ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task<SensorTypeDto> CreateAsync(CreateSensorTypeDto dto, CancellationToken ct = default)
    {
        var normalizedTypeId = dto.TypeId.ToLowerInvariant();

        // Check if TypeId already exists
        var exists = await _context.SensorTypes
            .AsNoTracking()
            .AnyAsync(st => st.TypeId == normalizedTypeId, ct);

        if (exists)
        {
            throw new InvalidOperationException($"SensorType with TypeId '{normalizedTypeId}' already exists.");
        }

        var sensorType = dto.ToEntity();

        _context.SensorTypes.Add(sensorType);
        await _unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        _cache.Remove(CacheKey);

        _logger.LogInformation("SensorType created: {TypeId} ({DisplayName}) ClusterId=0x{ClusterId:X4}",
            sensorType.TypeId, sensorType.DisplayName, sensorType.ClusterId);

        return sensorType.ToDto();
    }

    /// <inheritdoc />
    public async Task SyncFromCloudAsync(CancellationToken ct = default)
    {
        // Placeholder for Cloud synchronization
        _logger.LogInformation("Cloud-Sync for SensorTypes is not yet implemented");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SeedDefaultTypesAsync(CancellationToken ct = default)
    {
        var existingTypeIds = await _context.SensorTypes
            .AsNoTracking()
            .Select(st => st.TypeId)
            .ToListAsync(ct);

        var defaultTypes = DefaultSensorTypes.GetAll();
        var newTypes = new List<SensorType>();

        foreach (var dto in defaultTypes)
        {
            if (!existingTypeIds.Contains(dto.TypeId))
            {
                var sensorType = new SensorType
                {
                    TypeId = dto.TypeId,
                    DisplayName = dto.DisplayName,
                    ClusterId = dto.ClusterId,
                    MatterClusterName = dto.MatterClusterName,
                    Unit = dto.Unit,
                    Resolution = dto.Resolution,
                    MinValue = dto.MinValue,
                    MaxValue = dto.MaxValue,
                    Description = dto.Description,
                    IsCustom = dto.IsCustom,
                    Category = dto.Category,
                    Icon = dto.Icon,
                    Color = dto.Color,
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

            // Invalidate cache
            _cache.Remove(CacheKey);

            _logger.LogInformation("{Count} default SensorTypes created with Matter Cluster IDs", newTypes.Count);
        }
    }
}
