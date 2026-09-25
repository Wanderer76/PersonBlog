using Microsoft.EntityFrameworkCore;
using Shared;
using System.Linq.Expressions;

namespace Infrastructure.Extensions;

public static class SoftDeleteQueryExtensions
{
    public static Task<int> SoftDeleteAsync<T>(this IQueryable<T> query, DateTimeOffset deleteTime, CancellationToken cancellationToken = default) where T : ISoftDelete
    {
        return query.ExecuteUpdateAsync(x => x
        .SetProperty(e => e.IsDelete, true)
        .SetProperty(e => e.DeleteDateTime, deleteTime),
        cancellationToken
        );
    }

    public static Task<int> SoftDeleteAsync<T>(this IQueryable<T> query, DateTimeOffset deleteTime, Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default) where T : ISoftDelete
    {
        return query.Where(filter).SoftDeleteAsync(deleteTime, cancellationToken);
    }

    public static Task<int> UndoSoftDeleteAsync<T>(this IQueryable<T> query, CancellationToken cancellationToken = default) where T : ISoftDelete
    {
        return query.ExecuteUpdateAsync(x => x.SetProperty(x => x.IsDelete, x => false).SetProperty(x => x.DeleteDateTime, x => null), cancellationToken);
    }

    public static Task<int> UndoSoftDeleteAsync<T>(this IQueryable<T> query, Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default) where T : ISoftDelete
    {
        return query.Where(filter).UndoSoftDeleteAsync(cancellationToken);
    }
}
