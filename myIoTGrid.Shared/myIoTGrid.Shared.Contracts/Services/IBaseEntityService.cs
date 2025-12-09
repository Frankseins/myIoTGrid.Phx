namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Base interface for entity services with CRUD operations.
/// Hub and Cloud can extend this with their specific DTOs.
/// </summary>
/// <typeparam name="TDto">The DTO type for the entity</typeparam>
/// <typeparam name="TCreateDto">The DTO type for creating the entity</typeparam>
/// <typeparam name="TUpdateDto">The DTO type for updating the entity</typeparam>
public interface IBaseEntityService<TDto, TCreateDto, TUpdateDto>
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    /// <summary>Returns all entities</summary>
    Task<IEnumerable<TDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns an entity by ID</summary>
    Task<TDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates a new entity</summary>
    Task<TDto> CreateAsync(TCreateDto dto, CancellationToken ct = default);

    /// <summary>Updates an entity</summary>
    Task<TDto?> UpdateAsync(Guid id, TUpdateDto dto, CancellationToken ct = default);

    /// <summary>Deletes an entity</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Base interface for read-only entity services.
/// </summary>
/// <typeparam name="TDto">The DTO type for the entity</typeparam>
public interface IBaseReadOnlyService<TDto> where TDto : class
{
    /// <summary>Returns all entities</summary>
    Task<IEnumerable<TDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns an entity by ID</summary>
    Task<TDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
