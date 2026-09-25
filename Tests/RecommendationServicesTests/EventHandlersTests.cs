using Microsoft.Extensions.Options;
using Recommendation.Contracts.Events;
using Recommendation.Domain.Entities;
using Recommendation.Domain.Enums;
using Recommendation.Domain.Models;
using Recommendation.Services.Abstractions;
using Recommendation.Services.EventHandlers;
using Recommendation.Services.Options;
using Recommendation.Services.Services;

namespace RecommendationServicesTests;

public sealed class EventHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PostCatalogHandler_IsIdempotent_AndIgnoresStaleVersions()
    {
        var store = new InMemoryEventStore();
        var handler = new PostCatalogChangedV2Handler(store, new TestClock(Now));
        var postId = Guid.NewGuid();
        var newest = CatalogEvent(postId, Guid.NewGuid(), version: 2, title: "Newest");

        await handler.HandleAsync(newest);
        await handler.HandleAsync(newest);
        await handler.HandleAsync(CatalogEvent(postId, newest.BlogId, version: 1, title: "Stale"));

        var snapshot = Assert.Single(store.Posts.Values);
        Assert.Equal(2, snapshot.SourceVersion);
        Assert.Equal("Newest", snapshot.Title);
        Assert.Equal(2, store.ProcessedEventCount);
    }

    [Fact]
    public async Task InteractionHandler_StoresInteraction_AndUpdatesAffinitiesOnce()
    {
        var store = new InMemoryEventStore();
        var userId = Guid.NewGuid();
        var post = CreateSnapshot(Guid.NewGuid(), Guid.NewGuid(), [10, 20]);
        store.AddPostSnapshot(post);
        var handler = CreateInteractionHandler(store);
        var message = new UserInteractionRecordedV1
        {
            EventId = Guid.NewGuid(),
            OccurredAt = Now,
            UserId = userId,
            PostId = post.PostId,
            Type = UserInteractionType.Like,
            Reaction = true
        };

        await handler.HandleAsync(message);
        await handler.HandleAsync(message);

        Assert.Single(store.Interactions);
        Assert.Equal(5, store.BlogAffinities[(userId, post.BlogId)].Score);
        Assert.Equal(5, store.CategoryAffinities[(userId, 10)].Score);
        Assert.Equal(5, store.CategoryAffinities[(userId, 20)].Score);
    }

    [Fact]
    public async Task InteractionHandler_DoesNotPersonalizeAnonymousInteraction()
    {
        var store = new InMemoryEventStore();
        var post = CreateSnapshot(Guid.NewGuid(), Guid.NewGuid(), [10]);
        store.AddPostSnapshot(post);
        var handler = CreateInteractionHandler(store);

        await handler.HandleAsync(new UserInteractionRecordedV1
        {
            EventId = Guid.NewGuid(),
            OccurredAt = Now,
            AnonymousSessionId = "session-1",
            PostId = post.PostId,
            Type = UserInteractionType.Open
        });

        Assert.Single(store.Interactions);
        Assert.Empty(store.BlogAffinities);
        Assert.Empty(store.CategoryAffinities);
    }

    [Fact]
    public async Task SubscriptionHandler_ConvergesToLatestState()
    {
        var store = new InMemoryEventStore();
        var handler = new SubscriptionChangedV1Handler(store, new TestClock(Now));
        var userId = Guid.NewGuid();
        var blogId = Guid.NewGuid();

        await handler.HandleAsync(SubscriptionEvent(userId, blogId, true));
        await handler.HandleAsync(SubscriptionEvent(userId, blogId, true));
        Assert.Single(store.Subscriptions);

        await handler.HandleAsync(SubscriptionEvent(userId, blogId, false));
        Assert.Empty(store.Subscriptions);
    }

    private static UserInteractionRecordedV1Handler CreateInteractionHandler(InMemoryEventStore store) =>
        new(store, new TestClock(Now), new AffinityCalculator(Options.Create(new AffinityOptions())));

    private static PostCatalogChangedV2 CatalogEvent(Guid postId, Guid blogId, long version, string title) => new()
    {
        EventId = Guid.NewGuid(),
        AggregateVersion = version,
        OccurredAt = Now,
        PostId = postId,
        BlogId = blogId,
        PostType = RecommendationPostType.Text,
        Title = title,
        CategoryIds = [10, 20],
        Visibility = RecommendationPostVisibility.Public,
        ProcessState = RecommendationProcessState.Complete,
        IsDeleted = false,
        IsBanned = false,
        CreatedAt = Now.AddDays(-1),
        ViewCount = 3,
        LikeCount = 2,
        DislikeCount = 1
    };

    private static SubscriptionChangedV1 SubscriptionEvent(Guid userId, Guid blogId, bool subscribed) => new()
    {
        EventId = Guid.NewGuid(),
        OccurredAt = Now,
        UserId = userId,
        BlogId = blogId,
        IsSubscribed = subscribed
    };

    private static PostSnapshot CreateSnapshot(Guid postId, Guid blogId, IReadOnlyCollection<int> categories) =>
        PostSnapshot.Create(new PostSnapshotData
        {
            PostId = postId,
            SourceVersion = 1,
            BlogId = blogId,
            PostType = PostType.Text,
            Title = "Post",
            CategoryIds = categories,
            Visibility = PostVisibility.Public,
            ProcessState = PostProcessState.Complete,
            IsDeleted = false,
            IsBanned = false,
            CreatedAt = Now.AddDays(-1),
            ViewCount = 0,
            LikeCount = 0,
            DislikeCount = 0,
            UpdatedAt = Now.AddHours(-1)
        });

    private sealed class TestClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class InMemoryEventStore : IRecommendationEventStore, IRecommendationEventSession
    {
        private readonly HashSet<Guid> _processedEvents = [];

        public Dictionary<Guid, PostSnapshot> Posts { get; } = [];
        public List<UserInteraction> Interactions { get; } = [];
        public Dictionary<(Guid UserId, int CategoryId), UserCategoryAffinity> CategoryAffinities { get; } = [];
        public Dictionary<(Guid UserId, Guid BlogId), UserBlogAffinity> BlogAffinities { get; } = [];
        public Dictionary<(Guid UserId, Guid BlogId), UserSubscription> Subscriptions { get; } = [];
        public int ProcessedEventCount => _processedEvents.Count;

        public async Task ExecuteOnceAsync(
            InboxMessage message,
            Func<IRecommendationEventSession, CancellationToken, Task> apply,
            CancellationToken cancellationToken = default)
        {
            if (_processedEvents.Contains(message.EventId)) return;
            await apply(this, cancellationToken);
            _processedEvents.Add(message.EventId);
        }

        public Task<PostSnapshot?> GetPostSnapshotAsync(Guid postId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Posts.GetValueOrDefault(postId));

        public void AddPostSnapshot(PostSnapshot snapshot) => Posts.Add(snapshot.PostId, snapshot);
        public void AddInteraction(UserInteraction interaction) => Interactions.Add(interaction);

        public Task<UserCategoryAffinity?> GetCategoryAffinityAsync(Guid userId, int categoryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CategoryAffinities.GetValueOrDefault((userId, categoryId)));

        public void AddCategoryAffinity(UserCategoryAffinity affinity) =>
            CategoryAffinities.Add((affinity.UserId, affinity.CategoryId), affinity);

        public Task<UserBlogAffinity?> GetBlogAffinityAsync(Guid userId, Guid blogId, CancellationToken cancellationToken = default) =>
            Task.FromResult(BlogAffinities.GetValueOrDefault((userId, blogId)));

        public void AddBlogAffinity(UserBlogAffinity affinity) =>
            BlogAffinities.Add((affinity.UserId, affinity.BlogId), affinity);

        public Task<UserSubscription?> GetSubscriptionAsync(Guid userId, Guid blogId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Subscriptions.GetValueOrDefault((userId, blogId)));

        public void AddSubscription(UserSubscription subscription) =>
            Subscriptions.Add((subscription.UserId, subscription.BlogId), subscription);

        public void RemoveSubscription(UserSubscription subscription) =>
            Subscriptions.Remove((subscription.UserId, subscription.BlogId));
    }
}
