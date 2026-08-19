using Authentication.Contract.Constants;
using Blog.Contracts.Events;
using Blog.Contracts.Models;
using Blog.Contracts.Models.Post;
using Blog.Contracts.Services;
using Blog.Domain.Entities;
using Blog.Persistence;
using Blog.Service.EventHandlers;
using Blog.Service.Services.Implementation;
using Infrastructure.Models;
using Infrastructure.Services;
using MessageBus;
using MessageBus.EventHandler;
using MessageBus.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Profile.Domain.Events;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;

namespace BlogServiceTests;

public sealed class BlogFeatureIntegrationTests
{
    [Fact]
    public async Task Text_post_crud_persists_and_removes_uploaded_file()
    {
        await using var context = await CreateContextAsync();
        context.Add(CreateBlog());
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var storage = new TrackingFileStorage();
        var service = CreateProfileService(context, OwnerUser(), storage);
        await using var content = new MemoryStream("attachment"u8.ToArray());

        var createResult = await service.CreatePostAsync(new PostCreateCommand(
            PostType.Text,
            "First title",
            PostVisibility.Private,
            null,
            "First text",
            [],
            [new FileMetadataModel
            {
                Name = "attachment",
                FileName = "attachment.txt",
                FileExtension = ".txt",
                Length = content.Length,
                ContentType = "text/plain",
                ContentStream = content
            }],
            null));

        Assert.True(createResult.IsSuccess);
        context.ChangeTracker.Clear();
        var createdPost = await context.Posts
            .Include(post => post.TextPostInfo).ThenInclude(info => info.Files)
            .SingleAsync();
        var uploadedFile = Assert.Single(createdPost.TextPostInfo.Files);
        Assert.Contains(uploadedFile.ObjectName, storage.UploadedObjects);

        var readResult = await service.GetTextPostEditViewModelAsync(createdPost.Id);
        Assert.True(readResult.IsSuccess);
        Assert.Equal("First text", readResult.Value.Text);
        context.ChangeTracker.Clear();

        var updateResult = await service.UpdateTextPostAsync(new TextPostEditDto
        {
            Id = createdPost.Id,
            Title = "Updated title",
            Text = "Updated text",
            Visibility = PostVisibility.Public,
            RemovedMediaIds = [uploadedFile.Id]
        });

        Assert.True(updateResult.IsSuccess);
        Assert.Contains(uploadedFile.ObjectName, storage.RemovedObjects);
        context.ChangeTracker.Clear();
        var updatedPost = await context.Posts.Include(post => post.TextPostInfo).SingleAsync();
        Assert.Equal("Updated title", updatedPost.Title);
        Assert.Equal("Updated text", updatedPost.TextPostInfo.Text);
        Assert.Equal(PostVisibility.Public, updatedPost.Visibility);
        Assert.Empty(await context.PostFiles.ToListAsync());
        context.ChangeTracker.Clear();

        var removeResult = await service.RemovePostAsync(createdPost.Id);

        Assert.True(removeResult.IsSuccess);
        context.ChangeTracker.Clear();
        Assert.True((await context.Posts.SingleAsync()).IsDelete);
        Assert.Single(await context.PostRemoveEvents.ToListAsync());
        Assert.True(await context.ProfileEventMessages.CountAsync() >= 3);
    }

    [Fact]
    public async Task Anonymous_reactions_are_isolated_by_ip_and_same_reaction_toggles_off()
    {
        await using var context = await CreateContextAsync();
        context.AddRange(CreateBlog(), CreateVideoPost(PostVisibility.Public, ProcessState.Complete));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var cache = new TrackingCacheService();
        var service = CreatePostService(context, UserModel.AnonymousUser(), cache);

        await service.SetReactionToPost(new ReactionCreateModel
        {
            PostId = PostId,
            RemoteIp = "10.0.0.1",
            IsLike = true
        });
        context.ChangeTracker.Clear();
        await service.SetReactionToPost(new ReactionCreateModel
        {
            PostId = PostId,
            RemoteIp = "10.0.0.2",
            IsLike = false
        });
        context.ChangeTracker.Clear();

        var post = await context.Posts.SingleAsync();
        Assert.Equal(1, post.LikeCount);
        Assert.Equal(1, post.DislikeCount);
        Assert.Equal(2, post.ViewCount);
        Assert.Equal(2, await context.PostViewers.CountAsync());
        context.ChangeTracker.Clear();

        await service.SetReactionToPost(new ReactionCreateModel
        {
            PostId = PostId,
            RemoteIp = "10.0.0.1",
            IsLike = true
        });

        context.ChangeTracker.Clear();
        post = await context.Posts.SingleAsync();
        Assert.Equal(0, post.LikeCount);
        Assert.Equal(1, post.DislikeCount);
        Assert.NotEmpty(cache.RemovedKeys);

        context.ChangeTracker.Clear();
        var authenticatedUserId = Guid.NewGuid();
        await service.SetReactionToPost(new ReactionCreateModel
        {
            PostId = PostId,
            UserId = authenticatedUserId,
            RemoteIp = null,
            IsLike = true
        });
        context.ChangeTracker.Clear();
        Assert.Null((await context.PostViewers.SingleAsync(viewer => viewer.UserId == authenticatedUserId)).UserIpAddress);
    }

