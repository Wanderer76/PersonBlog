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
    private readonly IReadRepository<TEntity> _readRepository;
    private readonly IWriteRepository<TEntity> _writeRepository;

    public DefaultRepository(IReadRepository<TEntity> readRepository, IWriteRepository<TEntity> writeRepository)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
    }

    public IQueryable<TDbEntity> Get<TDbEntity>() where TDbEntity : class, TEntity
    {
        return _readRepository.Get<TDbEntity>();
    }

    public void Attach(TEntity entity)
    {
        _writeRepository.Attach(entity);
    }

    public void Add(TEntity entity)
    {
        _writeRepository.Add(entity);
    }

    public int SaveChanges()
    {
        return _writeRepository.SaveChanges();
    }

    public Task<int> SaveChangesAsync()
    {
        return _writeRepository.SaveChangesAsync();
    }

    public void Remove(TEntity entity)
    {
        _writeRepository.Remove(entity);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return _writeRepository.BeginTransactionAsync();
    }
    public async Task CommitAsync()
    {
        await _writeRepository.CommitAsync();
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