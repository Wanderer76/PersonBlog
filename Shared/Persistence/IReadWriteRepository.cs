using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Shared.Persistence;

public interface IReadRepository<TDbEntity>
{
    IQueryable<TEntity> Get<TEntity>() where TEntity : class, TDbEntity;
}

public interface IWriteRepository<TEntity> where TEntity : class
{
    void Attach(TEntity entity);
    void Add(TEntity entity);
    void Remove(TEntity entity);
    int SaveChanges();
    Task<int> SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task CommitAsync();
}

public interface IReadWriteRepository<TEntity> : IReadRepository<TEntity>, IWriteRepository<TEntity>
    where TEntity : class
{
}

public class DefaultRepository<TContext, TEntity> : IReadWriteRepository<TEntity>
    where TEntity : class
    where TContext : BaseDbContext
{
    private readonly TContext _context;

    public DefaultRepository(TContext context)
    {
        _context = context;
    }

    public IQueryable<TDbEntity> Get<TDbEntity>() where TDbEntity : class, TEntity
    {
        return _context.Set<TDbEntity>().AsNoTrackingWithIdentityResolution();
    }

    public void Attach(TEntity entity)
    {
        _context.Attach(entity);
    }

    public void Add(TEntity entity)
    {
        _context.Add(entity);
    }

    public int SaveChanges()
    {
        return _context.SaveChanges();
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    public void Remove(TEntity entity)
    {
        _context.Remove(entity);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return _context.Database.BeginTransactionAsync();
    } 
    public async Task CommitAsync()
    {
        await _context.Database.CommitTransactionAsync();
    }

}

public class DefaultReadRepository<TContext, TDbEntity> : IReadRepository<TDbEntity>
    where TContext : BaseDbContext
{
    private readonly TContext _context;
    public DefaultReadRepository(TContext context)
    {
        _context = context;
    }

    public IQueryable<TEntity> Get<TEntity>() where TEntity : class, TDbEntity
    {
        return _context.Set<TEntity>().AsNoTrackingWithIdentityResolution();
    }
}

public class DefaultWriteRepository<TContext, TEntity> : IWriteRepository<TEntity>
    where TEntity : class
    where TContext : BaseDbContext
{
    private readonly TContext _context;

    public DefaultWriteRepository(TContext context)
    {
        _context = context;
    }

    public void Attach(TEntity entity)
    {
        _context.Attach(entity);
    }

    public void Add(TEntity entity)
    {
        _context.Add(entity);
    }

    public int SaveChanges()
    {
        return _context.SaveChanges();
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    public void Remove(TEntity entity)
    {
        _context.Remove(entity);
    }
    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return _context.Database.BeginTransactionAsync();
    }
    public async Task CommitAsync()
    {
        await _context.Database.CommitTransactionAsync();
    }
}