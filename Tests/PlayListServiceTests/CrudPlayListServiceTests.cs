using Blog.Contracts;
using Blog.Contracts.Models;
using Infrastructure.Extensions;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlayListService.Domain.Entities;
using PlayListService.Persistence;
using PlayListService.Services;
using PlayListService.Services.Services;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace PlayListServiceTests;

public sealed class CrudPlayListServiceTests
{
    [Fact]
    public async Task AddVideoAsync_ReturnsFailure_WhenPlaylistDoesNotExist()
    {
        await using var fixture = CreateFixture();

        var result = await fixture.Service.AddVideoAsync(new PlayListItemAddRequest
        {
            PlayListId = Guid.NewGuid(),
            PostsToAdd = [Guid.NewGuid()]
        });

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task RemoveVideoAsync_ReturnsFailure_WhenPlaylistDoesNotExist()
    {
        await using var fixture = CreateFixture();

        var result = await fixture.Service.RemoveVideoAsync(new PlayListItemRemoveRequest
        {
            PlayListId = Guid.NewGuid(),
            PostId = Guid.NewGuid()
        });

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ChangePostPositionAsync_ReturnsFailure_WhenPlaylistDoesNotExist()
    {
        await using var fixture = CreateFixture();

        var result = await fixture.Service.ChangePostPositionAsync(new ChangePostPositionRequest
        {
            PlaylistId = Guid.NewGuid(),
            PostId = Guid.NewGuid(),
            Destination = 1
        });

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task RemovePlayListAsync_ReturnsFailure_WhenPlaylistDoesNotExist()
    {
        await using var fixture = CreateFixture();

        var result = await fixture.Service.RemovePlayListAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task RemoveVideoAsync_ReturnsFailure_WhenPostDoesNotExist()
    {
        await using var fixture = CreateFixture();
        var playlist = await fixture.SeedPlaylistAsync([Guid.NewGuid()]);

        var result = await fixture.Service.RemoveVideoAsync(new PlayListItemRemoveRequest
        {
            PlayListId = playlist.Id,
            PostId = Guid.NewGuid()
        });

        Assert.True(result.IsFailure);
        Assert.Single(playlist.PlayListItems);
    }

    [Fact]
    public async Task GetPlayListPostPagedAsync_PreservesStoredPlaylistOrder()
    {
        var firstPostId = Guid.NewGuid();
        var secondPostId = Guid.NewGuid();
        var thirdPostId = Guid.NewGuid();
        var postsReturnedByBlogService = new[]
        {
            CreatePost(thirdPostId),
            CreatePost(firstPostId),
            CreatePost(secondPostId)
        };
        await using var fixture = CreateFixture(new FixedResponseHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(postsReturnedByBlogService)
            }));
        var playlist = await fixture.SeedPlaylistAsync([firstPostId, secondPostId, thirdPostId]);

        var result = await fixture.Service.GetPlayListPostPagedAsync(playlist.Id, page: 1, pageSize: 3);

        Assert.Equal(
            [firstPostId, secondPostId, thirdPostId],
            result.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task CreatePlayListAsync_ReturnsFailure_WhenRequestedPostIsNotAnAvailableVideo()
    {
        var textPostId = Guid.NewGuid();
        await using var fixture = CreateFixture(new FixedResponseHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Array.Empty<PostCommonModel>())
            }));

        var result = await fixture.Service.CreatePlayListAsync(new PlayListService.Services.Models.CreatePlayListRequest
        {
            Title = "My playlist",
            PostIds = [textPostId]
        });

        Assert.True(result.IsFailure);
        Assert.Empty(await fixture.Context.PlayLists.ToListAsync());
    }

    [Fact]
    public async Task CreatePlayListAsync_CreatesPlaylist_WhenAllRequestedPostsAreAvailableVideos()
    {
        var videoPostId = Guid.NewGuid();
        await using var fixture = CreateFixture(new FixedResponseHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[] { CreatePost(videoPostId) })
            }));

        var result = await fixture.Service.CreatePlayListAsync(new PlayListService.Services.Models.CreatePlayListRequest
        {
            Title = "My playlist",
            PostIds = [videoPostId]
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(videoPostId, (await fixture.Context.PlayListItems.SingleAsync()).PostId);
    }

    [Fact]
    public async Task AddVideoAsync_ReturnsFailure_WhenRequestedPostIsNotAnAvailableVideo()
    {
        await using var fixture = CreateFixture(new FixedResponseHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Array.Empty<PostCommonModel>())
            }));
        var playlist = await fixture.SeedPlaylistAsync([Guid.NewGuid()]);

        var result = await fixture.Service.AddVideoAsync(new PlayListItemAddRequest
        {
            PlayListId = playlist.Id,
            PostsToAdd = [Guid.NewGuid()]
        });

        Assert.True(result.IsFailure);
        Assert.Single(await fixture.Context.PlayListItems.ToListAsync());
    }

    private static PostCommonModel CreatePost(Guid id) => new()
    {
        Id = id,
        Title = id.ToString()
    };

    private static ServiceFixture CreateFixture(HttpMessageHandler? postHandler = null)
    {
        var user = new UserModel(Guid.NewGuid(), "playlist-owner", null, Guid.Empty, []);
        var services = new ServiceCollection();
        services.AddDbContext<PlayListDbContext>(options =>
            options
                .UseInMemoryDatabase($"playlist-tests-{Guid.NewGuid()}")
                .ConfigureWarnings(warnings => warnings.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
        services.AddDefaultRepository<PlayListDbContext, IPlayListEntity>();
        services.AddSingleton<ICurrentUserService>(new StubCurrentUserService(user));
        services.AddSingleton<IFileStorageFactory, StubFileStorageFactory>();
        services.AddSingleton<ICacheService, StubCacheService>();
        services.AddScoped(serviceProvider => new PostApiClient(
            new HttpClient(postHandler ?? new FixedResponseHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(Array.Empty<PostCommonModel>())
                }))
            {
                BaseAddress = new Uri("http://blog/api/")
            },
            serviceProvider.GetRequiredService<ICacheService>()));
        services.AddScoped(_ => new BlogApiClient(new HttpClient(new FixedResponseHandler(
            new HttpResponseMessage(HttpStatusCode.NotFound)))
        {
            BaseAddress = new Uri("http://blog/api/")
        }));
        services.AddPlayListService();

        return new ServiceFixture(services.BuildServiceProvider(), user.UserId);
    }

    private sealed class ServiceFixture : IAsyncDisposable
    {
        private readonly ServiceProvider provider;
        private readonly AsyncServiceScope scope;
        private readonly Guid userId;

        public ServiceFixture(ServiceProvider provider, Guid userId)
        {
            this.provider = provider;
            this.userId = userId;
            scope = provider.CreateAsyncScope();
            Service = scope.ServiceProvider.GetRequiredService<IPlayListService>();
            Context = scope.ServiceProvider.GetRequiredService<PlayListDbContext>();
        }

        public IPlayListService Service { get; }
        public PlayListDbContext Context { get; }

        public async Task<PlayList> SeedPlaylistAsync(List<Guid> postIds)
        {
            var playlist = PlayList.Create(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                "My playlist",
                userId,
                thumbnailId: null,
                postIds).Value;
            Context.Add(playlist);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            return playlist;
        }

        public async ValueTask DisposeAsync()
        {
            await scope.DisposeAsync();
            await provider.DisposeAsync();
        }
    }

    private sealed class FixedResponseHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(response);
    }

    private sealed class StubCurrentUserService(UserModel user) : ICurrentUserService
    {
        public Task<UserModel> GetCurrentUserAsync() => Task.FromResult(user);
    }

    private sealed class StubCacheService : ICacheService
    {
        public Task<T?> GetCachedDataAsync<T>(ICacheKey key) => Task.FromResult<T?>(default);

        public Task<IEnumerable<T>> GetCachedDataAsync<T>(IEnumerable<ICacheKey> keys) =>
            Task.FromResult<IEnumerable<T>>([]);

        public Task SetCachedDataAsync<T>(ICacheKey key, T data, TimeSpan ttl) where T : notnull =>
            Task.CompletedTask;

        public Task RemoveCachedDataAsync(ICacheKey key) => Task.CompletedTask;
    }

    private sealed class StubFileStorageFactory : IFileStorageFactory
    {
        public IFileStorage CreateFileStorage() => new StubFileStorage();
    }

    private sealed class StubFileStorage : IFileStorage
    {
        public Task<string> PutFileAsync(Guid bucketId, string objectName, Stream input, CancellationToken cancellationToken = default) =>
            Task.FromResult(objectName);

        public Task ReadFileAsync(Guid bucketId, string objectName, Stream output, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<string> GetFileUrlAsync(Guid bucketId, string objectName, CancellationToken cancellationToken = default) =>
            Task.FromResult(objectName);

        public Task RemoveFileAsync(Guid bucketId, string objectName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveFilesByPrefixAsync(Guid bucketId, string prefix, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveBucketAsync(Guid bucketId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task CreateTempBucketAsync(Guid bucketId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<string> PutFileChunkAsync(
            Guid bucketId,
            Guid id,
            Stream input,
            ChunkUploadingInfo options,
            CancellationToken cancellationToken = default) => Task.FromResult(id.ToString());

        public Task<long> ReadFileByChunksAsync(
            Guid bucketId,
            string objectName,
            long offset,
            long length,
            Stream output,
            CancellationToken cancellationToken = default) => Task.FromResult(length);

        public async IAsyncEnumerable<(string ObjectName, IReadOnlyDictionary<string, string> Headers)> GetAllBucketObjects(
            Guid bucketId,
            ChunkUploadingInfo options,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public void Dispose()
        {
        }
    }
}
