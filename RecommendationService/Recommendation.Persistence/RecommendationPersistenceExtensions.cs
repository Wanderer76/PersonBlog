using Infrastructure.Extensions;
using Infrastructure.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Recommendation.Domain.Entities;
using Recommendation.Services.Abstractions;

namespace Recommendation.Persistence;

public static class RecommendationPersistenceExtensions
{
    public const string ConnectionStringName = "RecommendationDbContext";

    public static IServiceCollection AddRecommendationPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");
        }

        services.AddDefaultRepository<RecommendationDbContext, IRecommendationEntity>();
        services.AddNpgSqlDbContext<RecommendationDbContext>(connectionString);
        services.AddScoped<IDbInitializer, RecommendationDbInitializer>();
        services.AddScoped<IRecommendationEventStore, EfRecommendationEventStore>();
        services.AddScoped<IRecommendationFeedStore, EfRecommendationFeedStore>();
        return services;
    }
}
