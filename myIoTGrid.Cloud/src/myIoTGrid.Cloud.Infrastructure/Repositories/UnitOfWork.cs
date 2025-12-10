using Microsoft.EntityFrameworkCore.Storage;
using myIoTGrid.Cloud.Infrastructure.Data;
using myIoTGrid.Shared.Common.Entities;
using myIoTGrid.Shared.Common.Interfaces;

namespace myIoTGrid.Cloud.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation for Cloud API (PostgreSQL)
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly CloudDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(CloudDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
