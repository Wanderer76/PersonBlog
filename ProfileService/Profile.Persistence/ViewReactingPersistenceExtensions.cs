using Infrastructure.Extensions;
using Infrastructure.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Profile.Domain.Entities;

namespace Profile.Persistence
{
    public static class ViewReactingPersistenceExtensions
    {
        public static void AddViewReactingPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration["ConnectionStrings:VideoReactingDbContext"]!;
            services.AddNpgSqlDbContext<ProfileDbContext>(connectionString);
            services.AddScoped<IDbInitializer, ProfileDbInitializer>();
            services.AddDefaultRepository<ProfileDbContext, IUserEntity>();
        }
    }
}
