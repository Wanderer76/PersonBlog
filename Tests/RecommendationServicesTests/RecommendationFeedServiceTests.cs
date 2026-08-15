using Microsoft.Extensions.Options;
using Recommendation.Domain.Entities;
using Recommendation.Services.Abstractions;
using Recommendation.Services.Models;
using Recommendation.Services.Options;
using Recommendation.Services.Services;

namespace RecommendationServicesTests;

public sealed class RecommendationFeedServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 14, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Feed_PrioritizesPersonalAffinity_AndPersistsImpression()
    {
        var preferredPost = Guid.NewGuid();
        var store = new FeedStore([
            Candidate(preferredPost, Guid.NewGuid(), categoryAffinity: 20),
            Candidate(Guid.NewGuid(), Guid.NewGuid(), views: 10_000)
        ]);
        var service = CreateService(store);

        var response = await service.GetFeedAsync(new RecommendationFeedRequest(
            Guid.NewGuid(), null, 2, null, null));

        Assert.Equal(preferredPost, response.Items[0].PostId);
        Assert.Equal("preferred_category", response.Items[0].Reason);
        Assert.Equal(2, store.Impressions.Count);
        Assert.All(store.Impressions, x => Assert.Equal(response.RequestId, x.RequestId));
    }

    [Fact]
    public async Task Cursor_ReturnsStableNextPageWithoutDuplicates()
    {
        var store = new FeedStore(Enumerable.Range(0, 6)
            .Select(index => Candidate(Guid.NewGuid(), Guid.NewGuid(), views: 100 - index))
            .ToArray());
        var service = CreateService(store);
        const string sessionId = "anonymous-session";

        var first = await service.GetFeedAsync(new RecommendationFeedRequest(
            null, sessionId, 2, null, null));
        var second = await service.GetFeedAsync(new RecommendationFeedRequest(
            null, sessionId, 2, first.NextCursor, null));

        Assert.NotNull(first.NextCursor);
        Assert.Equal(first.RequestId, second.RequestId);
        Assert.Empty(first.Items.Select(x => x.PostId).Intersect(second.Items.Select(x => x.PostId)));
        Assert.Equal(4, store.Impressions.Count);
    }

    [Fact]
    public async Task Feed_DoesNotDropPostsWhenSingleBlogExceedsDiversificationLimit()
    {
        var blogId = Guid.NewGuid();
        var postIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        var store = new FeedStore(postIds
            .Select((postId, index) => Candidate(postId, blogId, views: 100 - index))
            .ToArray());
        var service = CreateService(store);

        var response = await service.GetFeedAsync(new RecommendationFeedRequest(
            null, "single-blog-session", 10, null, null));

        Assert.Equal(3, response.Items.Count);
        Assert.Null(response.NextCursor);
        Assert.Equal(postIds.Order(), response.Items.Select(x => x.PostId).Order());
    }

    [Fact]
    public async Task Cursor_DoesNotLoseDeferredPostsFromSameBlog()
    {
        var blogId = Guid.NewGuid();
        var store = new FeedStore(Enumerable.Range(0, 5)
            .Select(index => Candidate(Guid.NewGuid(), blogId, views: 100 - index))
            .ToArray());
        var service = CreateService(store);
        const string sessionId = "same-blog-session";

        var first = await service.GetFeedAsync(new RecommendationFeedRequest(
            null, sessionId, 2, null, null));
        var second = await service.GetFeedAsync(new RecommendationFeedRequest(
            null, sessionId, 2, first.NextCursor, null));
        var third = await service.GetFeedAsync(new RecommendationFeedRequest(
            null, sessionId, 2, second.NextCursor, null));

        var postIds = first.Items.Concat(second.Items).Concat(third.Items)
            .Select(x => x.PostId)
            .ToArray();
        Assert.Equal(5, postIds.Length);
        Assert.Equal(5, postIds.Distinct().Count());
        Assert.Null(third.NextCursor);
    }

    [Fact]
    public async Task Cursor_RejectsTampering()
    {
        var store = new FeedStore(Enumerable.Range(0, 3)
            .Select(_ => Candidate(Guid.NewGuid(), Guid.NewGuid()))
            .ToArray());
        var service = CreateService(store);
        var first = await service.GetFeedAsync(new RecommendationFeedRequest(
            null, "session", 1, null, null));
        var cursor = first.NextCursor!;
        var tampered = $"{(cursor[0] == 'A' ? 'B' : 'A')}{cursor[1..]}";

        await Assert.ThrowsAsync<InvalidRecommendationCursorException>(() =>
            service.GetFeedAsync(new RecommendationFeedRequest(
                null, "session", 1, tampered, null)));
    }

    private static HeuristicRecommendationFeedService CreateService(FeedStore store) => new(
        store,
        new TestClock(Now),
        Options.Create(new RecommendationFeedOptions
        {
            CursorSigningKey = "unit-test-cursor-signing-key-32-bytes",
            CandidatePoolSize = 100,
            MaxLimit = 10,
            MaxPostsPerBlog = 2
        }));

    private static RecommendationCandidateData Candidate(
        Guid postId,
        Guid blogId,
        int views = 0,
        double categoryAffinity = 0) => new(
        postId,
        blogId,
        Now.AddHours(-1),
        views,
        0,
        0,
        categoryAffinity,
        0,
        0,
        false,
        false);

    private sealed class TestClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FeedStore(IReadOnlyList<RecommendationCandidateData> candidates)
        : IRecommendationFeedStore
    {
        public List<RecommendationImpression> Impressions { get; } = [];

        public Task<IReadOnlyList<RecommendationCandidateData>> LoadCandidatesAsync(
            Guid? userId,
            Guid? currentPostId,
            int limit,
            DateTimeOffset seenSince,
            CancellationToken cancellationToken = default) => Task.FromResult(candidates);

        public Task SaveImpressionsAsync(
            IReadOnlyCollection<RecommendationImpression> impressions,
            CancellationToken cancellationToken = default)
        {
            foreach (var impression in impressions)
            {
                if (Impressions.All(x => x.RequestId != impression.RequestId || x.PostId != impression.PostId))
                    Impressions.Add(impression);
            }
            return Task.CompletedTask;
        }

    }
}
