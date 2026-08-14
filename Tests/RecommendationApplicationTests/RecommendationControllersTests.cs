using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Services;
using Recommendation.Application.Controllers;
using Recommendation.Application.Services;
using Recommendation.Services.Abstractions;
using Recommendation.Services.Models;
using Shared.Models;

namespace RecommendationApplicationTests;

public sealed class RecommendationControllersTests
{
    [Fact]
    public async Task FeedController_ReturnsFeedForResolvedSubject()
    {
        var response = new RecommendationFeedResponse(
            Guid.NewGuid(),
            "heuristic-v1",
            [new RecommendationFeedItem(Guid.NewGuid(), "fresh")],
            null);
        var feedService = new FeedService(response);
        var userId = Guid.NewGuid();
        var controller = new FeedController(
            feedService,
            new SubjectResolver(new RecommendationSubject(userId)))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.GetFeed(20, null, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, ok.Value);
        Assert.Equal(userId, feedService.LastRequest!.UserId);
        Assert.Null(feedService.LastRequest.AnonymousSessionId);
    }

    [Fact]
    public async Task FeedController_ForwardsAnonymousSubject()
    {
        const string sessionId = "87b61a0d7270441c936da927cb881f3e";
        var feedService = new FeedService(new RecommendationFeedResponse(
            Guid.NewGuid(), "heuristic-v1", [], null));
        var controller = new FeedController(
            feedService,
            new SubjectResolver(new RecommendationSubject(null, sessionId)))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.GetFeed();

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Null(feedService.LastRequest!.UserId);
        Assert.Equal(sessionId, feedService.LastRequest.AnonymousSessionId);
    }

    [Fact]
    public async Task SubjectResolver_UsesCurrentUserService()
    {
        var userId = Guid.NewGuid();
        var currentUser = new CurrentUserService(new UserModel(userId, "user", null, Guid.Empty, []));

        var subject = await new RecommendationSubjectResolver(
            currentUser,
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() }).ResolveAsync();

        Assert.NotNull(subject);
        Assert.Equal(userId, subject.UserId);
    }

    [Fact]
    public async Task SubjectResolver_CreatesStableSessionForAnonymousCurrentUser()
    {
        var currentUser = new CurrentUserService(UserModel.AnonymousUser());
        var context = new DefaultHttpContext();
        var resolver = new RecommendationSubjectResolver(
            currentUser,
            new HttpContextAccessor { HttpContext = context });

        var first = await resolver.ResolveAsync();
        var second = await resolver.ResolveAsync();

        Assert.NotNull(first);
        Assert.Null(first.UserId);
        Assert.Equal(first.AnonymousSessionId, second!.AnonymousSessionId);
        Assert.True(Guid.TryParseExact(first.AnonymousSessionId, "N", out _));
        Assert.Contains("AnonymousSessionId=", context.Response.Headers.SetCookie.ToString());
    }

    private sealed class FeedService(RecommendationFeedResponse response) : IRecommendationFeedService
    {
        public RecommendationFeedRequest? LastRequest { get; private set; }

        public Task<RecommendationFeedResponse> GetFeedAsync(
            RecommendationFeedRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(response);
        }
    }

    private sealed class SubjectResolver(RecommendationSubject? subject) : IRecommendationSubjectResolver
    {
        public Task<RecommendationSubject?> ResolveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(subject);
    }

    private sealed class CurrentUserService(UserModel user) : ICurrentUserService
    {
        public Task<UserModel> GetCurrentUserAsync() => Task.FromResult(user);
    }
}
