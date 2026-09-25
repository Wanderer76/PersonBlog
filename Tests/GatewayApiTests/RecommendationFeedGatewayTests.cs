using Gateway.API.Api;
using Gateway.API.Models.Recommendation;
using Gateway.API.Services;
using System.Net;
using System.Text;

namespace GatewayApiTests;

public sealed class RecommendationFeedGatewayTests
{
    [Fact]
    public async Task GetFeedAsync_PreservesRankingOrderAndSkipsMissingCards()
    {
        var firstPostId = Guid.NewGuid();
        var secondPostId = Guid.NewGuid();
        var missingPostId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var creatorUserId = Guid.NewGuid();
        var creatorBlogId = Guid.NewGuid();
        string? recommendationRequestUri = null;

        var recommendationJson = $$"""
            {
              "requestId": "{{requestId}}",
              "algorithmVersion": "heuristic-v1",
              "items": [
                { "postId": "{{firstPostId}}", "reason": "similar_category" },
                { "postId": "{{secondPostId}}", "reason": "subscribed_blog" },
                { "postId": "{{missingPostId}}", "reason": "popular" }
              ],
              "nextCursor": "next+cursor="
            }
            """;
        var blogJson = $$"""
            [
              {
                "id": "{{secondPostId}}",
                "title": "Second",
                "description": null,
                "previewObjectName": "second.jpg",
                "creator": { "userId": "{{creatorUserId}}", "blogId": "{{creatorBlogId}}", "name": "Creator", "avatarUrl": "avatar.jpg" }
              },
              {
                "id": "{{firstPostId}}",
                "title": "First",
                "description": "Description",
                "previewObjectName": "first.jpg",
                "creator": { "userId": "{{creatorUserId}}", "blogId": "{{creatorBlogId}}", "name": "Creator", "avatarUrl": "avatar.jpg" }
              }
            ]
            """;
        var recommendationHandler = new StubHandler(request =>
        {
            recommendationRequestUri = request.RequestUri?.ToString();
            return Json(HttpStatusCode.OK, recommendationJson);
        });
        var recommendationClient = new RecommendationApiClient(new HttpClient(recommendationHandler)
        {
            BaseAddress = new Uri("http://recommendation/api/")
        });
        var blogClient = new BlogFeedApiClient(new HttpClient(
            new StubHandler(_ => Json(HttpStatusCode.OK, blogJson)))
        {
            BaseAddress = new Uri("http://blog/api/")
        });
        var gateway = new RecommendationFeedGateway(recommendationClient, blogClient);

        var result = await gateway.GetFeedAsync(
            20,
            "previous+cursor=",
            firstPostId,
            RecommendationPostType.Video,
            CancellationToken.None);

        Assert.Equal(requestId, result.RequestId);
        Assert.Equal("heuristic-v1", result.AlgorithmVersion);
        Assert.Equal("next+cursor=", result.NextCursor);
        Assert.Collection(
            result.Items,
            first =>
            {
                Assert.Equal(firstPostId, first.PostId);
                Assert.Equal("similar_category", first.Reason);
                Assert.Equal("First", first.Title);
                Assert.Equal(creatorUserId, first.Creator.UserId);
                Assert.Equal(creatorBlogId, first.Creator.BlogId);
                Assert.Equal("Creator", first.Creator.Name);
                Assert.Equal("avatar.jpg", first.Creator.AvatarUrl);
            },
            second =>
            {
                Assert.Equal(secondPostId, second.PostId);
                Assert.Equal("subscribed_blog", second.Reason);
                Assert.Equal("Second", second.Title);
            });
        Assert.Contains("limit=20", recommendationRequestUri);
        Assert.Contains("postType=Video", recommendationRequestUri);
        Assert.Contains("cursor=previous%2Bcursor%3D", recommendationRequestUri);
        Assert.Contains($"currentPostId={firstPostId}", recommendationRequestUri);
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string body) => new(statusCode)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
