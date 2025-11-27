using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Domain.Interfaces;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Infrastructure.Matter;
using myIoTGrid.Hub.Service.Extensions;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for SensorData management
/// </summary>
public class SensorDataService : ISensorDataService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantService _tenantService;
    private readonly IHubService _hubService;
    private readonly ISensorService _sensorService;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly IMatterBridgeClient _matterBridgeClient;
    private readonly ILogger<SensorDataService> _logger;

    public SensorDataService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        ITenantService tenantService,
        IHubService hubService,
        ISensorService sensorService,
        ISignalRNotificationService signalRNotificationService,
        IMatterBridgeClient matterBridgeClient,
        ILogger<SensorDataService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _hubService = hubService;
        _sensorService = sensorService;
        _signalRNotificationService = signalRNotificationService;
        _matterBridgeClient = matterBridgeClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SensorDataDto> CreateAsync(CreateSensorDataDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Determine Hub - use provided HubId or use default
        var hubId = dto.HubId ?? "default-hub";
        var hubDto = await _hubService.GetOrCreateByHubIdAsync(hubId, ct);

        // Get or create Sensor
        var sensorDto = await _sensorService.GetOrCreateBySensorIdAsync(hubDto.Id, dto.SensorId, ct);

        // Validate SensorType
        var sensorType = await _context.SensorTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(st => st.Code == dto.SensorType.ToLowerInvariant(), ct);

        if (sensorType == null)
        {
            throw new InvalidOperationException($"SensorType '{dto.SensorType}' not found. Please create it first.");
        }

        // Create SensorData
        var sensorData = dto.ToEntity(tenantId, sensorDto.Id, sensorType.Id);

        _context.SensorData.Add(sensorData);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogDebug(
            "SensorData created: {SensorType}={Value}{Unit} from {SensorId}",
            sensorType.Code,
            sensorData.Value,
            sensorType.Unit,
            dto.SensorId);

        var sensorDataDto = sensorData.ToDto(sensorType, sensorDto.Location);

        // SignalR Broadcast to all clients in Tenant
        await _signalRNotificationService.NotifyNewSensorDataAsync(tenantId, sensorDataDto, ct);

        // Update Matter Bridge (fire and forget - don't block the main flow)
        _ = UpdateMatterBridgeAsync(sensorDto.SensorId, sensorType.Code, sensorData.Value, ct);

        return sensorDataDto;
    }

    /// <summary>
    /// Updates the Matter Bridge with new sensor data.
    /// This is fire-and-forget to not block the main data flow.
    /// </summary>
    private async Task UpdateMatterBridgeAsync(string sensorId, string sensorType, double value, CancellationToken ct)
    {
        try
        {
            var deviceId = MatterDeviceMapping.GenerateMatterDeviceId(sensorId, sensorType);
            var success = await _matterBridgeClient.UpdateDeviceValueAsync(deviceId, sensorType, value, ct);

            if (success)
            {
                _logger.LogDebug("Updated Matter device {DeviceId} with value {Value}", deviceId, value);
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - Matter Bridge updates should not block sensor data flow
            _logger.LogWarning(ex, "Failed to update Matter Bridge for sensor {SensorId}", sensorId);
        }
    }

    /// <inheritdoc />
    public async Task<SensorDataDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var sensorData = await _context.SensorData
            .AsNoTracking()
            .Include(sd => sd.SensorType)
            .Include(sd => sd.Sensor)
            .FirstOrDefaultAsync(sd => sd.Id == id && sd.TenantId == tenantId, ct);

        return sensorData?.ToDto();
    }

    /// <inheritdoc />
    public async Task<PaginatedResultDto<SensorDataDto>> GetFilteredAsync(SensorDataFilterDto filter, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.SensorData
            .AsNoTracking()
            .Include(sd => sd.SensorType)
            .Include(sd => sd.Sensor)
            .Where(sd => sd.TenantId == tenantId);

        // Apply filters
        if (filter.SensorId.HasValue)
            query = query.Where(sd => sd.SensorId == filter.SensorId.Value);

        if (!string.IsNullOrWhiteSpace(filter.SensorIdentifier))
            query = query.Where(sd => sd.Sensor!.SensorId == filter.SensorIdentifier);

        if (filter.HubId.HasValue)
            query = query.Where(sd => sd.Sensor!.HubId == filter.HubId.Value);

        if (!string.IsNullOrWhiteSpace(filter.SensorTypeCode))
            query = query.Where(sd => sd.SensorType!.Code == filter.SensorTypeCode.ToLowerInvariant());

        if (filter.From.HasValue)
            query = query.Where(sd => sd.Timestamp >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(sd => sd.Timestamp <= filter.To.Value);

        if (filter.IsSyncedToCloud.HasValue)
            query = query.Where(sd => sd.IsSyncedToCloud == filter.IsSyncedToCloud.Value);

        // Sorting (newest first)
        query = query.OrderByDescending(sd => sd.Timestamp);

        // Pagination
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return new PaginatedResultDto<SensorDataDto>(
            items.Select(sd => sd.ToDto()).ToList(),
            totalCount,
            filter.Page,
            filter.PageSize
        );
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SensorDataDto>> GetLatestByHubAsync(Guid sensorId, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Get the latest measurements per SensorType for a Sensor
        var latestData = await _context.SensorData
            .AsNoTracking()
            .Include(sd => sd.SensorType)
            .Include(sd => sd.Sensor)
            .Where(sd => sd.TenantId == tenantId && sd.SensorId == sensorId)
            .GroupBy(sd => sd.SensorTypeId)
            .Select(g => g.OrderByDescending(sd => sd.Timestamp).First())
            .ToListAsync(ct);

        return latestData.ToDtos();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SensorDataDto>> GetLatestAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Get the latest measurements per Sensor and SensorType
        var latestData = await _context.SensorData
            .AsNoTracking()
            .Include(sd => sd.SensorType)
            .Include(sd => sd.Sensor)
            .Where(sd => sd.TenantId == tenantId)
            .GroupBy(sd => new { sd.SensorId, sd.SensorTypeId })
            .Select(g => g.OrderByDescending(sd => sd.Timestamp).First())
            .ToListAsync(ct);

        return latestData.ToDtos();
    }

    /// <inheritdoc />
    public async Task MarkAsSyncedAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return;

        await _context.SensorData
            .Where(sd => idList.Contains(sd.Id))
            .ExecuteUpdateAsync(setters => setters.SetProperty(sd => sd.IsSyncedToCloud, true), ct);

        _logger.LogDebug("{Count} SensorData marked as synced", idList.Count);
    }
}
