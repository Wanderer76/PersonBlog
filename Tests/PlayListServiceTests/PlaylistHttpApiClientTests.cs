using Infrastructure.Services;
using PlayListService.Contract;
using PlayListService.Services.Models;
using PlayListService.Services.Services;
using Shared.Services;
using System.Net;
using System.Net.Http.Json;

namespace PlayListServiceTests;

public sealed class PlaylistHttpApiClientTests
{
    [Fact]
    public async Task CreatePlayListAsync_SendsMultipartFormDataExpectedByService()
    {
        var thumbnailId = Guid.NewGuid();
        var postIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new PlayListListItem
            {
                Id = Guid.NewGuid(),
                Title = "Road trip"
            })
        });
        var client = CreateClient(handler);

        var result = await client.CreatePlayListAsync(new CreatePlayListRequest
        {
            Title = "Road trip",
            ThumbnailId = thumbnailId,
            PostIds = [.. postIds]
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("multipart/form-data", handler.ContentType);
        Assert.Equal("Road trip", handler.FormValues["Title"].Single());
        Assert.Equal("0", handler.FormValues["ContentType"].Single());
        Assert.Equal("0", handler.FormValues["Kind"].Single());
        Assert.Equal(thumbnailId.ToString(), handler.FormValues["ThumbnailId"].Single());
        Assert.Equal(postIds.Select(x => x.ToString()), handler.FormValues["PostIds"]);
    }

    [Fact]
    public async Task AddVideoAsync_ReadsPlaylistFromDataEnvelope()
    {
        var playlist = new PlayListListItem
        {
            Id = Guid.NewGuid(),
            Title = "Road trip",
            PostCount = 2
        };
        var client = CreateClient(new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { data = playlist })
        }));

        var result = await client.AddVideoAsync(new PlayListItemAddRequest
        {
            PlayListId = playlist.Id,
            PostsToAdd = [Guid.NewGuid()]
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(playlist.Id, result.Value.Id);
        Assert.Equal(playlist.Title, result.Value.Title);
        Assert.Equal(playlist.PostCount, result.Value.PostCount);
    }

    [Fact]
    public async Task ChangePostPositionAsync_ReadsPlaylistFromDataEnvelope()
    {
        var playlist = new PlayListListItem
        {
            Id = Guid.NewGuid(),
            Title = "Road trip",
            PostCount = 3
        };
        var client = CreateClient(new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { data = playlist })
        }));

        var result = await client.ChangePostPositionAsync(new ChangePostPositionRequest
        {
            PlaylistId = playlist.Id,
            PostId = Guid.NewGuid(),
            Destination = 2
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(playlist.Id, result.Value.Id);
        Assert.Equal(playlist.Title, result.Value.Title);
        Assert.Equal(playlist.PostCount, result.Value.PostCount);
    }

    private static PlaylistHttpApiClient CreateClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("http://playlist/api/")
            },
            new StubCacheService());

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        public string? ContentType { get; private set; }
        public Dictionary<string, string[]> FormValues { get; } = new(StringComparer.Ordinal);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ContentType = request.Content?.Headers.ContentType?.MediaType;

            if (request.Content is MultipartFormDataContent multipart)
            {
                foreach (var part in multipart)
                {
                    var name = part.Headers.ContentDisposition?.Name?.Trim('"');
                    if (name is null || part.Headers.ContentDisposition?.FileName is not null)
                        continue;

                    var value = await part.ReadAsStringAsync(cancellationToken);
                    FormValues[name] = FormValues.TryGetValue(name, out var current)
                        ? [.. current, value]
                        : [value];
                }
            }

            return responseFactory(request);
        }
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
}
