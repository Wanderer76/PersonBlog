using Gateway.API.Api;
using Gateway.API.Models.TextPost;
using System.Net;
using System.Net.Http.Json;

namespace GatewayApiTests;

public sealed class TextPostDetailApiClientTests
{
    [Fact]
    public async Task GetDetailAsync_ForwardsPostIdAndReadsResponse()
    {
        var postId = Guid.NewGuid();
        var expected = CreateDetail(postId);
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(handler);

        var actual = await client.GetDetailAsync(postId, CancellationToken.None);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Title, actual.Title);
        Assert.Equal(expected.Text, actual.Text);
        Assert.Equal(expected.Author.BlogId, actual.Author.BlogId);
        Assert.Single(actual.Categories);
        Assert.Single(actual.Media);
        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal($"http://blog/api/TextPost/detail/{postId}", handler.RequestUri?.ToString());
    }

    [Fact]
    public async Task RegisterViewAsync_ForwardsPostId()
    {
        var postId = Guid.NewGuid();
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(handler);

        await client.RegisterViewAsync(postId, CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal($"http://blog/api/Post/setView/{postId}", handler.RequestUri?.ToString());
    }

    private static TextPostDetailApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://blog/api/")
        });

    private static TextPostDetailResponse CreateDetail(Guid postId) =>
        new(
            postId,
            "Title",
            "Lead",
            "Text",
            DateTimeOffset.UtcNow,
            5,
            10,
            3,
            1,
            [new TextPostCategory(1, "Development")],
            [new TextPostMedia(Guid.NewGuid(), "image.png", "/image.png", "image/png", 100)],
            new TextPostAuthor(Guid.NewGuid(), "Author", "Description", null, 12, 4, 1000),
            new TextPostViewerState(false, null, false, false, false));

    private sealed class CapturingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            return Task.FromResult(response);
        }
    }
}
