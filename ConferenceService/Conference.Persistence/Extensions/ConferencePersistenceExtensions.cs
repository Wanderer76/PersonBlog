using Conference.Domain.Entities;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Persistence;

namespace Conference.Persistence.Extensions
{
    public static class ConferencePersistenceExtensions
    {
        public static void AddConferencePersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration["ConnectionStrings:ConferenceDbContext"]
                ?? throw new InvalidOperationException("Conference database connection string is not configured.");
            services.AddNpgSqlDbContext<ConferenceDbContext>(connectionString);

            services.AddScoped<IReadWriteRepository<IConferenceEntity>, ConferenceRepository>();
            services.AddScoped<IDbInitializer, ConferenceDbInitializer>();
        }
    }
}
