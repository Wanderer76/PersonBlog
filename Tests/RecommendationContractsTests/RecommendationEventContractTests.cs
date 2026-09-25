using System.Text.Json;
using MessageBus.Models;
using Recommendation.Contracts;
using Recommendation.Contracts.Events;

namespace RecommendationContractsTests;

public sealed class RecommendationEventContractTests
{
    [Fact]
    public void Recommendation_routes_are_stable()
    {
        Assert.Equal("recommendation", RecommendationExchange.Name);
        Assert.Equal("post.catalog.changed.v2", RecommendationExchange.PostCatalogChangedV2RoutingKey);
        Assert.Equal("user.interaction.recorded.v1", RecommendationExchange.UserInteractionRecordedV1RoutingKey);
        Assert.Equal("subscription.changed.v1", RecommendationExchange.SubscriptionChangedV1RoutingKey);
    }

    [Fact]
    public void Post_catalog_event_round_trips_through_bus_envelope()
    {
        var eventId = Guid.NewGuid();
        var message = new PostCatalogChangedV2
        {
            EventId = eventId,
            AggregateVersion = 2,
            OccurredAt = DateTimeOffset.UtcNow,
            PostId = Guid.NewGuid(),
            BlogId = Guid.NewGuid(),
            PostType = RecommendationPostType.Video,
            Title = "Post",
            CategoryIds = [1, 2],
            Visibility = RecommendationPostVisibility.Public,
            ProcessState = RecommendationProcessState.Complete,
            IsDeleted = false,
            IsBanned = false,
            CreatedAt = DateTimeOffset.UtcNow,
            ViewCount = 10,
            LikeCount = 3,
            DislikeCount = 1
        };

        var json = JsonSerializer.Serialize(BaseEvent<PostCatalogChangedV2>.Create(message));
        var restored = JsonSerializer.Deserialize<BaseEvent<PostCatalogChangedV2>>(json);

        Assert.NotNull(restored);
        Assert.Equal(nameof(PostCatalogChangedV2), restored.EventType);
        Assert.Equal(eventId, restored.EventData.EventId);
        Assert.Equal([1, 2], restored.EventData.CategoryIds);
    }

    [Fact]
    public void User_interaction_event_round_trips_through_bus_envelope()
    {
        var eventId = Guid.NewGuid();
        var message = new UserInteractionRecordedV1
        {
            EventId = eventId,
            OccurredAt = DateTimeOffset.UtcNow,
            UserId = Guid.NewGuid(),
            PostId = Guid.NewGuid(),
            Type = UserInteractionType.Like,
            WatchedSeconds = 42,
            Reaction = true,
            PreviousReaction = false
        };

        var json = JsonSerializer.Serialize(BaseEvent<UserInteractionRecordedV1>.Create(message));
        var restored = JsonSerializer.Deserialize<BaseEvent<UserInteractionRecordedV1>>(json);

        Assert.NotNull(restored);
        Assert.Equal(eventId, restored.EventData.EventId);
        Assert.Equal(UserInteractionType.Like, restored.EventData.Type);
        Assert.True(restored.EventData.Reaction);
        Assert.False(restored.EventData.PreviousReaction);
    }

    [Fact]
    public void Subscription_event_round_trips_through_bus_envelope()
    {
        var eventId = Guid.NewGuid();
        var message = new SubscriptionChangedV1
        {
            EventId = eventId,
            OccurredAt = DateTimeOffset.UtcNow,
            UserId = Guid.NewGuid(),
            BlogId = Guid.NewGuid(),
            IsSubscribed = true
        };

        var json = JsonSerializer.Serialize(BaseEvent<SubscriptionChangedV1>.Create(message));
        var restored = JsonSerializer.Deserialize<BaseEvent<SubscriptionChangedV1>>(json);

        Assert.NotNull(restored);
        Assert.Equal(eventId, restored.EventData.EventId);
        Assert.True(restored.EventData.IsSubscribed);
    }
}
