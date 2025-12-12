using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for Hub (Raspberry Pi Gateway) management.
/// Single-Hub-Architecture: Only one Hub per Tenant/Installation allowed.
/// </summary>
public class HubService : IHubService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantService _tenantService;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HubService> _logger;

    private const string DefaultHubId = "my-iot-hub";
    private const string DefaultHubName = "My IoT Hub";

    public HubService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        ITenantService tenantService,
        ISignalRNotificationService signalRNotificationService,
        IConfiguration configuration,
        ILogger<HubService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _signalRNotificationService = signalRNotificationService;
        _configuration = configuration;
        _logger = logger;
    }

    // === Single-Hub API Implementation ===

    /// <inheritdoc />
    public async Task<HubDto> GetCurrentHubAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var hub = await _context.Hubs
            .AsNoTracking()
            .Include(h => h.Nodes)
            .FirstOrDefaultAsync(h => h.TenantId == tenantId, ct);

        if (hub == null)
        {
            throw new InvalidOperationException("Hub not initialized. Please restart the application.");
        }

        return hub.ToDto();
    }

    /// <inheritdoc />
    public async Task<HubDto> UpdateCurrentHubAsync(UpdateHubDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var hub = await _context.Hubs
            .Include(h => h.Nodes)
            .FirstOrDefaultAsync(h => h.TenantId == tenantId, ct);

        if (hub == null)
        {
            throw new InvalidOperationException("Hub not initialized. Please restart the application.");
        }

        hub.ApplyUpdate(dto);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Hub updated: {HubId} ({Name})", hub.HubId, hub.Name);

        return hub.ToDto();
    }

    /// <inheritdoc />
    public async Task<HubStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var hub = await _context.Hubs
            .AsNoTracking()
            .Include(h => h.Nodes)
            .FirstOrDefaultAsync(h => h.TenantId == tenantId, ct);

        if (hub == null)
        {
            throw new InvalidOperationException("Hub not initialized. Please restart the application.");
        }

        var nodeCount = hub.Nodes.Count;
        var onlineNodeCount = hub.Nodes.Count(n => n.IsOnline);

        // Check individual service status
        var services = new ServiceStatusDto(
            Api: new ServiceState(true, "Running"), // Always true if we reach this point
            Database: await CheckDatabaseStatusAsync(ct),
            Mqtt: CheckMqttStatus(),
            Cloud: CheckCloudStatus()
        );

        // Hub is online if API is reachable (which it is if we're here)
        return new HubStatusDto(true, DateTime.UtcNow, nodeCount, onlineNodeCount, services);
    }

    private async Task<ServiceState> CheckDatabaseStatusAsync(CancellationToken ct)
    {
        try
        {
            await _context.Database.CanConnectAsync(ct);
            return new ServiceState(true, "Connected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection check failed");
            return new ServiceState(false, "Connection failed");
        }
    }

    private ServiceState CheckMqttStatus()
    {
        // TODO: Implement actual MQTT status check when MQTT is enabled
        return new ServiceState(false, "Not configured");
    }

    private ServiceState CheckCloudStatus()
    {
        // TODO: Implement actual Cloud status check when Cloud sync is enabled
        return new ServiceState(false, "Not configured");
    }

    /// <inheritdoc />
    public async Task EnsureDefaultHubAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var existingHub = await _context.Hubs
            .FirstOrDefaultAsync(h => h.TenantId == tenantId, ct);

        if (existingHub != null)
        {
            // Single-Hub-Architecture: Mark existing hub as online on startup
            existingHub.IsOnline = true;
            existingHub.LastSeen = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogDebug("Hub already exists and marked as online: {HubId}", existingHub.HubId);
            return;
        }

        // Create the default Hub
        var defaultHub = new myIoTGrid.Shared.Common.Entities.Hub
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HubId = DefaultHubId,
            Name = DefaultHubName,
            Description = "Auto-initialized Hub for this installation",
            IsOnline = true,
            LastSeen = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Hubs.Add(defaultHub);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Default Hub created: {HubId} ({Name})", defaultHub.HubId, defaultHub.Name);
    }

    /// <inheritdoc />
    public async Task<HubProvisioningSettingsDto> GetProvisioningSettingsAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var hub = await _context.Hubs
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.TenantId == tenantId, ct);

        if (hub == null)
        {
            throw new InvalidOperationException("Hub not initialized. Please restart the application.");
        }

        return hub.ToProvisioningSettingsDto();
    }

    /// <inheritdoc />
    public Task<HubPropertiesDto> GetPropertiesAsync(CancellationToken ct = default)
    {
        // Read configuration values
        var tenantIdStr = _configuration["Hub:DefaultTenantId"] ?? "00000000-0000-0000-0000-000000000001";
        var tenantId = Guid.TryParse(tenantIdStr, out var id) ? id : Guid.Parse("00000000-0000-0000-0000-000000000001");
        var tenantName = _configuration["Hub:DefaultTenantName"] ?? "Default";

        // Get address from Discovery config or Hub config
        var protocol = _configuration["Discovery:Protocol"] ?? "https";
        var address = _configuration["Hub:Address"] ?? $"{protocol}://localhost";
        var port = _configuration.GetValue<int>("Discovery:ApiPort", 5001);

        // Get version from assembly
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";

        var properties = new HubPropertiesDto(
            Address: address,
            Port: port,
            TenantId: tenantId,
            TenantName: tenantName,
            Version: version
        );

        _logger.LogDebug("Hub properties retrieved: Address={Address}, Port={Port}, TenantId={TenantId}",
            properties.Address, properties.Port, properties.TenantId);

        return Task.FromResult(properties);
    }

    // === Legacy API Implementation ===

    /// <inheritdoc />
    public async Task<HubDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var hub = await _context.Hubs
            .AsNoTracking()
            .Include(h => h.Nodes)
            .FirstOrDefaultAsync(h => h.Id == id && h.TenantId == tenantId, ct);

        return hub?.ToDto();
    }

    /// <inheritdoc />
    public async Task<HubDto?> GetByHubIdAsync(string hubId, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var hub = await _context.Hubs
            .AsNoTracking()
            .Include(h => h.Nodes)
            .FirstOrDefaultAsync(h => h.HubId == hubId && h.TenantId == tenantId, ct);

        return hub?.ToDto();
    }

    /// <inheritdoc />
    public async Task<HubDto> GetOrCreateByHubIdAsync(string hubId, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Single-Hub-Architecture: Always return the existing hub, update its LastSeen
        var existingHub = await _context.Hubs
            .Include(h => h.Nodes)
            .FirstOrDefaultAsync(h => h.TenantId == tenantId, ct);

        if (existingHub != null)
        {
            // Update LastSeen and Online status
            existingHub.LastSeen = DateTime.UtcNow;
            existingHub.IsOnline = true;
            await _unitOfWork.SaveChangesAsync(ct);

            return existingHub.ToDto();
        }

        // Hub should have been created by EnsureDefaultHubAsync during startup
        throw new InvalidOperationException("Hub not initialized. Please restart the application.");
    }

    /// <inheritdoc />
    public async Task<HubDto> GetDefaultHubAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var hub = await _context.Hubs
            .Include(h => h.Nodes)
            .FirstOrDefaultAsync(h => h.TenantId == tenantId, ct);

        if (hub == null)
        {
            // Ensure default hub exists
            await EnsureDefaultHubAsync(ct);
            hub = await _context.Hubs
                .Include(h => h.Nodes)
                .FirstOrDefaultAsync(h => h.TenantId == tenantId, ct);
        }

        return hub!.ToDto();
    }

    /// <inheritdoc />
    public async Task<HubDto?> UpdateAsync(Guid id, UpdateHubDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var hub = await _context.Hubs
            .Include(h => h.Nodes)
            .FirstOrDefaultAsync(h => h.Id == id && h.TenantId == tenantId, ct);

        if (hub == null)
        {
            _logger.LogWarning("Hub not found: {HubId}", id);
            return null;
        }

        hub.ApplyUpdate(dto);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Hub updated: {HubId}", id);

        return hub.ToDto();
    }

    /// <inheritdoc />
    public async Task UpdateLastSeenAsync(Guid id, CancellationToken ct = default)
    {
        var hub = await _context.Hubs
            .FirstOrDefaultAsync(h => h.Id == id, ct);

        if (hub == null) return;

        hub.LastSeen = DateTime.UtcNow;
        hub.IsOnline = true;
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task SetOnlineStatusAsync(Guid id, bool isOnline, CancellationToken ct = default)
    {
        var hub = await _context.Hubs
            .Include(h => h.Nodes)
            .FirstOrDefaultAsync(h => h.Id == id, ct);

        if (hub == null) return;

        var wasOnline = hub.IsOnline;
        hub.IsOnline = isOnline;

        if (!isOnline)
        {
            _logger.LogWarning("Hub offline: {HubId} ({Name})", hub.HubId, hub.Name);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // SignalR Broadcast only if status changed
        if (wasOnline != isOnline)
        {
            await _signalRNotificationService.NotifyHubStatusChangedAsync(hub.TenantId, hub.ToDto(), ct);
        }
    }
}
