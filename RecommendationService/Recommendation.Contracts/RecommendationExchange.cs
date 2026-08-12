namespace Recommendation.Contracts;

public static class RecommendationExchange
{
    public const string Name = "recommendation";
    public const string PostCatalogChangedV2RoutingKey = "post.catalog.changed.v2";
    public const string UserInteractionRecordedV1RoutingKey = "user.interaction.recorded.v1";
    public const string SubscriptionChangedV1RoutingKey = "subscription.changed.v1";
}
