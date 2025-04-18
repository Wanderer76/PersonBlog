using Infrastructure.Extensions;
using Infrastructure.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ViewReacting.Domain.Entities;

namespace VideoReacting.Persistence
{
    public static class ViewReactingPersistenceExtensions
    {
        public static void AddViewReactingPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration["ConnectionStrings:VideoReactingDbContext"]!;
            services.AddNpgSqlDbContext<ViewReactingDbContext>(connectionString);
            services.AddScoped<IDbInitializer, ViewReactingDbInitializer>();
            services.AddDefaultRepository<ViewReactingDbContext, IVideoReactEntity>();
        }
    }
}
