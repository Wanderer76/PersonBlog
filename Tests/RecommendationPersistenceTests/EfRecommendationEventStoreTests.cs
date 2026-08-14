using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Interface;
using Recommendation.Domain.Entities;
using Recommendation.Domain.Enums;
using Recommendation.Domain.Models;
using Recommendation.Persistence;
using Recommendation.Services.Abstractions;
using Shared.Persistence;

namespace RecommendationPersistenceTests;

public sealed class EfRecommendationEventStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 4, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteOnceAsync_CommitsInboxAndProjectionExactlyOnce()
    {
        await using var context = CreateContext();
        var store = new EfRecommendationEventStore(context, new TestClock(Now.AddSeconds(1)));
        var message = new InboxMessage(Guid.NewGuid(), "PostCatalogChangedV2", Now);
        var postId = Guid.NewGuid();
        var calls = 0;

        Task Apply(IRecommendationEventSession session, CancellationToken _)
        {
            calls++;
            session.AddPostSnapshot(CreateSnapshot(postId));
            return Task.CompletedTask;
        }

        await store.ExecuteOnceAsync(message, Apply);
        await store.ExecuteOnceAsync(
            new InboxMessage(message.EventId, message.EventType, Now.AddMinutes(1)),
            Apply);

        Assert.Equal(1, calls);
        Assert.Equal(1, await context.PostSnapshots.CountAsync());
        var inbox = Assert.Single(await context.InboxMessages.ToListAsync());
        Assert.True(inbox.IsProcessed);
        Assert.Equal(Now.AddSeconds(1), inbox.ProcessedAt);
    }

    [Fact]
    public async Task ExecuteOnceAsync_FailureDoesNotLeaveInboxReservation()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(databaseName))
        {
            var store = new EfRecommendationEventStore(context, new TestClock(Now.AddSeconds(1)));
            var message = new InboxMessage(Guid.NewGuid(), "BrokenEvent", Now);

            await Assert.ThrowsAsync<InvalidOperationException>(() => store.ExecuteOnceAsync(
                message,
                (session, _) =>
                {
                    session.AddPostSnapshot(CreateSnapshot(Guid.NewGuid()));
                    throw new InvalidOperationException("Projection failed.");
                }));
        }

        await using var verificationContext = CreateContext(databaseName);
        Assert.Empty(await verificationContext.InboxMessages.ToListAsync());
        Assert.Empty(await verificationContext.PostSnapshots.ToListAsync());
    }

    [Fact]
    public void PostgreSqlModel_ContainsReadModelConstraintsAndIndexes()
    {
        var options = new DbContextOptionsBuilder<RecommendationDbContext>()
            .UseNpgsql("Host=localhost;Database=model_check;Username=postgres;Password=postgres")
            .Options;
        using var context = new RecommendationDbContext(options);

        Assert.IsAssignableFrom<BaseDbContext>(context);
        var migration = Assert.Single(context.Database.GetMigrations());
        var script = context.GetService<IMigrator>().GenerateScript();

        Assert.Equal("20260813041353_InitialRecommendationReadModel", migration);
        Assert.Contains("CREATE SCHEMA \"Recommendation\"", script);
        Assert.Contains("CREATE TABLE \"Recommendation\".\"InboxMessages\"", script);
        Assert.Contains("CREATE TABLE \"Recommendation\".\"PostSnapshots\"", script);
        Assert.Contains("CK_UserInteractions_Subject", script);
        Assert.Contains("IX_UserCategoryAffinities_UserId_Score", script);
    }

    [Fact]
    public void AddRecommendationPersistence_RegistersCommonPersistenceServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:RecommendationDbContext"] =
                    "Host=localhost;Database=recommendation;Username=postgres;Password=postgres"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddRecommendationPersistence(configuration);

        Assert.Contains(services, x => x.ServiceType == typeof(IReadRepository<IRecommendationEntity>));
        Assert.Contains(services, x => x.ServiceType == typeof(IWriteRepository<IRecommendationEntity>));
        Assert.Contains(services, x => x.ServiceType == typeof(IReadWriteRepository<IRecommendationEntity>));
        Assert.Contains(services, x => x.ServiceType == typeof(IDbInitializer));
        Assert.Contains(services, x => x.ServiceType == typeof(IRecommendationEventStore));
        Assert.Contains(services, x => x.ServiceType == typeof(IRecommendationFeedStore));
    }

    [Fact]
    public async Task FeedStore_LoadsPersonalSignalsFromOwnReadModel()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var blogId = Guid.NewGuid();
        var post = CreateSnapshot(Guid.NewGuid(), blogId, [10]);
        context.PostSnapshots.Add(post);
        context.UserCategoryAffinities.Add(new UserCategoryAffinity(userId, 10, 7, Now));
        context.UserBlogAffinities.Add(new UserBlogAffinity(userId, blogId, 4, Now));
        context.UserSubscriptions.Add(new UserSubscription(userId, blogId, Now));
        context.UserInteractions.Add(new UserInteraction(
            Guid.NewGuid(), userId, null, post.PostId, InteractionType.Open, Now));
        await context.SaveChangesAsync();
        var store = new EfRecommendationFeedStore(context);

        var candidates = await store.LoadCandidatesAsync(
            userId, null, 20, Now.AddDays(-1));

        var candidate = Assert.Single(candidates);
        Assert.Equal(7, candidate.CategoryAffinity);
        Assert.Equal(4, candidate.BlogAffinity);
        Assert.True(candidate.IsSubscribed);
        Assert.True(candidate.WasSeen);
    }

    private static RecommendationDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<RecommendationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
        return new RecommendationDbContext(options);
    }

    private static PostSnapshot CreateSnapshot(
        Guid postId,
        Guid? blogId = null,
        IReadOnlyCollection<int>? categories = null) => PostSnapshot.Create(new PostSnapshotData
    {
        PostId = postId,
        SourceVersion = 1,
        BlogId = blogId ?? Guid.NewGuid(),
        PostType = PostType.Text,
        Title = "Test post",
        CategoryIds = categories ?? [10],
        Visibility = PostVisibility.Public,
        ProcessState = PostProcessState.Complete,
        IsDeleted = false,
        IsBanned = false,
        CreatedAt = Now.AddDays(-1),
        ViewCount = 0,
        LikeCount = 0,
        DislikeCount = 0,
        UpdatedAt = Now
    });

    private sealed class TestClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
