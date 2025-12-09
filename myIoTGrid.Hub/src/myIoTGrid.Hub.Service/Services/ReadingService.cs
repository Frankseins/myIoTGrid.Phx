using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for Reading (Measurement) management (v3.0).
/// Matter-konform: Entspricht Attribute Reports.
/// Two-tier model: Sensor → NodeSensorAssignment
/// Uses EndpointId + MeasurementType to find Assignment, applies calibration from Sensor.
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

        // Find Assignment by EndpointId on this Node (v3.0: direct Sensor reference with Capabilities)
        var assignment = await _context.NodeSensorAssignments
            .Include(a => a.Sensor)
                .ThenInclude(s => s.Capabilities)
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
                AssignmentId = null, // No assignment
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

        // Get Sensor from assignment (v3.0: Sensor contains all configuration)
        var sensor = assignment.Sensor!;

        // Apply calibration: CalibratedValue = (RawValue * Gain) + Offset
        var calibratedValue = _effectiveConfigService.ApplyCalibration(dto.RawValue, sensor);

        // Get unit from capability (MeasurementType) - now on Sensor directly
        var capability = sensor.Capabilities
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
                .ThenInclude(a => a!.Sensor)
                    .ThenInclude(s => s!.Capabilities)
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

        // DeviceId from sensor can be either:
        // 1. GUID (internal Node ID)
        // 2. NodeId/SerialNumber (e.g., SIM-8F470D6C-0001 or ESP-AABBCCDD)
        Node? node = null;

        if (Guid.TryParse(dto.DeviceId, out var nodeGuid))
        {
            // Find Node by GUID
            node = await _context.Nodes
                .Include(n => n.Hub)
                .FirstOrDefaultAsync(n => n.Id == nodeGuid && n.Hub!.TenantId == tenantId, ct);
        }
        else
        {
            // Find Node by NodeId (SerialNumber)
            node = await _context.Nodes
                .Include(n => n.Hub)
                .FirstOrDefaultAsync(n => n.NodeId == dto.DeviceId && n.Hub!.TenantId == tenantId, ct);
        }

        if (node == null)
        {
            throw new InvalidOperationException($"Node not found: {dto.DeviceId}");
        }

        // Convert timestamp (Unix seconds to DateTime)
        var timestamp = dto.Timestamp.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(dto.Timestamp.Value).UtcDateTime
            : DateTime.UtcNow;

        var measurementType = dto.Type.ToLowerInvariant();

        // Try to find Assignment - prioritize EndpointId if provided, otherwise match by MeasurementType
        NodeSensorAssignment? assignment;

        if (dto.EndpointId.HasValue)
        {
            // Use EndpointId to find the specific assignment (more precise when multiple sensors have same capability)
            assignment = await _context.NodeSensorAssignments
                .Include(a => a.Sensor)
                    .ThenInclude(s => s.Capabilities)
                .Where(a => a.NodeId == node.Id && a.IsActive && a.EndpointId == dto.EndpointId.Value)
                .FirstOrDefaultAsync(ct);
        }
        else
        {
            // Fallback: match by MeasurementType to Sensor Capabilities
            assignment = await _context.NodeSensorAssignments
                .Include(a => a.Sensor)
                    .ThenInclude(s => s.Capabilities)
                .Where(a => a.NodeId == node.Id && a.IsActive)
                .Where(a => a.Sensor.Capabilities.Any(c =>
                    c.MeasurementType.ToLower() == measurementType))
                .FirstOrDefaultAsync(ct);
        }

        Guid? assignmentId = assignment?.Id;
        var sensor = assignment?.Sensor;
        var unit = dto.Unit ?? string.Empty;
        var calibratedValue = dto.Value;
        SensorCapability? capabilityForMapping = null;

        if (assignment != null && sensor != null)
        {
            // Apply calibration if we have an assignment
            calibratedValue = _effectiveConfigService.ApplyCalibration(dto.Value, sensor);

            // Get unit from capability
            var capability = sensor.Capabilities
                .FirstOrDefault(c => c.MeasurementType.Equals(measurementType, StringComparison.OrdinalIgnoreCase));
            if (capability != null && string.IsNullOrEmpty(dto.Unit))
            {
                unit = capability.Unit ?? string.Empty;
            }

            _logger.LogDebug("Found assignment {AssignmentId} for {NodeId} {MeasurementType}",
                assignmentId, node.NodeId, measurementType);
        }
        else
        {
            // No assignment found - try to find a capability from any sensor that matches this measurementType
            // This allows us to display a proper name like "Temperatur" instead of "temperature"
            capabilityForMapping = await _context.SensorCapabilities
                .Include(c => c.Sensor)
                .Where(c => c.MeasurementType.ToLower() == measurementType)
                .FirstOrDefaultAsync(ct);

            if (capabilityForMapping != null && string.IsNullOrEmpty(dto.Unit))
            {
                unit = capabilityForMapping.Unit ?? string.Empty;
            }

            _logger.LogDebug("No assignment found for {NodeId} {MeasurementType}, using capability lookup (found: {Found})",
                node.NodeId, measurementType, capabilityForMapping != null);
        }

        // Create reading with assignment if found
        var reading = new Reading
        {
            TenantId = tenantId,
            NodeId = node.Id,
            AssignmentId = assignmentId,
            MeasurementType = measurementType,
            RawValue = dto.Value,
            Value = calibratedValue,
            Unit = unit,
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
            .Include(r => r.Assignment)
                .ThenInclude(a => a!.Sensor)
                    .ThenInclude(s => s!.Capabilities)
            .FirstAsync(r => r.Id == reading.Id, ct);

        // Use capability lookup for mapping if no assignment (to get proper DisplayName)
        var readingDto = reading.ToDto(capabilityForMapping);

        // SignalR Notification
        await _signalRNotificationService.NotifyNewReadingAsync(readingDto, ct);

        _logger.LogDebug("Sensor reading created: {NodeId} {Type}={RawValue} -> {CalibratedValue} {Unit} (Assignment: {AssignmentId})",
            node.NodeId, dto.Type, dto.Value, calibratedValue, unit, assignmentId?.ToString() ?? "none");

        return readingDto;
    }

    /// <inheritdoc />
    public async Task<BatchReadingsResultDto> CreateBatchAsync(CreateBatchReadingsDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var errors = new List<string>();
        var successCount = 0;
        var failedCount = 0;
        var readings = dto.Readings.ToList();

        _logger.LogInformation("Processing batch upload: {Count} readings from Node {NodeId}",
            readings.Count, dto.NodeId);

        // Find Node (required for batch upload)
        Node? node = null;
        if (Guid.TryParse(dto.NodeId, out var nodeGuid))
        {
            node = await _context.Nodes
                .Include(n => n.Hub)
                .FirstOrDefaultAsync(n => n.Id == nodeGuid && n.Hub!.TenantId == tenantId, ct);
        }
        else
        {
            node = await _context.Nodes
                .Include(n => n.Hub)
                .FirstOrDefaultAsync(n => n.NodeId == dto.NodeId && n.Hub!.TenantId == tenantId, ct);
        }

        if (node == null)
        {
            return new BatchReadingsResultDto(
                SuccessCount: 0,
                FailedCount: readings.Count,
                TotalCount: readings.Count,
                NodeId: dto.NodeId,
                ProcessedAt: DateTime.UtcNow,
                Errors: new[] { $"Node not found: {dto.NodeId}" }
            );
        }

        // Pre-load all assignments for this node (for performance)
        var assignments = await _context.NodeSensorAssignments
            .Include(a => a.Sensor)
                .ThenInclude(s => s.Capabilities)
            .Where(a => a.NodeId == node.Id && a.IsActive)
            .ToListAsync(ct);

        // Process each reading in the batch
        var baseTimestamp = dto.Timestamp ?? DateTime.UtcNow;
        var readingsToAdd = new List<Reading>();

        foreach (var readingValue in readings)
        {
            try
            {
                var measurementType = readingValue.MeasurementType.ToLowerInvariant();

                // Find assignment by EndpointId
                var assignment = assignments.FirstOrDefault(a => a.EndpointId == readingValue.EndpointId);

                Guid? assignmentId = assignment?.Id;
                var sensor = assignment?.Sensor;
                var calibratedValue = readingValue.RawValue;
                var unit = string.Empty;

                if (assignment != null && sensor != null)
                {
                    // Apply calibration
                    calibratedValue = _effectiveConfigService.ApplyCalibration(readingValue.RawValue, sensor);

                    // Get unit from capability
                    var capability = sensor.Capabilities
                        .FirstOrDefault(c => c.MeasurementType.Equals(measurementType, StringComparison.OrdinalIgnoreCase));
                    unit = capability?.Unit ?? string.Empty;
                }

                var reading = new Reading
                {
                    TenantId = tenantId,
                    NodeId = node.Id,
                    AssignmentId = assignmentId,
                    MeasurementType = measurementType,
                    RawValue = readingValue.RawValue,
                    Value = calibratedValue,
                    Unit = unit,
                    Timestamp = baseTimestamp,
                    IsSyncedToCloud = false
                };

                readingsToAdd.Add(reading);
                successCount++;
            }
            catch (Exception ex)
            {
                failedCount++;
                errors.Add($"EndpointId {readingValue.EndpointId} ({readingValue.MeasurementType}): {ex.Message}");
                _logger.LogWarning(ex, "Failed to process batch reading: EndpointId={EndpointId}, Type={Type}",
                    readingValue.EndpointId, readingValue.MeasurementType);
            }
        }

        // Bulk insert all readings
        if (readingsToAdd.Count > 0)
        {
            _context.Readings.AddRange(readingsToAdd);
            await _unitOfWork.SaveChangesAsync(ct);

            // Update Node LastSeen and sync status
            node.LastSeen = DateTime.UtcNow;
            node.LastSyncAt = DateTime.UtcNow;
            node.PendingSyncCount = Math.Max(0, node.PendingSyncCount - successCount);
            node.LastSyncError = failedCount > 0 ? $"{failedCount} readings failed" : null;
            await _unitOfWork.SaveChangesAsync(ct);

            // SignalR notification for latest readings only (to avoid flooding)
            var latestByType = readingsToAdd
                .GroupBy(r => r.MeasurementType)
                .Select(g => g.OrderByDescending(r => r.Timestamp).First());

            foreach (var reading in latestByType)
            {
                // Reload with navigation properties for DTO conversion
                var reloadedReading = await _context.Readings
                    .Include(r => r.Node)
                        .ThenInclude(n => n!.Location)
                    .Include(r => r.Assignment)
                        .ThenInclude(a => a!.Sensor)
                            .ThenInclude(s => s!.Capabilities)
                    .FirstOrDefaultAsync(r => r.Id == reading.Id, ct);

                if (reloadedReading != null)
                {
                    await _signalRNotificationService.NotifyNewReadingAsync(reloadedReading.ToDto(), ct);
                }
            }
        }

        _logger.LogInformation("Batch upload completed: {Success}/{Total} readings from Node {NodeId}",
            successCount, readings.Count, dto.NodeId);

        return new BatchReadingsResultDto(
            SuccessCount: successCount,
            FailedCount: failedCount,
            TotalCount: readings.Count,
            NodeId: dto.NodeId,
            ProcessedAt: DateTime.UtcNow,
            Errors: errors.Count > 0 ? errors : null
        );
    }

    /// <inheritdoc />
    public async Task<ReadingDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var reading = await _context.Readings
            .AsNoTracking()
            .Include(r => r.Node)
                .ThenInclude(n => n!.Location)
            .Include(r => r.Assignment)
                .ThenInclude(a => a!.Sensor)
                    .ThenInclude(s => s!.Capabilities)
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
                .ThenInclude(a => a!.Sensor)
                    .ThenInclude(s => s!.Capabilities)
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
                .ThenInclude(a => a!.Sensor)
                    .ThenInclude(s => s!.Capabilities)
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
                .ThenInclude(a => a!.Sensor)
                    .ThenInclude(s => s!.Capabilities)
            .Where(r => r.TenantId == tenantId);

        // Global search (MeasurementType, Unit, Sensor Name/Code)
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var term = queryParams.Search.ToLower();
            query = query.Where(r =>
                r.MeasurementType.ToLower().Contains(term) ||
                r.Unit.ToLower().Contains(term) ||
                (r.Assignment != null && r.Assignment.Sensor != null &&
                    (r.Assignment.Sensor.Name.ToLower().Contains(term) ||
                     r.Assignment.Sensor.Code.ToLower().Contains(term))));
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
                .ThenInclude(a => a!.Sensor)
                    .ThenInclude(s => s!.Capabilities)
            .Where(r => r.NodeId == nodeId)
            .GroupBy(r => r.MeasurementType)
            .Select(g => g.OrderByDescending(r => r.Timestamp).First())
            .ToListAsync(ct);

        // Get capability lookup for DisplayName resolution
        var capabilityLookup = await GetCapabilityLookupAsync(ct);
        return latestReadings.ToDtos(capabilityLookup);
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
                .ThenInclude(a => a!.Sensor)
                    .ThenInclude(s => s!.Capabilities)
            .Where(r => r.TenantId == tenantId)
            .GroupBy(r => new { r.NodeId, r.MeasurementType })
            .Select(g => g.OrderByDescending(r => r.Timestamp).First())
            .ToListAsync(ct);

        // Get capability lookup for DisplayName resolution
        var capabilityLookup = await GetCapabilityLookupAsync(ct);
        return latestReadings.ToDtos(capabilityLookup);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ReadingDto>> GetUnsyncedAsync(int limit = 100, CancellationToken ct = default)
    {
        var readings = await _context.Readings
            .AsNoTracking()
            .Include(r => r.Node)
                .ThenInclude(n => n!.Location)
            .Include(r => r.Assignment)
                .ThenInclude(a => a!.Sensor)
                    .ThenInclude(s => s!.Capabilities)
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

    /// <inheritdoc />
    public async Task<DeleteReadingsResultDto> DeleteRangeAsync(DeleteReadingsRangeDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Build query with required filters
        var query = _context.Readings
            .Where(r => r.TenantId == tenantId)
            .Where(r => r.NodeId == dto.NodeId)
            .Where(r => r.Timestamp >= dto.From && r.Timestamp <= dto.To);

        // Optional: Filter by AssignmentId (sensor)
        if (dto.AssignmentId.HasValue)
        {
            query = query.Where(r => r.AssignmentId == dto.AssignmentId.Value);
        }

        // Optional: Filter by MeasurementType
        if (!string.IsNullOrEmpty(dto.MeasurementType))
        {
            var measurementType = dto.MeasurementType.ToLowerInvariant();
            query = query.Where(r => r.MeasurementType == measurementType);
        }

        // Execute bulk delete
        var deletedCount = await query.ExecuteDeleteAsync(ct);

        _logger.LogInformation(
            "Deleted {Count} readings for Node {NodeId} from {From} to {To} (AssignmentId: {AssignmentId}, MeasurementType: {MeasurementType})",
            deletedCount, dto.NodeId, dto.From, dto.To, dto.AssignmentId, dto.MeasurementType);

        return new DeleteReadingsResultDto(
            DeletedCount: deletedCount,
            NodeId: dto.NodeId,
            From: dto.From,
            To: dto.To,
            AssignmentId: dto.AssignmentId,
            MeasurementType: dto.MeasurementType
        );
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

    /// <summary>
    /// Builds a dictionary mapping MeasurementType to SensorCapability for DisplayName lookup
    /// </summary>
    private async Task<Dictionary<string, SensorCapability>> GetCapabilityLookupAsync(CancellationToken ct)
    {
        var capabilities = await _context.SensorCapabilities
            .AsNoTracking()
            .Include(c => c.Sensor)
            .ToListAsync(ct);

        // Group by MeasurementType and take the first one (they should all have the same DisplayName)
        return capabilities
            .GroupBy(c => c.MeasurementType.ToLowerInvariant())
            .ToDictionary(
                g => g.Key,
                g => g.First()
            );
    }
}
