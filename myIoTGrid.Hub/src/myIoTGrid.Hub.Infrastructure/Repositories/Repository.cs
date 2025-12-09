using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using myIoTGrid.Hub.Infrastructure.Data;

namespace myIoTGrid.Hub.Infrastructure.Repositories;

/// <summary>
/// Generic Repository Implementation
/// </summary>
/// <typeparam name="T">Entity-Typ</typeparam>
public class Repository<T> : IRepository<T> where T : class, IEntity
{
    protected readonly HubDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(HubDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await DbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await DbSet.AsNoTracking().ToListAsync(ct);
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await DbSet.AsNoTracking().Where(predicate).ToListAsync(ct);
    }

    /// <inheritdoc />
    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await DbSet.AsNoTracking().FirstOrDefaultAsync(predicate, ct);
    }

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await DbSet.AsNoTracking().AnyAsync(predicate, ct);
    }

    /// <inheritdoc />
    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
    {
        return predicate == null
            ? await DbSet.AsNoTracking().CountAsync(ct)
            : await DbSet.AsNoTracking().CountAsync(predicate, ct);
    }

    /// <inheritdoc />
    public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        var entry = await DbSet.AddAsync(entity, ct);
        return entry.Entity;
    }

    /// <inheritdoc />
    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        await DbSet.AddRangeAsync(entities, ct);
    }

    /// <inheritdoc />
    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    /// <inheritdoc />
    public virtual void UpdateRange(IEnumerable<T> entities)
    {
        DbSet.UpdateRange(entities);
    }

    /// <inheritdoc />
    public virtual void Remove(T entity)
    {
        DbSet.Remove(entity);
    }

    /// <inheritdoc />
    public virtual void RemoveRange(IEnumerable<T> entities)
    {
        DbSet.RemoveRange(entities);
    }
}

/// <summary>
/// Repository Implementation für Tenant-spezifische Entities
/// </summary>
/// <typeparam name="T">Entity-Typ mit ITenantEntity</typeparam>
public class TenantRepository<T> : Repository<T>, ITenantRepository<T> where T : class, ITenantEntity
{
    public TenantRepository(HubDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await DbSet.AsNoTracking().Where(e => e.TenantId == tenantId).ToListAsync(ct);
    }
}
