using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Shared.Persistence;

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

    public IQueryable<TEntity> FromSqlRaw<TEntity>(string sql, params object[] parameters)
      where TEntity : class, TDbEntity
    {
        return _context.Set<TEntity>().FromSqlRaw(sql, parameters);
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

    public IQueryable<TDbEntity> FromSqlRaw<TDbEntity>(string sql, params object[] parameters)
        where TDbEntity : class, TEntity
    {
        return _readRepository.FromSqlRaw<TDbEntity>(sql, parameters);
    }
}