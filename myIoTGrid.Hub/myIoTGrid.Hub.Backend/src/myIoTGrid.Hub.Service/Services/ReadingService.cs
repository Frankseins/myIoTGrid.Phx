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
/// Service for Reading (Measurement) management.
/// Matter-konform: Entspricht Attribute Reports.
/// </summary>
public class ReadingService : IReadingService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INodeService _nodeService;
    private readonly ITenantService _tenantService;
    private readonly IHubService _hubService;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly ILogger<ReadingService> _logger;

    public ReadingService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        INodeService nodeService,
        ITenantService tenantService,
        IHubService hubService,
        ISignalRNotificationService signalRNotificationService,
        ILogger<ReadingService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _nodeService = nodeService;
        _tenantService = tenantService;
        _hubService = hubService;
        _signalRNotificationService = signalRNotificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ReadingDto> CreateAsync(CreateReadingDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Find Hub (use provided or default)
        var hub = await GetOrCreateHubAsync(dto.HubId, tenantId, ct);

        // Find or create Node (auto-registration)
        var node = await _nodeService.GetOrCreateByNodeIdAsync(hub.Id, dto.NodeId, ct);

        // Get SensorType for unit
        var sensorType = await _context.SensorTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(st => st.TypeId == dto.Type.ToLowerInvariant(), ct);

        if (sensorType == null)
        {
            _logger.LogWarning("Unknown SensorType: {SensorType}. Using raw type.", dto.Type);
        }

        var reading = new Reading
        {
            TenantId = tenantId,
            NodeId = node.Id,
            SensorTypeId = dto.Type.ToLowerInvariant(),
            Value = dto.Value,
            Timestamp = dto.Timestamp ?? DateTime.UtcNow,
            IsSyncedToCloud = false
        };

        _context.Readings.Add(reading);
        await _unitOfWork.SaveChangesAsync(ct);

        // Update Node LastSeen
        await _nodeService.UpdateLastSeenAsync(node.Id, ct);

        // Reload with navigation properties
        reading = await _context.Readings
            .Include(r => r.Node)
            .Include(r => r.SensorType)
            .FirstAsync(r => r.Id == reading.Id, ct);

        var readingDto = reading.ToDto(node.Location);

        // SignalR Notification
        await _signalRNotificationService.NotifyNewReadingAsync(readingDto, ct);

        _logger.LogDebug("Reading created: {NodeId} {Type}={Value}",
            dto.NodeId, dto.Type, dto.Value);

        return readingDto;
    }

    /// <inheritdoc />
    public async Task<ReadingDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var reading = await _context.Readings
            .AsNoTracking()
            .Include(r => r.Node)
            .Include(r => r.SensorType)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        return reading?.ToDto();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ReadingDto>> GetByNodeAsync(Guid nodeId, ReadingFilterDto? filter = null, CancellationToken ct = default)
    {
        var query = _context.Readings
            .AsNoTracking()
            .Include(r => r.Node)
            .Include(r => r.SensorType)
            .Where(r => r.NodeId == nodeId);

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.SensorTypeId))
                query = query.Where(r => r.SensorTypeId == filter.SensorTypeId.ToLowerInvariant());

            if (filter.From.HasValue)
                query = query.Where(r => r.Timestamp >= filter.From.Value);

            if (filter.To.HasValue)
                query = query.Where(r => r.Timestamp <= filter.To.Value);
        }

        var readings = await query
            .OrderByDescending(r => r.Timestamp)
            .Take(filter?.PageSize ?? 50)
            .ToListAsync(ct);

        return readings.ToDtos();
    }

    /// <inheritdoc />
    public async Task<PaginatedResultDto<ReadingDto>> GetFilteredAsync(ReadingFilterDto filter, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Readings
            .AsNoTracking()
            .Include(r => r.Node)
            .Include(r => r.SensorType)
            .Where(r => r.TenantId == tenantId);

        // Apply filters
        if (filter.NodeId.HasValue)
            query = query.Where(r => r.NodeId == filter.NodeId.Value);

        if (!string.IsNullOrEmpty(filter.NodeIdentifier))
            query = query.Where(r => r.Node != null && r.Node.NodeId == filter.NodeIdentifier);

        if (filter.HubId.HasValue)
            query = query.Where(r => r.Node != null && r.Node.HubId == filter.HubId.Value);

        if (!string.IsNullOrEmpty(filter.SensorTypeId))
            query = query.Where(r => r.SensorTypeId == filter.SensorTypeId.ToLowerInvariant());

        if (filter.From.HasValue)
            query = query.Where(r => r.Timestamp >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(r => r.Timestamp <= filter.To.Value);

        if (filter.IsSyncedToCloud.HasValue)
            query = query.Where(r => r.IsSyncedToCloud == filter.IsSyncedToCloud.Value);

        // Count total
        var totalCount = await query.CountAsync(ct);

        // Paginate
        var readings = await query
            .OrderByDescending(r => r.Timestamp)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return new PaginatedResultDto<ReadingDto>(
            Items: readings.ToDtos().ToList(),
            TotalCount: totalCount,
            Page: filter.Page,
            PageSize: filter.PageSize
        );
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ReadingDto>> GetLatestByNodeAsync(Guid nodeId, CancellationToken ct = default)
    {
        // Get the latest reading for each SensorType
        var latestReadings = await _context.Readings
            .AsNoTracking()
            .Include(r => r.Node)
            .Include(r => r.SensorType)
            .Where(r => r.NodeId == nodeId)
            .GroupBy(r => r.SensorTypeId)
            .Select(g => g.OrderByDescending(r => r.Timestamp).First())
            .ToListAsync(ct);

        return latestReadings.ToDtos();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ReadingDto>> GetLatestAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Get the latest reading for each Node+SensorType combination
        var latestReadings = await _context.Readings
            .AsNoTracking()
            .Include(r => r.Node)
            .Include(r => r.SensorType)
            .Where(r => r.TenantId == tenantId)
            .GroupBy(r => new { r.NodeId, r.SensorTypeId })
            .Select(g => g.OrderByDescending(r => r.Timestamp).First())
            .ToListAsync(ct);

        return latestReadings.ToDtos();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ReadingDto>> GetUnsyncedAsync(int limit = 100, CancellationToken ct = default)
    {
        var readings = await _context.Readings
            .AsNoTracking()
            .Include(r => r.Node)
            .Include(r => r.SensorType)
            .Where(r => !r.IsSyncedToCloud)
            .OrderBy(r => r.Timestamp)
            .Take(limit)
            .ToListAsync(ct);

        return readings.ToDtos();
    }

    /// <inheritdoc />
    public async Task MarkAsSyncedAsync(IEnumerable<long> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return;

        await _context.Readings
            .Where(r => idList.Contains(r.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsSyncedToCloud, true), ct);

        _logger.LogInformation("Marked {Count} readings as synced to cloud", idList.Count);
    }

    private async Task<HubDto> GetOrCreateHubAsync(string? hubId, Guid tenantId, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(hubId))
        {
            return await _hubService.GetOrCreateByHubIdAsync(hubId, ct);
        }

        // Use default hub for tenant
        var defaultHub = await _hubService.GetDefaultHubAsync(ct);
        if (defaultHub != null)
            return defaultHub;

        // Create default hub
        return await _hubService.GetOrCreateByHubIdAsync("default-hub", ct);
    }
}
