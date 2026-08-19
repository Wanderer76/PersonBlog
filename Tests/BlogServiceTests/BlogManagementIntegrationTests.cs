using Blog.Contracts.Models;
using Blog.Contracts.Models.Blog;
using Blog.Domain.Entities;
using Blog.Persistence;
using Blog.Service.Services.Implementation;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;

namespace BlogServiceTests;

public sealed class BlogManagementIntegrationTests
{
    [Fact]
    public async Task Owner_can_update_blog_and_replace_photo()
    {
        await using var context = await CreateContextAsync();
        context.Add(CreateBlog("old-photo.png"));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var storage = new TrackingFileStorage();
        var service = CreateBlogService(context, OwnerUser(), storage);
        await using var photoStream = new MemoryStream([1, 2, 3]);
        var photo = new FormFile(photoStream, 0, photoStream.Length, "PhotoUrl", "new-photo.png");

        var result = await service.UpdateBlogAsync(new BlogEditRequest
        {
            Id = BlogId,
            Title = "Updated blog",
            Description = "Updated description",
            PhotoUrl = photo
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated blog", result.Value.Name);
        Assert.Contains("old-photo.png", storage.RemovedObjects);

        context.ChangeTracker.Clear();
        var blog = await context.Blogs.SingleAsync();
        Assert.Equal("Updated blog", blog.Title);
        Assert.NotEqual("old-photo.png", blog.PhotoUrl);
    }

    [Fact]
    public async Task Foreign_user_cannot_update_blog()
    {
        await using var context = await CreateContextAsync();
        context.Add(CreateBlog());
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateBlogService(context, ForeignUser(), new TrackingFileStorage());
        var result = await service.UpdateBlogAsync(new BlogEditRequest
        {
            Id = BlogId,
            Title = "Hacked"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Key == "Forbidden");
        context.ChangeTracker.Clear();
        Assert.Equal("Test blog", (await context.Blogs.SingleAsync()).Title);
    }

    [Fact]
    public async Task Foreign_user_cannot_create_update_or_delete_subscription_levels()
    {
        await using var context = await CreateContextAsync();
        var level = CreateLevel();
        context.AddRange(CreateBlog(), level);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateSubscriptionService(context, ForeignUser());
        var createResult = await service.CreateSubscriptionAsync(new SubscriptionCreateDto
        {
            BlogId = BlogId,
            Title = "Foreign create",
            Price = 10
        });
        var updateResult = await service.UpdateSubscriptionAsync(new SubscriptionUpdateDto
        {
            Id = level.Id,
            BlogId = BlogId,
            Title = "Foreign update",
            Price = 20
        });
        var deleteResult = await service.DeleteSubscriptionAsync(level.Id);

        Assert.Contains(createResult.Errors, error => error.Key == "Forbidden");
        Assert.Contains(updateResult.Errors, error => error.Key == "Forbidden");
        Assert.Contains(deleteResult.Errors, error => error.Key == "Forbidden");
    }

    [Fact]
    public async Task Owner_can_complete_subscription_level_crud_and_links_are_repaired()
    {
        await using var context = await CreateContextAsync();
        context.Add(CreateBlog());
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = CreateSubscriptionService(context, OwnerUser());

        var firstResult = await service.CreateSubscriptionAsync(new SubscriptionCreateDto
        {
            BlogId = BlogId,
            Title = "Basic",
            Price = 100
        });
        Assert.True(firstResult.IsSuccess);
        context.ChangeTracker.Clear();

        var secondResult = await service.CreateSubscriptionAsync(new SubscriptionCreateDto
        {
            BlogId = BlogId,
            PreviousLevelId = firstResult.Value.Id,
            Title = "Premium",
            Price = 200
        });
        Assert.True(secondResult.IsSuccess);
        context.ChangeTracker.Clear();

        var firstAfterCreate = await context.PaymentSubscriptions.SingleAsync(x => x.Id == firstResult.Value.Id);
        Assert.Equal(secondResult.Value.Id, firstAfterCreate.NextLevelId);
        context.ChangeTracker.Clear();

        var getResult = await service.GetSubscriptionByIdAsync(secondResult.Value.Id);
        Assert.True(getResult.IsSuccess);
        Assert.Equal("Premium", getResult.Value.Title);

        var updateResult = await service.UpdateSubscriptionAsync(new SubscriptionUpdateDto
        {
            Id = secondResult.Value.Id,
            BlogId = BlogId,
            PreviousLevelId = firstResult.Value.Id,
            Title = "Premium Plus",
            Description = "Updated",
            Price = 250
        });
        Assert.True(updateResult.IsSuccess);
        Assert.Equal("Premium Plus", updateResult.Value.Title);
        context.ChangeTracker.Clear();

        var deleteResult = await service.DeleteSubscriptionAsync(secondResult.Value.Id);
        Assert.True(deleteResult.IsSuccess);
        context.ChangeTracker.Clear();

        var deleted = await context.PaymentSubscriptions.SingleAsync(x => x.Id == secondResult.Value.Id);
        var firstAfterDelete = await context.PaymentSubscriptions.SingleAsync(x => x.Id == firstResult.Value.Id);
        Assert.True(deleted.IsDeleted);
        Assert.Null(firstAfterDelete.NextLevelId);
        Assert.True((await service.GetSubscriptionByIdAsync(secondResult.Value.Id)).IsFailure);
        Assert.Single(await service.GetAllSubscriptionsByBlogIdAsync(BlogId));
    }

    private static async Task<BlogDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new BlogDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static DefaultBlogService CreateBlogService(
        BlogDbContext context,
        UserModel user,
        TrackingFileStorage storage) =>
        new(CreateRepository(context), new NoOpCacheService(), new StubFileStorageFactory(storage), new StubCurrentUserService(user));

    private static DefaultSubscriptionLevelService CreateSubscriptionService(BlogDbContext context, UserModel user) =>
        new(CreateRepository(context), new NoOpCacheService(), new StubCurrentUserService(user));

    private static IReadWriteRepository<IBlogEntity> CreateRepository(BlogDbContext context) =>
        new DefaultRepository<BlogDbContext, IBlogEntity>(
            new DefaultReadRepository<BlogDbContext, IBlogEntity>(context),
            new DefaultWriteRepository<BlogDbContext, IBlogEntity>(context));

    private static PersonBlog CreateBlog(string? photo = null) => PersonBlog.CreateBlog(
        BlogId,
        DateTimeOffset.UtcNow,
        "Test blog",
        "Description",
        photo,
        OwnerUserId).Value;

    private static PaymentSubscription CreateLevel() =>
        new(Guid.NewGuid(), BlogId, "Basic", null, 100, null, null);

    private static UserModel OwnerUser() => new(OwnerUserId, "owner", null, BlogId, []);
    private static UserModel ForeignUser() => new(Guid.NewGuid(), "foreign", null, Guid.NewGuid(), []);

    private static readonly Guid BlogId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid OwnerUserId = Guid.Parse("20000000-0000-0000-0000-000000000002");

    private sealed class StubCurrentUserService(UserModel user) : ICurrentUserService
    {
        public Task<UserModel> GetCurrentUserAsync() => Task.FromResult(user);
    }

    private sealed class NoOpCacheService : ICacheService
    {
        public Task<T?> GetCachedDataAsync<T>(ICacheKey key) => Task.FromResult(default(T));
        public Task<IEnumerable<T>> GetCachedDataAsync<T>(IEnumerable<ICacheKey> keys) => Task.FromResult<IEnumerable<T>>([]);
        public Task SetCachedDataAsync<T>(ICacheKey key, T data, TimeSpan ttl) where T : notnull => Task.CompletedTask;
        public Task RemoveCachedDataAsync(ICacheKey key) => Task.CompletedTask;
    }

    private sealed class StubFileStorageFactory(TrackingFileStorage storage) : IFileStorageFactory
    {
        public IFileStorage CreateFileStorage() => storage;
    }

    private sealed class TrackingFileStorage : IFileStorage
    {
        public List<string> RemovedObjects { get; } = [];
        public void Dispose() { }
        public Task<string> PutFileAsync(Guid bucketId, string objectName, Stream input) => Task.FromResult(objectName);
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
}
