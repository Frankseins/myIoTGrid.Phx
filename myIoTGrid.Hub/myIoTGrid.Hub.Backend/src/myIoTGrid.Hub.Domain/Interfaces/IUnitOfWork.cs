namespace myIoTGrid.Hub.Domain.Interfaces;

/// <summary>
/// Unit of Work Pattern Interface
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>Speichert alle Änderungen in der Datenbank</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Startet eine Transaktion</summary>
    Task BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>Bestätigt die aktuelle Transaktion</summary>
    Task CommitTransactionAsync(CancellationToken ct = default);

    /// <summary>Macht die aktuelle Transaktion rückgängig</summary>
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
