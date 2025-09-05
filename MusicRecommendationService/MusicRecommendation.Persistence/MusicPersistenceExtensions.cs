using Infrastructure.Extensions;
using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MusicRecommendation.Domain.Domain;
using MusicRecommendation.Domain.Repositories;
using MusicRecommendation.Persistence.Repositories;
using Npgsql;

namespace MusicRecommendation.Persistence
{
    public static class MusicPersistenceExtensions
    {
        public static void AddMusicRecommendationPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration["ConnectionStrings:MusicRecommendationDbContext"]!;
            //services.AddDbContextPool<MusicRecommendationDbContext>(opt=>
            //{
            //    var ds = new NpgsqlDataSourceBuilder(connectionString);
            //    ds.EnableDynamicJson();
            //    opt.UseNpgsql(ds.Build());
            //});
            services.AddDefaultRepository<MusicRecommendationDbContext, IRecommendation>();
            services.AddNpgSqlDbContext<MusicRecommendationDbContext>(connectionString);
            services.AddRedisCache(configuration);
            services.AddScoped<IDbInitializer, MusicDbInitializer>();
            services.AddScoped<ITrackRecommendationRepository,TrackRecommendationRepository>();
            services.AddScoped<ITrackRepository,TrackRepository>();
            services.AddScoped<IUserListenHistoryRepository, UserListenHistoryRepository>();
        }
    }
}
