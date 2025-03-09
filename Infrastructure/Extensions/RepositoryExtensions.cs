using Microsoft.Extensions.DependencyInjection;
using Shared.Persistence;

namespace Infrastructure.Extensions
{
    public static class RepositoryExtensions
    {
        public static void AddDefaultRepository<TDbContext, TEntity>(this IServiceCollection services)
            where TEntity : class
            where TDbContext : BaseDbContext
        {
            services.AddScoped<IReadRepository<TEntity>, DefaultReadRepository<TDbContext, TEntity>>();
            services.AddScoped<IWriteRepository<TEntity>, DefaultWriteRepository<TDbContext, TEntity>>();
            services.AddScoped<IReadWriteRepository<TEntity>, DefaultRepository<TDbContext, TEntity>>();
        }
    }
}
