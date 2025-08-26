using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Music.Persistence
{
    internal class DesignTimeFactory : IDesignTimeDbContextFactory<MusicDbContext>
    {
        public MusicDbContext CreateDbContext(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
               .ConfigureServices((ctx, services) =>
               {
                   services.AddMusicPersistence(ctx.Configuration);
               })
               .Build()
               .Services.CreateScope().ServiceProvider
               .GetRequiredService<MusicDbContext>();
        }
    }
}
