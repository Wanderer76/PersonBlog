using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Persistence;

namespace Infrastructure.Extensions
{
    public static class DbContextExtensions
    {
        public static void AddNpgSqlDbContext<TDbContext>(this IServiceCollection services, string connectionString)
        where TDbContext : BaseDbContext
        {
            var schemaName = typeof(TDbContext).Name.Replace("DbContext", "");

            services.AddDbContextPool<TDbContext>(option =>
            option.UseNpgsql(connectionString, x =>
            {
                x.MigrationsHistoryTable($"_{schemaName}_MigrationsHistory", schemaName);
            }));
        }

        public static void AddInMemoryDbContext<TDbContext>(this IServiceCollection services, string dbName)
            where TDbContext : BaseDbContext
        {
            services.AddDbContextPool<TDbContext>(option => option.UseInMemoryDatabase(dbName));
        }
    }
}
