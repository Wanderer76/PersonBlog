using Blog.Contracts;
using Blog.Contracts.Models.Blog;
using System.Net;
using System.Net.Http.Json;

namespace GatewayApiTests;

public sealed class BlogApiClientTests
{
    [Fact]
    public async Task CreateBlogAsync_ConvertsEmptyUnauthorizedResponseToFailure()
    {
        var client = CreateClient(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var result = await client.CreateBlogAsync(new BlogCreateRequest("Blog", null, null));

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(HttpStatusCode.Unauthorized), error.Key);
    }

    [Fact]
    public async Task CreateBlogAsync_ConvertsEmptySuccessfulResponseToFailure()
    {
        var client = CreateClient(new HttpResponseMessage(HttpStatusCode.NoContent));

        var result = await client.CreateBlogAsync(new BlogCreateRequest("Blog", null, null));

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal("EmptyResponse", error.Key);
    }

    [Fact]
    public async Task GetBlogViewerInfoByPostIdAsync_UsesViewerContractAndRoute()
    {
        var postId = Guid.NewGuid();
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                id = Guid.NewGuid(),
                name = "Blog",
                description = "Description",
                createdAt = DateTimeOffset.UtcNow,
                photoUrl = (string?)null,
                hasSubscription = true,
                subscribersCount = 12
            })
        });
        var client = new BlogApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://blog/api/")
        });

        var result = await client.GetBlogViewerInfoByPostIdAsync(postId);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasSubscription);
        Assert.Equal($"http://blog/api/Blog/blogViewerInfoByPost/{postId}", handler.RequestUri?.ToString());
    }

    private static BlogApiClient CreateClient(HttpResponseMessage response)
    {
        var httpClient = new HttpClient(new StubHandler(response))
        {
            BaseAddress = new Uri("http://blog/api/")
        };

        return new BlogApiClient(httpClient);
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }

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
}
