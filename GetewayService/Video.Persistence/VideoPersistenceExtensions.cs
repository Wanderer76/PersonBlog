using Infrastructure.Extensions;
using Infrastructure.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Video.Domain.Entities;

namespace Video.Persistence
{
    public static class VideoPersistenceExtensions
    {
        public static void AddVideoPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration["ConnectionStrings:VideoDbContext"]!;
            services.AddNpgSqlDbContext<VideoDbContext>(connectionString);
            services.AddScoped<IDbInitializer, VideoDbContextInitializer>();

            services.AddDefaultRepository<VideoDbContext, IVideoViewEntity>();
        }
    }
}
