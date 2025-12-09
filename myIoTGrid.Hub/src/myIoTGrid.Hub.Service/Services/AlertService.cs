using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Infrastructure.Matter;
using myIoTGrid.Hub.Service.Extensions;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for Alert management
/// </summary>
public class AlertService : IAlertService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantService _tenantService;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly IMatterBridgeClient _matterBridgeClient;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        ITenantService tenantService,
        ISignalRNotificationService signalRNotificationService,
        IMatterBridgeClient matterBridgeClient,
        ILogger<AlertService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _signalRNotificationService = signalRNotificationService;
        _matterBridgeClient = matterBridgeClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AlertDto> CreateFromCloudAsync(CreateAlertDto dto, CancellationToken ct = default)
    {
        return await CreateAlertInternalAsync(dto, AlertSource.Cloud, ct);
    }

    /// <inheritdoc />
    public async Task<AlertDto> CreateLocalAlertAsync(CreateAlertDto dto, CancellationToken ct = default)
    {
        return await CreateAlertInternalAsync(dto, AlertSource.Local, ct);
    }

    /// <inheritdoc />
    public async Task<AlertDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var alert = await _context.Alerts
            .AsNoTracking()
            .Include(a => a.AlertType)
            .Include(a => a.Hub)
            .Include(a => a.Node)
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, ct);

        return alert?.ToDto();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AlertDto>> GetActiveAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        
        var alerts = await _context.Alerts
            .AsNoTracking()
            .Include(a => a.AlertType)
            .Include(a => a.Hub)
            .Include(a => a.Node)
            .Where(a => a.TenantId == tenantId && a.IsActive)
            .OrderByDescending(a => a.Level)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        return alerts.ToDtos();
    }

    /// <inheritdoc />
    public async Task<PaginatedResultDto<AlertDto>> GetFilteredAsync(AlertFilterDto filter, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Alerts
            .AsNoTracking()
            .Include(a => a.AlertType)
            .Include(a => a.Hub)
            .Include(a => a.Node)
            .Where(a => a.TenantId == tenantId);

        // Apply filters
        if (filter.HubId.HasValue)
            query = query.Where(a => a.HubId == filter.HubId.Value);

        if (filter.NodeId.HasValue)
            query = query.Where(a => a.NodeId == filter.NodeId.Value);

        if (!string.IsNullOrWhiteSpace(filter.AlertTypeCode))
            query = query.Where(a => a.AlertType!.Code == filter.AlertTypeCode.ToLowerInvariant());

        if (filter.Level.HasValue)
            query = query.Where(a => a.Level == filter.Level.Value.ToEntity());

        if (filter.Source.HasValue)
            query = query.Where(a => a.Source == filter.Source.Value.ToEntity());

        if (filter.IsActive.HasValue)
            query = query.Where(a => a.IsActive == filter.IsActive.Value);

        if (filter.IsAcknowledged.HasValue)
        {
            if (filter.IsAcknowledged.Value)
                query = query.Where(a => a.AcknowledgedAt != null);
            else
                query = query.Where(a => a.AcknowledgedAt == null);
        }

        if (filter.From.HasValue)
            query = query.Where(a => a.CreatedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(a => a.CreatedAt <= filter.To.Value);

        // Sorting (Critical first, then newest)
        query = query
            .OrderByDescending(a => a.Level)
            .ThenByDescending(a => a.CreatedAt);

        // Pagination
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return new PaginatedResultDto<AlertDto>(
            items.Select(a => a.ToDto()).ToList(),
            totalCount,
            filter.Page,
            filter.PageSize
        );
    }

    /// <inheritdoc />
    public async Task<AlertDto?> AcknowledgeAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var alert = await _context.Alerts
            .Include(a => a.AlertType)
            .Include(a => a.Hub)
            .Include(a => a.Node)
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, ct);

        if (alert == null)
        {
            _logger.LogWarning("Alert not found: {AlertId}", id);
            return null;
        }

        alert.AcknowledgedAt = DateTime.UtcNow;
        alert.IsActive = false;
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Alert acknowledged: {AlertId} ({AlertType})", id, alert.AlertType?.Code);

        var alertDto = alert.ToDto();

        // SignalR Broadcast to all clients in Tenant
        await _signalRNotificationService.NotifyAlertAcknowledgedAsync(tenantId, alertDto, ct);

        // Update Matter Bridge Contact Sensor to CLOSED (fire and forget)
        if (alert.AlertType != null)
        {
            _ = UpdateMatterContactSensorAsync(alert.AlertType.Code, alert.Node?.NodeId, false, ct);
        }

        return alertDto;
    }

    /// <inheritdoc />
    public async Task CreateNodeOfflineAlertAsync(Guid nodeId, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Get Node information
        var node = await _context.Nodes
            .AsNoTracking()
            .Include(n => n.Hub)
            .FirstOrDefaultAsync(n => n.Id == nodeId, ct);

        if (node == null) return;

        // Check if an active node_offline alert already exists for this node
        var existingAlert = await _context.Alerts
            .AsNoTracking()
            .Include(a => a.AlertType)
            .AnyAsync(a =>
                a.NodeId == nodeId &&
                a.TenantId == tenantId &&
                a.AlertType!.Code == "node_offline" &&
                a.IsActive, ct);

        if (existingAlert) return;

        var dto = new CreateAlertDto(
            AlertTypeCode: "node_offline",
            NodeId: node.NodeId,
            HubId: node.Hub?.HubId,
            Level: AlertLevelDto.Critical,
            Message: $"Node device '{node.Name}' is offline.",
            Recommendation: "Please check the power supply and network connection of the device."
        );

        await CreateLocalAlertAsync(dto, ct);
    }

    /// <inheritdoc />
    public async Task CreateHubOfflineAlertAsync(Guid hubId, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Get Hub information
        var hub = await _context.Hubs
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == hubId, ct);

        if (hub == null) return;

        // Check if an active hub_offline alert already exists for this hub
        var existingAlert = await _context.Alerts
            .AsNoTracking()
            .Include(a => a.AlertType)
            .AnyAsync(a =>
                a.HubId == hubId &&
                a.TenantId == tenantId &&
                a.AlertType!.Code == "hub_offline" &&
                a.IsActive, ct);

        if (existingAlert) return;

        var dto = new CreateAlertDto(
            AlertTypeCode: "hub_offline",
            HubId: hub.HubId,
            NodeId: null,
            Level: AlertLevelDto.Critical,
            Message: $"Hub '{hub.Name}' is offline.",
            Recommendation: "Please check the power supply and network connection of the hub."
        );

        await CreateLocalAlertAsync(dto, ct);
    }

    /// <inheritdoc />
    public async Task DeactivateNodeAlertsAsync(Guid nodeId, string alertTypeCode, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        await _context.Alerts
            .Where(a =>
                a.TenantId == tenantId &&
                a.NodeId == nodeId &&
                a.AlertType!.Code == alertTypeCode.ToLowerInvariant() &&
                a.IsActive)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.IsActive, false), ct);
    }

    /// <inheritdoc />
    public async Task DeactivateHubAlertsAsync(Guid hubId, string alertTypeCode, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        await _context.Alerts
            .Where(a =>
                a.TenantId == tenantId &&
                a.HubId == hubId &&
                a.AlertType!.Code == alertTypeCode.ToLowerInvariant() &&
                a.IsActive)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.IsActive, false), ct);
    }

    /// <summary>
    /// Internal method to create an Alert
    /// </summary>
    private async Task<AlertDto> CreateAlertInternalAsync(CreateAlertDto dto, AlertSource source, CancellationToken ct)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Validate AlertType
        var alertType = await _context.AlertTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(at => at.Code == dto.AlertTypeCode.ToLowerInvariant(), ct);

        if (alertType == null)
        {
            throw new InvalidOperationException($"AlertType '{dto.AlertTypeCode}' not found.");
        }

        // Determine Hub (if specified)
        Guid? hubId = null;
        string? hubName = null;
        if (!string.IsNullOrWhiteSpace(dto.HubId))
        {
            var hub = await _context.Hubs
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.HubId == dto.HubId && h.TenantId == tenantId, ct);

            hubId = hub?.Id;
            hubName = hub?.Name;
        }

        // Determine Node (if specified)
        Guid? nodeId = null;
        string? nodeName = null;
        if (!string.IsNullOrWhiteSpace(dto.NodeId))
        {
            var node = await _context.Nodes
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.NodeId == dto.NodeId, ct);

            nodeId = node?.Id;
            nodeName = node?.Name;
        }

        // Create Alert
        var alert = dto.ToEntity(tenantId, alertType.Id, hubId, nodeId, source);

        _context.Alerts.Add(alert);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Alert created: {AlertType} - {Level} - {Message} (Source: {Source})",
            alertType.Code,
            alert.Level,
            alert.Message,
            source);

        var alertDto = alert.ToDto(alertType, hubName, nodeName);

        // SignalR Broadcast to all clients in Tenant
        await _signalRNotificationService.NotifyAlertReceivedAsync(tenantId, alertDto, ct);

        // Update Matter Bridge Contact Sensor (fire and forget)
        _ = UpdateMatterContactSensorAsync(alertType.Code, dto.NodeId, true, ct);

        return alertDto;
    }

    /// <summary>
    /// Updates the Matter Bridge Contact Sensor state.
    /// Contact Sensor is OPEN when alert is active, CLOSED when resolved.
    /// </summary>
    private async Task UpdateMatterContactSensorAsync(string alertTypeCode, string? nodeId, bool isOpen, CancellationToken ct)
    {
        try
        {
            var deviceId = MatterDeviceMapping.GenerateAlertDeviceId(alertTypeCode, nodeId);

            // First ensure the device is registered
            var displayName = MatterDeviceMapping.CreateAlertDisplayName(alertTypeCode, null);
            await _matterBridgeClient.RegisterDeviceAsync(deviceId, displayName, "contact", null, ct);

            // Then set the contact state
            var success = await _matterBridgeClient.SetContactSensorStateAsync(deviceId, isOpen, ct);

            if (success)
            {
                _logger.LogDebug("Updated Matter contact sensor {DeviceId} to {State}",
                    deviceId, isOpen ? "OPEN" : "CLOSED");
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - Matter Bridge updates should not block alert flow
            _logger.LogWarning(ex, "Failed to update Matter contact sensor for alert {AlertType}", alertTypeCode);
        }
    }
}
