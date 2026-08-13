using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Recommendation.Persistence;

public sealed class RecommendationDbContextFactory : IDesignTimeDbContextFactory<RecommendationDbContext>
{
    public RecommendationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("RECOMMENDATION_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=person_blog;Username=postgres;Password=1";
        var options = new DbContextOptionsBuilder<RecommendationDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable(
                    "_Recommendation_MigrationsHistory",
                    RecommendationDbContext.Schema))
            .Options;
        return new RecommendationDbContext(options);
    }
}
