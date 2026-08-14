using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MessageBus;
using MessageBus.Models;
using Recommendation.Contracts;
using Recommendation.Contracts.Events;
using Recommendation.Services.Abstractions;
using Recommendation.Services.EventHandlers;
using Recommendation.Services.Options;
using Recommendation.Services.Services;

namespace Recommendation.Services;

public static class RecommendationServicesExtensions
{
    public static IServiceCollection AddRecommendationEventServices(
        this IServiceCollection services,
        Action<AffinityOptions>? configureAffinity = null)
    {
        var options = new AffinityOptions();
        configureAffinity?.Invoke(options);

        services.AddSingleton<IOptions<AffinityOptions>>(
            Microsoft.Extensions.Options.Options.Create(options));
        services.TryAddSingleton<IClock, SystemClock>();
        services.AddSingleton<AffinityCalculator>();
        services.AddScoped<PostCatalogChangedV2Handler>();
        services.AddScoped<UserInteractionRecordedV1Handler>();
        services.AddScoped<SubscriptionChangedV1Handler>();
        return services;
    }

    public static IServiceCollection AddRecommendationFeedServices(
        this IServiceCollection services,
        Action<RecommendationFeedOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new RecommendationFeedOptions();
        configure(options);
        services.AddSingleton<IOptions<RecommendationFeedOptions>>(
            Microsoft.Extensions.Options.Options.Create(options));
        services.TryAddSingleton<IClock, SystemClock>();
        services.AddScoped<IRecommendationFeedService, HeuristicRecommendationFeedService>();
        services.AddScoped<IRecommendationCatalogService, RecommendationCatalogService>();
        return services;
    }

    public static IMessageBusBuilder AddRecommendationEventSubscriptions(this IMessageBusBuilder builder)
    {
        builder.AddSubscription<PostCatalogChangedV2, PostCatalogChangedV2Handler>(options =>
        {
            options.QueueName = "recommendation-post-catalog-v2";
            options.Exchange = CreateExchange(RecommendationExchange.PostCatalogChangedV2RoutingKey);
        });
        builder.AddSubscription<UserInteractionRecordedV1, UserInteractionRecordedV1Handler>(options =>
        {
            options.QueueName = "recommendation-user-interaction-v1";
            options.Exchange = CreateExchange(RecommendationExchange.UserInteractionRecordedV1RoutingKey);
        });
        builder.AddSubscription<SubscriptionChangedV1, SubscriptionChangedV1Handler>(options =>
        {
            options.QueueName = "recommendation-subscription-v1";
            options.Exchange = CreateExchange(RecommendationExchange.SubscriptionChangedV1RoutingKey);
        });
        return builder;
    }

    private static ExchangeParam CreateExchange(string routingKey) => new()
    {
        Name = RecommendationExchange.Name,
        RoutingKey = routingKey,
        ExchangeType = "direct",
        Durable = true
    };
}
