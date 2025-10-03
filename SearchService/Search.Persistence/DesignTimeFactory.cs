using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace Search.Persistence
{
    internal class DesignTimeFactory : IDesignTimeDbContextFactory<SearchDbContext>
    {
        public SearchDbContext CreateDbContext(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
               .ConfigureServices((ctx, services) =>
               {
                   services.AddSearchPersistence(ctx.Configuration);
               })
               .Build()
               .Services.CreateScope().ServiceProvider
               .GetRequiredService<SearchDbContext>();
        }
    }
}
