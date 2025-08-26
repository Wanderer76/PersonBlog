using Infrastructure.Extensions;
using Infrastructure.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Music.Domain.Entities;
using Music.Domain.Repositories;
using Music.Persistence.Repositories;

namespace Music.Persistence
{
    public static class MusicPersistenceExtensions
    {
        public static void AddMusicPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration["ConnectionStrings:MusicDbContext"]!;
            services.AddNpgSqlDbContext<MusicDbContext>(connectionString);
            services.AddRedisCache(configuration);
            services.AddScoped<IDbInitializer, MusicDbInitializer>();
            services.AddDefaultRepository<MusicDbContext, IMusicEntity>();
            services.AddScoped<ITempFileMetadataRepository,DefaultTempTrackMetadataRepository>();
        }
    }
}
