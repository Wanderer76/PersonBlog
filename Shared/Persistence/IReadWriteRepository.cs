using Microsoft.EntityFrameworkCore;

namespace Shared.Persistence;

public interface IReadRepository
{
    IQueryable<TEntity> Get<TEntity>() where TEntity : class;
}

public interface IWriteRepository<TEntity> where TEntity : class
{
    void Attach(TEntity entity);
    void Add(TEntity entity);
    void Remove(TEntity entity);
    int SaveChanges();
    Task<int> SaveChangesAsync();
}

public interface IReadWriteRepository<TEntity> : IReadRepository, IWriteRepository<TEntity>
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

    public IQueryable<TDbEntity> Get<TDbEntity>() where TDbEntity : class
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
}