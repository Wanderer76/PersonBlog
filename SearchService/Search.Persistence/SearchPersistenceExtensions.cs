using Infrastructure.Extensions;
using Infrastructure.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Search.Domain.Entities;
using Search.Persistence.Repositories;

namespace Search.Persistence
{
    public static class SearchPersistenceExtensions
    {
        public static void AddSearchPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration["ConnectionStrings:SearchDbContext"]!;
            services.AddNpgSqlDbContext<SearchDbContext>(connectionString);
            services.AddRedisCache(configuration);
            services.AddScoped<IDbInitializer, SearchDbInitializer>();
            services.AddScoped<NpgSqlSearchRepository>();
            services.AddDefaultRepository<SearchDbContext, ISearch>();
        }
    }
}
