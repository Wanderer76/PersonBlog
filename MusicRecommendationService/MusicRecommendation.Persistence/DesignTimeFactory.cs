using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MusicRecommendation.Persistence
{
    internal class DesignTimeFactory : IDesignTimeDbContextFactory<MusicRecommendationDbContext>
    {
        public MusicRecommendationDbContext CreateDbContext(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
               .ConfigureServices((ctx, services) =>
               {
                   services.AddMusicRecommendationPersistence(ctx.Configuration);
               })
               .Build()
               .Services.CreateScope().ServiceProvider
               .GetRequiredService<MusicRecommendationDbContext>();
        }
    }
}