    [Fact]
    public async Task Video_visibility_honors_private_banned_owner_and_moderator_rules()
    {
        await using var context = await CreateContextAsync();
        var publicPost = CreateVideoPost(PostVisibility.Public, ProcessState.Complete, Guid.NewGuid());
        var privatePost = CreateVideoPost(PostVisibility.Private, ProcessState.Complete, Guid.NewGuid());
        var bannedPost = CreateVideoPost(PostVisibility.Public, ProcessState.Complete, Guid.NewGuid());
        bannedPost.SetPostBanned(new BanMessage(Guid.NewGuid(), bannedPost.Id, DateTimeOffset.UtcNow, "blocked"));
        context.AddRange(CreateBlog(), publicPost, privatePost, bannedPost);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var currentUser = new MutableCurrentUserService(UserModel.AnonymousUser());
        var service = CreatePostService(context, currentUser, new TrackingCacheService());

        Assert.True(await service.CanAccessVideoAsync(BlogId, publicPost.Id));
        Assert.False(await service.CanAccessVideoAsync(BlogId, privatePost.Id));
        Assert.False(await service.CanAccessVideoAsync(BlogId, bannedPost.Id));

        currentUser.User = OwnerUser();
        Assert.True(await service.CanAccessVideoAsync(BlogId, privatePost.Id));
        Assert.True(await service.CanAccessVideoAsync(BlogId, bannedPost.Id));

        currentUser.User = new UserModel(Guid.NewGuid(), "moderator", null, Guid.Empty, [Roles.AdminRoleId]);
        Assert.False(await service.CanAccessVideoAsync(BlogId, privatePost.Id));
        Assert.True(await service.CanAccessVideoAsync(BlogId, bannedPost.Id));
    }

    [Fact]
    public async Task Video_upload_is_owner_only_and_completion_is_idempotent()
    {
        await using var context = await CreateContextAsync();
        context.AddRange(CreateBlog(), CreateVideoPost(PostVisibility.Public, ProcessState.Draft));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var currentUser = new MutableCurrentUserService(ForeignUser());
        var service = CreateVideoService(context, currentUser);
        var request = new InitiateUploadRequest
        {
            PostId = PostId,
            ObjectName = $"{PostId}/source.mp4",
            Size = 1024,
            ContentType = "video/mp4",
            FileExtension = ".mp4",
            FileName = "source.mp4",
            Duration = 12
        };

        var foreignResult = await service.InitVideoUploadAsync(request);
        Assert.Contains(foreignResult.Errors, error => error.Key == "Forbidden");
        Assert.Empty(await context.VideoMetadata.ToListAsync());

        currentUser.User = OwnerUser();
        var initResult = await service.InitVideoUploadAsync(request);
        Assert.True(initResult.IsSuccess);
        context.ChangeTracker.Clear();
        Assert.Equal(ProcessState.Load, (await context.Posts.SingleAsync()).ProcessState);
        var metadata = await context.VideoMetadata.SingleAsync();
        Assert.Equal(request.ObjectName, metadata.ObjectName);
        context.ChangeTracker.Clear();

        Assert.True((await service.CompleteUploadAsync(PostId)).IsSuccess);
        context.ChangeTracker.Clear();
        Assert.True((await service.CompleteUploadAsync(PostId)).IsSuccess);
        context.ChangeTracker.Clear();

        Assert.Equal(ProcessState.Draft, (await context.Posts.SingleAsync()).ProcessState);
        var conversionEvents = await context.ProfileEventMessages
            .Where(message => message.EventType == nameof(ConvertVideoCommand))
            .ToListAsync();
        Assert.Single(conversionEvents);
        Assert.Equal(metadata.Id, conversionEvents[0].CorrelationId);
    }

