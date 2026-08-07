using Conference.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shared.Persistence;

namespace Conference.Persistence;

internal sealed class ConferenceRepository(ConferenceDbContext context) : IReadWriteRepository<IConferenceEntity>
{
    public IQueryable<TEntity> Get<TEntity>() where TEntity : class, IConferenceEntity
    {
        return context.Set<TEntity>();
    }

    public IQueryable<TEntity> FromSqlRaw<TEntity>(string sql, params object[] parameters)
        where TEntity : class, IConferenceEntity
    {
        return context.Set<TEntity>().FromSqlRaw(sql, parameters);
    }

    public void Attach(IConferenceEntity entity) => context.Attach(entity);
    public void Add(IConferenceEntity entity) => context.Add(entity);
    public void Remove(IConferenceEntity entity) => context.Remove(entity);
    public int SaveChanges() => context.SaveChanges();
    public Task<int> SaveChangesAsync() => context.SaveChangesAsync();
    public Task<IDbContextTransaction> BeginTransactionAsync() => context.Database.BeginTransactionAsync();
    public Task CommitAsync() => context.Database.CommitTransactionAsync();
}
