using System.Linq.Expressions;

namespace myIoTGrid.Shared.Common.Interfaces;

/// <summary>
/// Basis-Interface für alle Entities
/// </summary>
public interface IEntity
{
    /// <summary>Primärschlüssel</summary>
    Guid Id { get; set; }
}

/// <summary>
/// Interface für Entities mit Tenant-Zugehörigkeit
/// </summary>
public interface ITenantEntity : IEntity
{
    /// <summary>Tenant-ID für Multi-Tenant Support</summary>
    Guid TenantId { get; set; }
}

/// <summary>
/// Interface für Entities die mit Cloud synchronisiert werden
/// </summary>
public interface ISyncableEntity : IEntity
{
    /// <summary>Ob dieses Entity global (von Cloud definiert) ist</summary>
    bool IsGlobal { get; set; }

    /// <summary>Erstellungszeitpunkt</summary>
    DateTime CreatedAt { get; set; }
}

/// <summary>
/// Generic Repository Interface
/// </summary>
/// <typeparam name="T">Entity-Typ</typeparam>
public interface IRepository<T> where T : class, IEntity
{
    /// <summary>Entity by ID abrufen</summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Alle Entities abrufen</summary>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Entities nach Bedingung filtern</summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>Erstes Entity nach Bedingung</summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>Prüfen ob Entity existiert</summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>Anzahl der Entities zählen</summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);

    /// <summary>Entity hinzufügen</summary>
    Task<T> AddAsync(T entity, CancellationToken ct = default);

    /// <summary>Mehrere Entities hinzufügen</summary>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

    /// <summary>Entity aktualisieren</summary>
    void Update(T entity);

    /// <summary>Mehrere Entities aktualisieren</summary>
    void UpdateRange(IEnumerable<T> entities);

    /// <summary>Entity entfernen</summary>
    void Remove(T entity);

    /// <summary>Mehrere Entities entfernen</summary>
    void RemoveRange(IEnumerable<T> entities);
}

/// <summary>
/// Repository Interface für Tenant-spezifische Entities
/// </summary>
/// <typeparam name="T">Entity-Typ mit ITenantEntity</typeparam>
public interface ITenantRepository<T> : IRepository<T> where T : class, ITenantEntity
{
    /// <summary>Alle Entities eines Tenants abrufen</summary>
    Task<IEnumerable<T>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
}

/// <summary>
/// Unit of Work Interface
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>Änderungen speichern</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Transaktion starten</summary>
    Task BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>Transaktion committen</summary>
    Task CommitTransactionAsync(CancellationToken ct = default);

    /// <summary>Transaktion zurückrollen</summary>
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
