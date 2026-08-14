using Blog.Contracts;
using Blog.Contracts.Models.Blog;
using System.Net;

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
}
