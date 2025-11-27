using System.Linq.Expressions;

namespace myIoTGrid.Hub.Domain.Interfaces;

/// <summary>
/// Generic Repository Pattern Interface
/// </summary>
/// <typeparam name="T">Entity-Typ</typeparam>
public interface IRepository<T> where T : class, IEntity
{
    /// <summary>Findet Entity anhand der ID</summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Gibt alle Entities zurück</summary>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Findet Entities anhand eines Prädikats</summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>Findet ein einzelnes Entity anhand eines Prädikats</summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>Prüft ob ein Entity existiert</summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>Zählt Entities anhand eines Prädikats</summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);

    /// <summary>Fügt ein neues Entity hinzu</summary>
    Task<T> AddAsync(T entity, CancellationToken ct = default);

    /// <summary>Fügt mehrere Entities hinzu</summary>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

    /// <summary>Aktualisiert ein Entity</summary>
    void Update(T entity);

    /// <summary>Aktualisiert mehrere Entities</summary>
    void UpdateRange(IEnumerable<T> entities);

    /// <summary>Entfernt ein Entity</summary>
    void Remove(T entity);

    /// <summary>Entfernt mehrere Entities</summary>
    void RemoveRange(IEnumerable<T> entities);
}

/// <summary>
/// Repository Interface für Tenant-spezifische Entities
/// </summary>
public interface ITenantRepository<T> : IRepository<T> where T : class, ITenantEntity
{
    /// <summary>Gibt alle Entities für einen Tenant zurück</summary>
    Task<IEnumerable<T>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
}
