using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Extensions
{
    public static class SoftDeleteQueryExtensions
    {
        public static Task<int> SoftDelete<T>(this IQueryable<T> query) where T : ISoftDelete
        {
            return query.ExecuteUpdateAsync(x => x.SetProperty(x => x.IsDelete, x => true));
        }
        public static Task<int> SoftDelete<T>(this IQueryable<T> query, Expression<Func<T, bool>> filter) where T : ISoftDelete
        {
            return query.Where(filter).ExecuteUpdateAsync(x => x.SetProperty(x => x.IsDelete, x => true));
        }

        public static Task<int> UndoSoftDelete<T>(this IQueryable<T> query) where T : ISoftDelete
        {
            return query.ExecuteUpdateAsync(x => x.SetProperty(x => x.IsDelete, x => false));
        }

        public static Task<int> UndoSoftDelete<T>(this IQueryable<T> query, Expression<Func<T, bool>> filter) where T : ISoftDelete
        {
            return query.Where(filter).ExecuteUpdateAsync(x => x.SetProperty(x => x.IsDelete, x => false));
        }
    }
}
