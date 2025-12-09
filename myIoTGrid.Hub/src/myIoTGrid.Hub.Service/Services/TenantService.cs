using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service für Tenant-Verwaltung (Scoped)
/// </summary>
public class TenantService : ITenantService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TenantService> _logger;
    private readonly IConfiguration _configuration;
    private Guid _currentTenantId;

    public TenantService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<TenantService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _configuration = configuration;

        // Default Tenant aus Konfiguration laden
        var defaultTenantIdStr = _configuration["Hub:DefaultTenantId"];
        _currentTenantId = Guid.TryParse(defaultTenantIdStr, out var id)
            ? id
            : Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    /// <inheritdoc />
    public Guid GetCurrentTenantId() => _currentTenantId;

    /// <inheritdoc />
    public void SetCurrentTenantId(Guid tenantId)
    {
        _currentTenantId = tenantId;
        _logger.LogDebug("Tenant-ID gesetzt auf: {TenantId}", tenantId);
    }

    /// <inheritdoc />
    public async Task EnsureDefaultTenantAsync(CancellationToken ct = default)
    {
        var defaultTenantIdStr = _configuration["Hub:DefaultTenantId"] ?? "00000000-0000-0000-0000-000000000001";
        var defaultTenantId = Guid.Parse(defaultTenantIdStr);
        var defaultTenantName = _configuration["Hub:DefaultTenantName"] ?? "Default";

        var exists = await _context.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == defaultTenantId, ct);

        if (!exists)
        {
            var tenant = new Tenant
            {
                Id = defaultTenantId,
                Name = defaultTenantName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Tenants.Add(tenant);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Default-Tenant erstellt: {TenantId} ({TenantName})", defaultTenantId, defaultTenantName);
        }
    }

    /// <inheritdoc />
    public async Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        return tenant?.ToDto();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TenantDto>> GetAllAsync(CancellationToken ct = default)
    {
        var tenants = await _context.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        return tenants.Select(t => t.ToDto());
    }

    /// <inheritdoc />
    public async Task<TenantDto> CreateAsync(CreateTenantDto dto, CancellationToken ct = default)
    {
        var tenant = dto.ToEntity();

        _context.Tenants.Add(tenant);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Tenant erstellt: {TenantId} ({TenantName})", tenant.Id, tenant.Name);

        return tenant.ToDto();
    }

    /// <inheritdoc />
    public async Task<TenantDto?> UpdateAsync(Guid id, UpdateTenantDto dto, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant nicht gefunden: {TenantId}", id);
            return null;
        }

        tenant.ApplyUpdate(dto);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Tenant aktualisiert: {TenantId}", id);

        return tenant.ToDto();
    }
}
