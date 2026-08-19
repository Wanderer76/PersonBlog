using Authentication.Contract.Constants;
using Blog.Contracts.Models;
using Blog.Domain.Entities;
using Blog.Persistence;
using Blog.Service.Services.Implementation;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;

namespace BlogServiceTests;

public sealed class PostAccessIntegrationTests
{
    [Fact]
    public async Task Cached_private_post_is_not_returned_to_anonymous_user()
    {
        await using var context = await CreateContextAsync();
        var blog = CreateBlog();
        var post = CreatePost(PostVisibility.Private);
        context.AddRange(blog, post);
        await context.SaveChangesAsync();

        var currentUser = new MutableCurrentUserService(OwnerUser());
        var service = new DefaultPostService(
            CreateRepository(context),
            new StubFileStorageFactory(),
            new MemoryCacheService(),
            currentUser);

        Assert.NotNull(await service.GetDetailPostByIdAsync(post.Id));

        currentUser.User = UserModel.AnonymousUser();

        Assert.Null(await service.GetDetailPostByIdAsync(post.Id));
    }

    [Fact]
    public async Task Banned_post_is_visible_only_to_owner_or_moderator()
    {
        await using var context = await CreateContextAsync();
        var blog = CreateBlog();
        var post = CreatePost(PostVisibility.Public);
        post.SetPostBanned(new BanMessage(Guid.NewGuid(), post.Id, DateTimeOffset.UtcNow, "blocked"));
        context.AddRange(blog, post);
        await context.SaveChangesAsync();

        var currentUser = new MutableCurrentUserService(UserModel.AnonymousUser());
        var service = new DefaultPostService(
            CreateRepository(context),
            new StubFileStorageFactory(),
            new MemoryCacheService(),
            currentUser);

        Assert.Null(await service.GetDetailPostByIdAsync(post.Id));

        currentUser.User = OwnerUser();
        Assert.NotNull(await service.GetDetailPostByIdAsync(post.Id));

        currentUser.User = new UserModel(Guid.NewGuid(), "moderator", null, Guid.Empty, [Roles.AdminRoleId]);
        Assert.NotNull(await service.GetDetailPostByIdAsync(post.Id));
    }

    [Fact]
    public async Task Public_blog_feed_excludes_non_public_banned_deleted_and_incomplete_posts()
    {
        await using var context = await CreateContextAsync();
        var blog = CreateBlog();
        var publicPost = CreatePost(PostVisibility.Public);
        var privatePost = CreatePost(PostVisibility.Private);
        var byUrlPost = CreatePost(PostVisibility.ByUrl);
        var bannedPost = CreatePost(PostVisibility.Public);
        bannedPost.SetPostBanned(new BanMessage(Guid.NewGuid(), bannedPost.Id, DateTimeOffset.UtcNow, "blocked"));
        var deletedPost = CreatePost(PostVisibility.Public);
        deletedPost.Delete();
        var incompletePost = CreatePost(PostVisibility.Public, ProcessState.Draft);
        context.AddRange(blog, publicPost, privatePost, byUrlPost, bannedPost, deletedPost, incompletePost);
        await context.SaveChangesAsync();

        var service = CreateProfileService(context, UserModel.AnonymousUser());

        var result = await service.GetAvailablePostsByBlogIdAsync(BlogId, 1, 20, PostType.Video);

        var item = Assert.Single(result.Items);
        Assert.Equal(publicPost.Id, item.Id);
        Assert.Equal(1, result.TotalPostsCount);
    }

    [Fact]
    public async Task Owner_blog_feed_keeps_private_by_url_banned_and_incomplete_posts()
    {
        await using var context = await CreateContextAsync();
        var blog = CreateBlog();
        var posts = new[]
        {
            CreatePost(PostVisibility.Public),
            CreatePost(PostVisibility.Private),
            CreatePost(PostVisibility.ByUrl),
            CreatePost(PostVisibility.Public, ProcessState.Draft)
        };
        posts[0].SetPostBanned(new BanMessage(Guid.NewGuid(), posts[0].Id, DateTimeOffset.UtcNow, "blocked"));
        context.Add(blog);
        context.AddRange(posts);
        await context.SaveChangesAsync();

        var service = CreateProfileService(context, OwnerUser());

        var result = await service.GetAvailablePostsByBlogIdAsync(BlogId, 1, 20, PostType.Video);

        Assert.Equal(posts.Length, result.TotalPostsCount);
        Assert.Equal(posts.Select(x => x.Id).Order(), result.Items.Select(x => x.Id).Order());
    }

    private static DefaultProfilePostV2Service CreateProfileService(BlogDbContext context, UserModel user) =>
        new(
            CreateRepository(context),
            new StubFileStorageFactory(),
            new MutableCurrentUserService(user),
            null!,
            null!,
            null!);

    private static async Task<BlogDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new BlogDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static PersonBlog CreateBlog() => PersonBlog.CreateBlog(
        BlogId,
        DateTimeOffset.UtcNow,
        "Test blog",
        null,
        null,
        OwnerUserId).Value;

    private static Post CreatePost(
        PostVisibility visibility,
        ProcessState processState = ProcessState.Complete) => new(
            Guid.NewGuid(),
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

    private static UserModel OwnerUser() =>
        new(OwnerUserId, "owner", null, BlogId, []);

    private static IReadWriteRepository<IBlogEntity> CreateRepository(BlogDbContext context) =>
        new DefaultRepository<BlogDbContext, IBlogEntity>(
            new DefaultReadRepository<BlogDbContext, IBlogEntity>(context),
            new DefaultWriteRepository<BlogDbContext, IBlogEntity>(context));

    private static readonly Guid BlogId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid OwnerUserId = Guid.Parse("10000000-0000-0000-0000-000000000002");

    private sealed class MutableCurrentUserService(UserModel user) : ICurrentUserService
    {
        public UserModel User { get; set; } = user;
        public Task<UserModel> GetCurrentUserAsync() => Task.FromResult(User);
    }

    private sealed class MemoryCacheService : ICacheService
    {
        private readonly Dictionary<string, object> _items = new();

        public Task<T?> GetCachedDataAsync<T>(ICacheKey key) =>
            Task.FromResult(_items.TryGetValue(key.GetKey(), out var value) ? (T?)value : default);

        public Task<IEnumerable<T>> GetCachedDataAsync<T>(IEnumerable<ICacheKey> keys) =>
            Task.FromResult(keys
                .Where(key => _items.ContainsKey(key.GetKey()))
                .Select(key => (T)_items[key.GetKey()]));

        public Task SetCachedDataAsync<T>(ICacheKey key, T data, TimeSpan ttl) where T : notnull
        {
            _items[key.GetKey()] = data;
            return Task.CompletedTask;
        }

        public Task RemoveCachedDataAsync(ICacheKey key)
        {
            _items.Remove(key.GetKey());
            return Task.CompletedTask;
        }
    }

    private sealed class StubFileStorageFactory : IFileStorageFactory
    {
        public IFileStorage CreateFileStorage() => new StubFileStorage();
    }

    private sealed class StubFileStorage : IFileStorage
    {
        public void Dispose() { }
        public Task<string> GetFileUrlAsync(Guid bucketId, string objectName) => Task.FromResult(objectName);
        public Task<string> PutFileAsync(Guid bucketId, string objectName, Stream input) => Task.FromResult(objectName);
        public Task ReadFileAsync(Guid bucketId, string objectName, Stream output, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveFileAsync(Guid bucketId, string objectName) => Task.CompletedTask;
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
