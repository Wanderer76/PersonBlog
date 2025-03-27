using Conference.Domain.Entities;
using Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Persistence;

namespace Conference.Persistence.Extensions
{
    public static class ConferencePersistenceExtensions
    {
        public static void AddConferencePersistence(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddInMemoryDbContext<ConferenceDbContext>("Conference");

            services.AddScoped<IWriteRepository<IConferenceEntity>, DefaultWriteRepository<ConferenceDbContext, IConferenceEntity>>();
            services.AddScoped<IReadRepository<IConferenceEntity>, ReadConferenceContext<ConferenceDbContext, IConferenceEntity>>();
            services.AddScoped<IReadWriteRepository<IConferenceEntity>, DefaultRepository<ConferenceDbContext, IConferenceEntity>>();
        }
    }
}
