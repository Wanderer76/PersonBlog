using Blog.Contracts;
using Blog.Contracts.Models;
using Blog.Contracts.Services;
using Blog.Domain.Entities;
using Gateway.API.Controllers;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Models;
using Shared.Services;
using System.Net;
using System.Net.Http.Json;

namespace GatewayApiTests;

public sealed class PostApiClientTests
{
    [Fact]
    public async Task CreateTextPostAsync_ReadsCreatedPostFromResponse()
    {
        var postId = Guid.NewGuid();
        var client = CreateClient(new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new UserPostInfoModel
            {
                Id = postId,
                BlogId = Guid.NewGuid(),
                Title = "Text post",
                Visibility = PostVisibility.Public,
                CreatedAt = DateTimeOffset.UtcNow
            })
        }));

        var result = await client.CreateTextPostAsync(new TextPostCreateForm
        {
            Title = "Text post",
            Text = "Content",
            Visibility = PostVisibility.Public
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(postId, result.Value.Id);
    }

    [Fact]
    public async Task GetUserInfo_IgnoresCallerSuppliedIdentity()
    {
        var attackerUserId = Guid.NewGuid();
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(UserViewInfo.CreateEmpty)
        });
        var controller = new PostController(
            NullLogger<BaseApiController>.Instance,
            CreateClient(handler),
            new StubCurrentUserService(UserModel.AnonymousUser()))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.QueryString = new QueryString($"?userId={attackerUserId}&address=spoofed");

        await controller.GetUserInfo(Guid.NewGuid());

        var query = handler.RequestUri!.Query;
        Assert.DoesNotContain(attackerUserId.ToString(), query);
        Assert.DoesNotContain("spoofed", query);
        Assert.Contains("address=", query);
    }

    [Fact]
    public async Task GetCurrentUserPostCommonModelWithExcludeIdsAsync_SendsPostTypeFilter()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<PostCommonModel>())
        });
        var client = CreateClient(handler);

        await client.GetCurrentUserPostCommonModelWithExcludeIdsAsync([], PostType.Video);

        Assert.Equal("?postType=Video", handler.RequestUri!.Query);
    }

    private static PostApiClient CreateClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("http://blog/api/")
            },
            new StubCacheService());

    private sealed class CapturingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(response);
        }
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
}
