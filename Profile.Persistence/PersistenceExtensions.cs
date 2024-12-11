using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Profile.Domain.Entities;
using Shared.Persistence;

namespace Profile.Persistence
{
    public static class PersistenceExtensions
    {
        public static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration["ConnectionStrings:ProfileContext"];
            services.AddDbContextPool<ProfileDbContext>(option =>
            option.UseInMemoryDatabase("Profile")
                //option.UseNpgsql(connectionString)
                );
            services.AddScoped<IReadWriteRepository<IProfileEntity>, DefaultRepository<ProfileDbContext, IProfileEntity>>();

        }
    }
}
