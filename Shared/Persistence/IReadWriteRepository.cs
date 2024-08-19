using Microsoft.EntityFrameworkCore;

namespace Shared.Persistence;

public interface IReadRepository<TContext> where TContext : BaseDbContext
{
    IQueryable<TEntity> Get<TEntity>() where TEntity : class;
}

public interface IWriteRepository<TContext> where TContext : BaseDbContext
{
    void Attach<TEntity>(TEntity entity) where TEntity : class;
    void Add<TEntity>(TEntity entity) where TEntity : class;
    int SaveChanges();
    Task<int> SaveChangesAsync();
}

public interface IReadWriteRepository<TContext> : IReadRepository<TContext>, IWriteRepository<TContext>
    where TContext : BaseDbContext
{
}

public class DefaultRepository<TContext> : IReadWriteRepository<TContext> where TContext : BaseDbContext
{
    private readonly TContext _context; 

    public DefaultRepository(TContext context)
    {
        _context = context;
    }

    public IQueryable<TEntity> Get<TEntity>() where TEntity : class
    {
        return _context.Set<TEntity>().AsNoTrackingWithIdentityResolution();
    }

    public void Attach<TEntity>(TEntity entity) where TEntity : class
    {
        _context.Set<TEntity>().Attach(entity);
    }

    public void Add<TEntity>(TEntity entity) where TEntity : class
    {
        _context.Set<TEntity>().Add(entity);
    }

    public int SaveChanges()
    {
        return _context.SaveChanges();
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}