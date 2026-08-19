using Blog.Contracts.Events;
using Blog.Domain.Entities;
using Blog.Persistence;
using Blog.Service.EventHandlers;
using Infrastructure.Services;
using MessageBus;
using MessageBus.EventHandler;
using MessageBus.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;

namespace BlogServiceTests;

public sealed class VideoProcessingSagaTests
{
    [Fact]
    public async Task Converted_response_is_applied_exactly_once()
    {
        await using var context = await CreateContextAsync(includeVideo: true);
        var response = SuccessfulResponse();

        await HandleAsync(context, response);
        context.ChangeTracker.Clear();
        await HandleAsync(context, response);

        Assert.Equal(2, await context.ProfileEventMessages.CountAsync());
        Assert.Single(await context.PostFiles.ToListAsync());

        var post = await context.Posts.Include(x => x.VideoPostInfo).SingleAsync();
        Assert.Equal(ProcessState.Complete, post.ProcessState);
        Assert.Equal(response.VideoMetadataId, post.VideoPostInfo.VideoFileId);
        Assert.Equal(2, post.RecommendationVersion);
    }

    [Fact]
    public async Task Failed_conversion_does_not_publish_post_created_events()
    {
        await using var context = await CreateContextAsync(includeVideo: true);
        var response = SuccessfulResponse();
        response.Error = "conversion failed";
        response.ProcessState = ProcessState.Error;
        response.ObjectName = null!;
        response.PreviewId = null;

        await HandleAsync(context, response);

        Assert.Empty(await context.ProfileEventMessages.ToListAsync());
        Assert.Equal(ProcessState.Error, (await context.Posts.SingleAsync()).ProcessState);
        Assert.Equal("conversion failed", (await context.VideoMetadata.SingleAsync()).ErrorMessage);
    }

    [Fact]
    public async Task Stale_conversion_response_does_not_persist_preview()
    {
        await using var context = await CreateContextAsync(includeVideo: false);

        await HandleAsync(context, SuccessfulResponse());

        Assert.Empty(await context.PostFiles.ToListAsync());
        Assert.Empty(await context.ProfileEventMessages.ToListAsync());
        Assert.Equal(ProcessState.Draft, (await context.Posts.SingleAsync()).ProcessState);
    }

    [Fact]
    public void Third_outbox_failure_moves_message_to_error_state()
    {
        var message = VideoProcessEvent.Create(new { Value = 1 });

        message.RegisterPublishFailure("first");
        Assert.Equal(EventState.Pending, message.State);
        message.RegisterPublishFailure("second");
        Assert.Equal(EventState.Pending, message.State);
        message.RegisterPublishFailure("third");

        Assert.Equal(VideoProcessEvent.MaxPublishAttempts, message.RetryCount);
        Assert.Equal(EventState.Error, message.State);
        Assert.Equal("third", message.ErrorMessage);
    }

    [Fact]
    public async Task Process_state_rejects_concurrent_transitions()
    {
        var databaseName = Guid.NewGuid().ToString();
        var databaseRoot = new InMemoryDatabaseRoot();
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .Options;

        await using (var seedContext = new BlogDbContext(options))
        {
            var post = CreatePost();
            post.ProcessState = ProcessState.Load;
            seedContext.Add(post);
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = new BlogDbContext(options);
        await using var secondContext = new BlogDbContext(options);
        var firstPost = await firstContext.Posts.SingleAsync();
        var secondPost = await secondContext.Posts.SingleAsync();

        firstPost.ProcessState = ProcessState.Draft;
        secondPost.ProcessState = ProcessState.Error;
        await firstContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondContext.SaveChangesAsync());
    }

    private static async Task HandleAsync(BlogDbContext context, VideoConvertedResponse response)
    {
        var repository = CreateRepository(context);
        var readyHandler = new VideoReadyToPublishEventHandler(repository, new NoOpCacheService());
        var sagaHandler = new VideoProcessSagaHandler(repository, readyHandler);
        var publisher = new NoOpMessagePublisher();

        await sagaHandler.Handle(MessageContext.Create(response.VideoMetadataId, response, publisher));
    }

    private static async Task<BlogDbContext> CreateContextAsync(bool includeVideo)
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new BlogDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var post = CreatePost();
        context.Add(post);

        if (includeVideo)
        {
            context.Add(new VideoFile
            {
                Id = TestIds.VideoMetadataId,
                PostId = TestIds.PostId,
                Name = "source.mp4",
                ObjectName = "source/source.mp4",
                ContentType = "video/mp4",
                FileExtension = ".mp4",
                CreatedAt = DateTimeOffset.UtcNow,
                Resolution = FileStorage.Service.Models.VideoResolution.Original
            });
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return context;
    }

    private static Post CreatePost() => new(
            TestIds.PostId,
            TestIds.BlogId,
            PostType.Video,
            "description",
            "title",
            null,
            PostVisibility.Public,
            [],
            null)
        {
            ProcessState = ProcessState.Draft
        };

    private static IReadWriteRepository<IBlogEntity> CreateRepository(BlogDbContext context) =>
        new DefaultRepository<BlogDbContext, IBlogEntity>(
            new DefaultReadRepository<BlogDbContext, IBlogEntity>(context),
            new DefaultWriteRepository<BlogDbContext, IBlogEntity>(context));

    private static VideoConvertedResponse SuccessfulResponse() => new()
    {
        VideoMetadataId = TestIds.VideoMetadataId,
        PostId = TestIds.PostId,
        ObjectName = "video/master.m3u8",
        Duration = 42,
        ProcessState = ProcessState.Complete,
        PreviewId = new BaseFileMetadataEntity
        {
            Id = TestIds.PreviewId,
            Name = "preview.png",
            ObjectName = "video/preview.png",
            ContentType = "image/png",
            FileExtension = ".png",
            CreatedAt = DateTimeOffset.UtcNow
        }
    };

    private static class TestIds
    {
        public static readonly Guid BlogId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid PostId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid VideoMetadataId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid PreviewId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    }

    private sealed class NoOpCacheService : ICacheService
    {
        public Task<T?> GetCachedDataAsync<T>(ICacheKey key) => Task.FromResult(default(T));
        public Task<IEnumerable<T>> GetCachedDataAsync<T>(IEnumerable<ICacheKey> keys) =>
            Task.FromResult<IEnumerable<T>>([]);
        public Task SetCachedDataAsync<T>(ICacheKey key, T data, TimeSpan ttl) where T : notnull =>
            Task.CompletedTask;
        public Task RemoveCachedDataAsync(ICacheKey key) => Task.CompletedTask;
    }

    private sealed class NoOpMessagePublisher : IMessagePublish
    {
        public IRequestClient Value => throw new NotSupportedException();
        public Task PublishAsync<T>(BaseEvent<T> message, MessageProperty? cfg = null) => Task.CompletedTask;
        public Task PublishAsync(BaseEvent message, MessageProperty? cfg = null) => Task.CompletedTask;
    }
}
