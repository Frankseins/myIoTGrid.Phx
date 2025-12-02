using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Domain.Interfaces;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.DTOs.Common;
using myIoTGrid.Hub.Shared.Extensions;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for Reading (Measurement) management.
/// Matter-konform: Entspricht Attribute Reports.
/// New model: Uses EndpointId + MeasurementType to find Assignment, applies calibration.
/// </summary>
public class ReadingService : IReadingService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INodeService _nodeService;
    private readonly ITenantService _tenantService;
    private readonly IHubService _hubService;
    private readonly IEffectiveConfigService _effectiveConfigService;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly ILogger<ReadingService> _logger;

    public ReadingService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        INodeService nodeService,
        ITenantService tenantService,
        IHubService hubService,
        IEffectiveConfigService effectiveConfigService,
        ISignalRNotificationService signalRNotificationService,
        ILogger<ReadingService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _nodeService = nodeService;
        _tenantService = tenantService;
        _hubService = hubService;
        _effectiveConfigService = effectiveConfigService;
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

        // Find Assignment by EndpointId on this Node
        var assignment = await _context.NodeSensorAssignments
            .Include(a => a.Sensor)
                .ThenInclude(s => s.SensorType)
                    .ThenInclude(st => st.Capabilities)
            .FirstOrDefaultAsync(a => a.NodeId == node.Id && a.EndpointId == dto.EndpointId, ct);

        if (assignment == null)
        {
            _logger.LogWarning(
                "No assignment found for Node {NodeId} EndpointId {EndpointId}. Creating reading with unknown assignment.",
                dto.NodeId, dto.EndpointId);

            // Create a reading without assignment (will need manual assignment later)
            var unknownReading = new Reading
            {
                TenantId = tenantId,
                NodeId = node.Id,
                AssignmentId = Guid.Empty, // No assignment
                MeasurementType = dto.MeasurementType.ToLowerInvariant(),
                RawValue = dto.RawValue,
                Value = dto.RawValue, // No calibration without assignment
                Unit = string.Empty,
                Timestamp = dto.Timestamp ?? DateTime.UtcNow,
                IsSyncedToCloud = false
            };

            _context.Readings.Add(unknownReading);
            await _unitOfWork.SaveChangesAsync(ct);

            // Update Node LastSeen
            await _nodeService.UpdateLastSeenAsync(node.Id, ct);

            // Reload with navigation properties
            unknownReading = await _context.Readings
                .Include(r => r.Node)
                    .ThenInclude(n => n!.Location)
                .FirstAsync(r => r.Id == unknownReading.Id, ct);

            return unknownReading.ToDto();
        }

        // Get Sensor and SensorType from assignment
        var sensor = assignment.Sensor!;
        var sensorType = sensor.SensorType!;

        // Apply calibration: CalibratedValue = (RawValue * Gain) + Offset
        var calibratedValue = _effectiveConfigService.ApplyCalibration(dto.RawValue, sensor, sensorType);

        // Get unit from capability (MeasurementType)
        var capability = sensorType.Capabilities
            .FirstOrDefault(c => c.MeasurementType.Equals(dto.MeasurementType, StringComparison.OrdinalIgnoreCase));
        var unit = capability?.Unit ?? string.Empty;

        // Create reading with calibration applied
        var reading = dto.ToEntity(
            tenantId: tenantId,
            nodeId: node.Id,
            assignmentId: assignment.Id,
            unit: unit,
            calibratedValue: calibratedValue
        );

        _context.Readings.Add(reading);
        await _unitOfWork.SaveChangesAsync(ct);

        // Update Node LastSeen
        await _nodeService.UpdateLastSeenAsync(node.Id, ct);

        // Reload with navigation properties
        reading = await _context.Readings
            .Include(r => r.Node)
                .ThenInclude(n => n!.Location)
            .Include(r => r.Assignment)
            .FirstAsync(r => r.Id == reading.Id, ct);

        var readingDto = reading.ToDto();

        // SignalR Notification
        await _signalRNotificationService.NotifyNewReadingAsync(readingDto, ct);

        _logger.LogDebug("Reading created: {NodeId} Endpoint={EndpointId} {Type}={RawValue} -> {CalibratedValue} {Unit}",
            dto.NodeId, dto.EndpointId, dto.MeasurementType, dto.RawValue, calibratedValue, unit);

        return readingDto;
    }

    /// <inheritdoc />
    public async Task<ReadingDto> CreateFromSensorAsync(CreateSensorReadingDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // DeviceId from sensor is the Node's internal GUID (returned from registration)
        if (!Guid.TryParse(dto.DeviceId, out var nodeGuid))
        {
            throw new InvalidOperationException($"Invalid DeviceId format: {dto.DeviceId}. Expected GUID.");
        }

        // Find Node by GUID
        var node = await _context.Nodes
            .FirstOrDefaultAsync(n => n.Id == nodeGuid, ct);

        if (node == null)
        {
            throw new InvalidOperationException($"Node not found: {dto.DeviceId}");
        }

        // Convert timestamp (Unix seconds to DateTime)
        var timestamp = dto.Timestamp.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(dto.Timestamp.Value).UtcDateTime
            : DateTime.UtcNow;

        // Create reading without assignment lookup (simplified sensor flow)
        var reading = new Reading
        {
            TenantId = tenantId,
            NodeId = node.Id,
            AssignmentId = null, // Sensor readings without assignments
            MeasurementType = dto.Type.ToLowerInvariant(),
            RawValue = dto.Value,
            Value = dto.Value, // No calibration without assignment
            Unit = dto.Unit ?? string.Empty,
            Timestamp = timestamp,
            IsSyncedToCloud = false
        };

        _context.Readings.Add(reading);
        await _unitOfWork.SaveChangesAsync(ct);

        // Update Node LastSeen
        await _nodeService.UpdateLastSeenAsync(node.Id, ct);

        // Reload with navigation properties
        reading = await _context.Readings
            .Include(r => r.Node)
                .ThenInclude(n => n!.Location)
            .FirstAsync(r => r.Id == reading.Id, ct);

        var readingDto = reading.ToDto();

        // SignalR Notification
        await _signalRNotificationService.NotifyNewReadingAsync(readingDto, ct);

        _logger.LogDebug("Sensor reading created: {NodeId} {Type}={Value} {Unit}",
            node.NodeId, dto.Type, dto.Value, dto.Unit ?? "");

        return readingDto;
    }

    /// <inheritdoc />
    public async Task<ReadingDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var reading = await _context.Readings
            .AsNoTracking()
            .Include(r => r.Node)
                .ThenInclude(n => n!.Location)
            .Include(r => r.Assignment)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        return reading?.ToDto();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ReadingDto>> GetByNodeAsync(Guid nodeId, ReadingFilterDto? filter = null, CancellationToken ct = default)
    {
        var query = _context.Readings
            .AsNoTracking()
            .Include(r => r.Node)
                .ThenInclude(n => n!.Location)
            .Include(r => r.Assignment)
            .Where(r => r.NodeId == nodeId);

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.MeasurementType))
                query = query.Where(r => r.MeasurementType == filter.MeasurementType.ToLowerInvariant());

            if (filter.AssignmentId.HasValue)
                query = query.Where(r => r.AssignmentId == filter.AssignmentId.Value);

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
                .ThenInclude(n => n!.Location)
            .Include(r => r.Assignment)
            .Where(r => r.TenantId == tenantId);

        // Apply filters
        if (filter.NodeId.HasValue)
            query = query.Where(r => r.NodeId == filter.NodeId.Value);

        if (!string.IsNullOrEmpty(filter.NodeIdentifier))
            query = query.Where(r => r.Node != null && r.Node.NodeId == filter.NodeIdentifier);

        if (filter.HubId.HasValue)
            query = query.Where(r => r.Node != null && r.Node.HubId == filter.HubId.Value);

        if (filter.AssignmentId.HasValue)
            query = query.Where(r => r.AssignmentId == filter.AssignmentId.Value);

        if (!string.IsNullOrEmpty(filter.MeasurementType))
            query = query.Where(r => r.MeasurementType == filter.MeasurementType.ToLowerInvariant());

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
    public async Task<PagedResultDto<ReadingDto>> GetPagedAsync(QueryParamsDto queryParams, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Readings
            .AsNoTracking()
            .Include(r => r.Node)
                .ThenInclude(n => n!.Location)
            .Include(r => r.Assignment)
            .Where(r => r.TenantId == tenantId);

        // Global search (MeasurementType, Unit)
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            query = query.ApplySearch(
                queryParams.Search,
                r => r.MeasurementType,
                r => r.Unit);
        }

        // Filter by NodeId
        if (queryParams.Filters?.TryGetValue("nodeId", out var nodeIdFilter) == true &&
            Guid.TryParse(nodeIdFilter, out var nodeId))
        {
            query = query.Where(r => r.NodeId == nodeId);
        }

        // Filter by HubId (via Node)
        if (queryParams.Filters?.TryGetValue("hubId", out var hubIdFilter) == true &&
            Guid.TryParse(hubIdFilter, out var hubId))
        {
            query = query.Where(r => r.Node != null && r.Node.HubId == hubId);
        }

        // Filter by MeasurementType
        if (queryParams.Filters?.TryGetValue("measurementType", out var measurementTypeFilter) == true &&
            !string.IsNullOrEmpty(measurementTypeFilter))
        {
            query = query.Where(r => r.MeasurementType == measurementTypeFilter.ToLowerInvariant());
        }

        // Filter by AssignmentId
        if (queryParams.Filters?.TryGetValue("assignmentId", out var assignmentIdFilter) == true &&
            Guid.TryParse(assignmentIdFilter, out var assignmentId))
        {
            query = query.Where(r => r.AssignmentId == assignmentId);
        }

        // Filter by IsSyncedToCloud
        if (queryParams.Filters?.TryGetValue("isSyncedToCloud", out var syncedFilter) == true &&
            bool.TryParse(syncedFilter, out var isSynced))
        {
            query = query.Where(r => r.IsSyncedToCloud == isSynced);
        }

        // Date filter on Timestamp
        query = query.ApplyDateFilter(queryParams, r => r.Timestamp);

        // Total count before paging
        var totalRecords = await query.CountAsync(ct);

        // Apply sorting (default: Timestamp descending)
        var (sortField, ascending) = queryParams.ParseSort();
        if (string.IsNullOrWhiteSpace(sortField))
        {
            query = query.OrderByDescending(r => r.Timestamp);
        }
        else
        {
            query = query.ApplySort(queryParams, "Timestamp");
        }

        // Apply paging
        query = query.ApplyPaging(queryParams);

        var items = await query.ToListAsync(ct);

        return PagedResultDto<ReadingDto>.Create(
            items.ToDtos(),
            totalRecords,
            queryParams);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ReadingDto>> GetLatestByNodeAsync(Guid nodeId, CancellationToken ct = default)
    {
        // Get the latest reading for each MeasurementType
        var latestReadings = await _context.Readings
            .AsNoTracking()
            .Include(r => r.Node)
                .ThenInclude(n => n!.Location)
            .Include(r => r.Assignment)
            .Where(r => r.NodeId == nodeId)
            .GroupBy(r => r.MeasurementType)
            .Select(g => g.OrderByDescending(r => r.Timestamp).First())
            .ToListAsync(ct);

        return latestReadings.ToDtos();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ReadingDto>> GetLatestAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Get the latest reading for each Node+MeasurementType combination
        var latestReadings = await _context.Readings
            .AsNoTracking()
            .Include(r => r.Node)
                .ThenInclude(n => n!.Location)
            .Include(r => r.Assignment)
            .Where(r => r.TenantId == tenantId)
            .GroupBy(r => new { r.NodeId, r.MeasurementType })
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
                .ThenInclude(n => n!.Location)
            .Include(r => r.Assignment)
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

        // Single-Hub-Architecture: Use the current hub for this tenant
        return await _hubService.GetCurrentHubAsync(ct);
    }
}
