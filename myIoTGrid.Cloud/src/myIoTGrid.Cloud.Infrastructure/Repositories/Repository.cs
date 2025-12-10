using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using myIoTGrid.Cloud.Infrastructure.Data;
using myIoTGrid.Shared.Common.Interfaces;

namespace myIoTGrid.Cloud.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation for Cloud API (PostgreSQL)
/// </summary>
public class Repository<T> : IRepository<T> where T : class, IEntity
{
    protected readonly CloudDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(CloudDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(ct);
    }

    public virtual async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking().Where(predicate).ToListAsync(ct);
    }

    public virtual async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate, ct);
    }

    public virtual async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking().AnyAsync(predicate, ct);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        return predicate == null
            ? await _dbSet.AsNoTracking().CountAsync(ct)
            : await _dbSet.AsNoTracking().CountAsync(predicate, ct);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        var entry = await _dbSet.AddAsync(entity, ct);
        return entry.Entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        await _dbSet.AddRangeAsync(entities, ct);
    }

    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public virtual void UpdateRange(IEnumerable<T> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    public virtual void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual void RemoveRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }
}

/// <summary>
/// Repository implementation for tenant-specific entities
/// </summary>
public class TenantRepository<T> : Repository<T>, ITenantRepository<T> where T : class, ITenantEntity
{
    public TenantRepository(CloudDbContext context) : base(context)
    {
    }

    public virtual async Task<IEnumerable<T>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking().Where(e => e.TenantId == tenantId).ToListAsync(ct);
    }
}
