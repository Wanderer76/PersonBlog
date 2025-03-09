using Infrastructure.Extensions;
using Infrastructure.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Profile.Domain.Entities;

namespace Profile.Persistence
{
    public static class ProfilePersistenceExtensions
    {
        public static void AddProfilePersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration["ConnectionStrings:ProfileDbContext"]!;
            services.AddNpgSqlDbContext<ProfileDbContext>(connectionString);
            services.AddScoped<IDbInitializer,ProfileDbInitializer>();
            //services.AddDbContextPool<ProfileDbContext>(option =>
            //option.UseInMemoryDatabase("Profile"));
            //services.AddScoped<IReadWriteRepository<IProfileEntity>, DefaultRepository<ProfileDbContext, IProfileEntity>>();
            services.AddDefaultRepository<ProfileDbContext, IProfileEntity>();
        }
    }
}
