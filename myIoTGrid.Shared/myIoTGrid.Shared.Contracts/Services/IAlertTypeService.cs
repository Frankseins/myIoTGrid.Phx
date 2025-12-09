using myIoTGrid.Shared.Common.DTOs;

namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service for managing alert types.
/// </summary>
public interface IAlertTypeService
{
    Task<IEnumerable<AlertTypeDto>> GetAllAsync(CancellationToken ct = default);
    Task<AlertTypeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AlertTypeDto?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<AlertTypeDto> CreateAsync(CreateAlertTypeDto dto, CancellationToken ct = default);
    Task SyncFromCloudAsync(CancellationToken ct = default);
    Task SeedDefaultTypesAsync(CancellationToken ct = default);
}
