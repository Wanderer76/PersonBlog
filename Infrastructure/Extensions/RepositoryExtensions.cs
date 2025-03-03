using Microsoft.Extensions.DependencyInjection;
using Shared.Persistence;

namespace Infrastructure.Extensions
{
    public static class RepositoryExtensions
    {
        public static void AddRepository<TDbContext, TEntity>(this IServiceCollection services)
            where TEntity : class
            where TDbContext : BaseDbContext
        {
            services.AddScoped<IReadWriteRepository<TEntity>, DefaultRepository<TDbContext, TEntity>>();
            services.AddScoped<IReadRepository<TEntity>, DefaultReadRepository<TDbContext, TEntity>>();
            services.AddScoped<IWriteRepository<TEntity>, DefaultWriteRepository<TDbContext, TEntity>>();
        }
    }
}
