using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Domain.Interfaces;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for Hub (Raspberry Pi Gateway) management
/// </summary>
public class HubService : IHubService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantService _tenantService;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly ILogger<HubService> _logger;

    public HubService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        ITenantService tenantService,
        ISignalRNotificationService signalRNotificationService,
        ILogger<HubService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _signalRNotificationService = signalRNotificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<HubDto>> GetAllAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var hubs = await _context.Hubs
            .AsNoTracking()
            .Where(h => h.TenantId == tenantId)
            .Include(h => h.Nodes)
            .OrderBy(h => h.Name)
            .ToListAsync(ct);

        return hubs.ToDtos();
    }

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

        var existingHub = await _context.Hubs
            .Include(h => h.Nodes)
            .FirstOrDefaultAsync(h => h.HubId == hubId && h.TenantId == tenantId, ct);

        if (existingHub != null)
        {
            // Update LastSeen and Online status
            existingHub.LastSeen = DateTime.UtcNow;
            existingHub.IsOnline = true;
            await _unitOfWork.SaveChangesAsync(ct);

            return existingHub.ToDto();
        }

        // Create new Hub (Auto-Registration)
        var newHub = new Domain.Entities.Hub
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HubId = hubId,
            Name = GenerateNameFromHubId(hubId),
            IsOnline = true,
            LastSeen = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Hubs.Add(newHub);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Hub auto-registered: {HubId} ({Name})", newHub.HubId, newHub.Name);

        return newHub.ToDto(0);
    }

    /// <inheritdoc />
    public async Task<HubDto> CreateAsync(CreateHubDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Check if HubId already exists
        var exists = await _context.Hubs
            .AsNoTracking()
            .AnyAsync(h => h.HubId == dto.HubId && h.TenantId == tenantId, ct);

        if (exists)
        {
            throw new InvalidOperationException($"Hub with HubId '{dto.HubId}' already exists.");
        }

        var hub = dto.ToEntity(tenantId);

        _context.Hubs.Add(hub);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Hub created: {HubId} ({Name})", hub.HubId, hub.Name);

        return hub.ToDto(0);
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

    /// <inheritdoc />
    public async Task<HubDto?> GetDefaultHubAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var hub = await _context.Hubs
            .AsNoTracking()
            .Include(h => h.Nodes)
            .Where(h => h.TenantId == tenantId)
            .OrderBy(h => h.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return hub?.ToDto();
    }

    /// <summary>
    /// Generates a name from the HubId
    /// </summary>
    private static string GenerateNameFromHubId(string hubId)
    {
        if (string.IsNullOrWhiteSpace(hubId)) return "Unknown Hub";

        // "hub-home-01" -> "Hub Home 01"
        var parts = hubId.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        var formattedParts = parts.Select(s =>
            s.Length > 0 ? char.ToUpper(s[0]) + s[1..].ToLower() : s);

        return string.Join(" ", formattedParts);
    }
}