    [Fact]
    public async Task Subscribe_and_cancel_events_are_idempotent()
    {
        await using var context = await CreateContextAsync();
        context.Add(CreateBlog());
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var handler = new SubscribeHandlers(CreateRepository(context));
        var publisher = new StubMessagePublisher();
        var subscriberId = Guid.NewGuid();
        var subscribe = new SubscribeCreateEvent
        {
            BlogId = BlogId,
            UserId = subscriberId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await handler.Handle(MessageContext.Create(Guid.NewGuid(), subscribe, publisher));
        context.ChangeTracker.Clear();
        await handler.Handle(MessageContext.Create(Guid.NewGuid(), subscribe, publisher));
        context.ChangeTracker.Clear();

        Assert.Single(await context.Subscribers.Where(item => item.SubscriptionEndDate == null).ToListAsync());
        Assert.Equal(1, (await context.Blogs.SingleAsync()).SubscriptionsCount);
        context.ChangeTracker.Clear();

        var cancel = new SubscribeCancelEvent
        {
            BlogId = BlogId,
            UserId = subscriberId,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(1)
        };
        await handler.Handle(MessageContext.Create(Guid.NewGuid(), cancel, publisher));
        context.ChangeTracker.Clear();
        await handler.Handle(MessageContext.Create(Guid.NewGuid(), cancel, publisher));
        context.ChangeTracker.Clear();

        Assert.NotNull((await context.Subscribers.SingleAsync()).SubscriptionEndDate);
        Assert.Equal(0, (await context.Blogs.SingleAsync()).SubscriptionsCount);
    }

    [Fact]
    public async Task Outbox_publishes_pending_message_and_marks_it_processed()
    {
        await using var context = await CreateContextAsync();
        context.Add(VideoProcessEvent.Create(new PostRemoveEvent(PostId, DateTimeOffset.UtcNow)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var messageBus = new StubMessagePublisher();
        var publisher = new OutboxPublisher(CreateRepository(context), messageBus);

        var count = await publisher.PublishPendingAsync();

        Assert.Equal(1, count);
        Assert.Single(messageBus.PublishedMessages);
        context.ChangeTracker.Clear();
        Assert.Equal(EventState.Processed, (await context.ProfileEventMessages.SingleAsync()).State);
    }

    [Fact]
    public async Task Outbox_moves_message_to_error_after_three_publish_failures()
    {
        var options = CreateOptions();
        await using (var seedContext = new BlogDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Add(VideoProcessEvent.Create(new PostRemoveEvent(PostId, DateTimeOffset.UtcNow)));
            await seedContext.SaveChangesAsync();
        }

        for (var attempt = 1; attempt <= VideoProcessEvent.MaxPublishAttempts; attempt++)
        {
            await using var attemptContext = new BlogDbContext(options);
            var publisher = new OutboxPublisher(CreateRepository(attemptContext), new StubMessagePublisher(shouldFail: true));
            Assert.Equal(1, await publisher.PublishPendingAsync());
        }

        await using var verificationContext = new BlogDbContext(options);
        var message = await verificationContext.ProfileEventMessages.SingleAsync();
        Assert.Equal(EventState.Error, message.State);
        Assert.Equal(VideoProcessEvent.MaxPublishAttempts, message.RetryCount);
        verificationContext.ChangeTracker.Clear();
        var ignored = new OutboxPublisher(CreateRepository(verificationContext), new StubMessagePublisher());
        Assert.Equal(0, await ignored.PublishPendingAsync());
    }

    private static DefaultProfilePostV2Service CreateProfileService(
        BlogDbContext context,
        UserModel user,
        TrackingFileStorage storage) =>
        new(CreateRepository(context), new StubFileStorageFactory(storage), new MutableCurrentUserService(user), null!, null!, new PassThroughImageConvertService());

    private static DefaultPostService CreatePostService(
        BlogDbContext context,
        UserModel user,
        TrackingCacheService cache) =>
        CreatePostService(context, new MutableCurrentUserService(user), cache);

    private static DefaultPostService CreatePostService(
        BlogDbContext context,
        ICurrentUserService currentUser,
        TrackingCacheService cache) =>
        new(CreateRepository(context), new StubFileStorageFactory(new TrackingFileStorage()), cache, currentUser);

    private static DefaultVideoService CreateVideoService(BlogDbContext context, ICurrentUserService currentUser) =>
        new(CreateRepository(context), new TrackingCacheService(), new StubFileStorageFactory(new TrackingFileStorage()), currentUser);

    private static async Task<BlogDbContext> CreateContextAsync()
    {
        var context = new BlogDbContext(CreateOptions());
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static DbContextOptions<BlogDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    private static IReadWriteRepository<IBlogEntity> CreateRepository(BlogDbContext context) =>
        new DefaultRepository<BlogDbContext, IBlogEntity>(
            new DefaultReadRepository<BlogDbContext, IBlogEntity>(context),
            new DefaultWriteRepository<BlogDbContext, IBlogEntity>(context));

    private static PersonBlog CreateBlog() => PersonBlog.CreateBlog(
        BlogId,
        DateTimeOffset.UtcNow,
        "Test blog",
        null,
        null,
        OwnerUserId).Value;

    private static Post CreateVideoPost(
        PostVisibility visibility,
        ProcessState processState,
        Guid? postId = null) => new(
            postId ?? PostId,
            BlogId,
            PostType.Video,
            "description",
            "title",
            null,
            visibility,
            [],
            null)
        {
            ProcessState = processState
        };

    private static UserModel OwnerUser() => new(OwnerUserId, "owner", null, BlogId, []);
    private static UserModel ForeignUser() => new(Guid.NewGuid(), "foreign", null, Guid.NewGuid(), []);

    private static readonly Guid BlogId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid OwnerUserId = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private static readonly Guid PostId = Guid.Parse("30000000-0000-0000-0000-000000000003");

    private sealed class MutableCurrentUserService(UserModel user) : ICurrentUserService
    {
        public UserModel User { get; set; } = user;
        public Task<UserModel> GetCurrentUserAsync() => Task.FromResult(User);
    }

    private sealed class TrackingCacheService : ICacheService
    {
        public List<string> RemovedKeys { get; } = [];
        public Task<T?> GetCachedDataAsync<T>(ICacheKey key) => Task.FromResult(default(T));
        public Task<IEnumerable<T>> GetCachedDataAsync<T>(IEnumerable<ICacheKey> keys) => Task.FromResult<IEnumerable<T>>([]);
        public Task SetCachedDataAsync<T>(ICacheKey key, T data, TimeSpan ttl) where T : notnull => Task.CompletedTask;
        public Task RemoveCachedDataAsync(ICacheKey key)
        {
            RemovedKeys.Add(key.GetKey());
            return Task.CompletedTask;
        }
    }

    private sealed class StubFileStorageFactory(TrackingFileStorage storage) : IFileStorageFactory
    {
        public IFileStorage CreateFileStorage() => storage;
    }

    private sealed class TrackingFileStorage : IFileStorage
    {
        public List<string> UploadedObjects { get; } = [];
        public List<string> RemovedObjects { get; } = [];
        public void Dispose() { }
        public Task<string> PutFileAsync(Guid bucketId, string objectName, Stream input)
        {
            UploadedObjects.Add(objectName);
            return Task.FromResult(objectName);
        }
        public Task<string> GetFileUrlAsync(Guid bucketId, string objectName) => Task.FromResult(objectName);
        public Task RemoveFileAsync(Guid bucketId, string objectName)
        {
            RemovedObjects.Add(objectName);
            return Task.CompletedTask;
        }
        public Task ReadFileAsync(Guid bucketId, string objectName, Stream output, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveBucketAsync(string bucketId) => Task.CompletedTask;
        public Task CreateTempBucketAsync(Guid bucketId) => Task.CompletedTask;
        public Task<string> PutFileChunkAsync(Guid bucketId, Guid id, Stream input, ChunkUploadingInfo options) => Task.FromResult(id.ToString());
        public Task<long> ReadFileByChunksAsync(Guid bucketId, string objectName, long offset, long length, Stream output) => Task.FromResult(0L);
        public async IAsyncEnumerable<(string Objectname, IReadOnlyDictionary<string, string> Headers)> GetAllBucketObjects(Guid bucketId, ChunkUploadingInfo options)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class PassThroughImageConvertService : FileStorage.Service.IImageConvertService
    {
        public Task<Result<FileMetadataModel>> ConvertImageToPngAsync(FileMetadataModel image, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<FileMetadataModel>.Success(image));
    }

    private sealed class StubMessagePublisher(bool shouldFail = false) : IMessagePublish
    {
        public List<object> PublishedMessages { get; } = [];
        public IRequestClient Value => throw new NotSupportedException();
        public Task PublishAsync<T>(BaseEvent<T> message, MessageProperty? cfg = null)
        {
            if (shouldFail)
                throw new InvalidOperationException("publish failed");

            PublishedMessages.Add(message);
            return Task.CompletedTask;
        }
        public Task PublishAsync(BaseEvent message, MessageProperty? cfg = null)
        {
            if (shouldFail)
                throw new InvalidOperationException("publish failed");

            PublishedMessages.Add(message);
            return Task.CompletedTask;
        }
    }
}
