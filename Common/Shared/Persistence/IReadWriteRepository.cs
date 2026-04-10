using Microsoft.EntityFrameworkCore.Storage;

namespace Shared.Persistence;

public interface IReadRepository<TDbEntity>
{
    IQueryable<TEntity> Get<TEntity>() where TEntity : class, TDbEntity;
    IQueryable<TEntity> FromSqlRaw<TEntity>(string sql, params object[] parameters) where TEntity : class, TDbEntity;
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