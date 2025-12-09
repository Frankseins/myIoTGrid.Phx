using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service für AlertType-Verwaltung
/// </summary>
public class AlertTypeService : IAlertTypeService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AlertTypeService> _logger;

    public AlertTypeService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<AlertTypeService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AlertTypeDto>> GetAllAsync(CancellationToken ct = default)
    {
        var alertTypes = await _context.AlertTypes
            .AsNoTracking()
            .OrderBy(at => at.Name)
            .ToListAsync(ct);

        return alertTypes.ToDtos();
    }

    /// <inheritdoc />
    public async Task<AlertTypeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var alertType = await _context.AlertTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(at => at.Id == id, ct);

        return alertType?.ToDto();
    }

    /// <inheritdoc />
    public async Task<AlertTypeDto?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalizedCode = code.ToLowerInvariant();

        var alertType = await _context.AlertTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(at => at.Code == normalizedCode, ct);

        return alertType?.ToDto();
    }

    /// <inheritdoc />
    public async Task<AlertTypeDto> CreateAsync(CreateAlertTypeDto dto, CancellationToken ct = default)
    {
        var normalizedCode = dto.Code.ToLowerInvariant();

        // Prüfen ob Code bereits existiert
        var exists = await _context.AlertTypes
            .AsNoTracking()
            .AnyAsync(at => at.Code == normalizedCode, ct);

        if (exists)
        {
            throw new InvalidOperationException($"AlertType mit Code '{normalizedCode}' existiert bereits.");
        }

        var alertType = dto.ToEntity();

        _context.AlertTypes.Add(alertType);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("AlertType erstellt: {Code} ({Name})", alertType.Code, alertType.Name);

        return alertType.ToDto();
    }

    /// <inheritdoc />
    public async Task SyncFromCloudAsync(CancellationToken ct = default)
    {
        // Placeholder für Cloud-Synchronisation
        _logger.LogInformation("Cloud-Sync für AlertTypes ist noch nicht implementiert");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SeedDefaultTypesAsync(CancellationToken ct = default)
    {
        var existingCodes = await _context.AlertTypes
            .AsNoTracking()
            .Select(at => at.Code)
            .ToListAsync(ct);

        var defaultTypes = DefaultAlertTypes.GetAll();
        var newTypes = new List<AlertType>();

        foreach (var dto in defaultTypes)
        {
            if (!existingCodes.Contains(dto.Code))
            {
                var alertType = new AlertType
                {
                    Id = Guid.NewGuid(),
                    Code = dto.Code,
                    Name = dto.Name,
                    Description = dto.Description,
                    DefaultLevel = dto.DefaultLevel.ToEntity(),
                    IconName = dto.IconName,
                    IsGlobal = true,
                    CreatedAt = DateTime.UtcNow
                };
                newTypes.Add(alertType);
            }
        }

        if (newTypes.Count > 0)
        {
            await _context.AlertTypes.AddRangeAsync(newTypes, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("{Count} Default-AlertTypes erstellt", newTypes.Count);
        }
    }
}
