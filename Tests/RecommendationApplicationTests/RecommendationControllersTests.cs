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
    public async Task FeedController_ReturnsBadRequestWhenSubjectIsMissing()
    {
        var controller = new FeedController(
            new FeedService(null!),
            new SubjectResolver(null))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.GetFeed();

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(unauthorized.Value);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.Status);
    }

    [Fact]
    public async Task SubjectResolver_UsesCurrentUserService()
    {
        var userId = Guid.NewGuid();
        var currentUser = new CurrentUserService(new UserModel(userId, "user", null, Guid.Empty, []));

        var subject = await new RecommendationSubjectResolver(currentUser).ResolveAsync();

        Assert.NotNull(subject);
        Assert.Equal(userId, subject.UserId);
    }

    [Fact]
    public async Task SubjectResolver_ReturnsNullForAnonymousCurrentUser()
    {
        var currentUser = new CurrentUserService(UserModel.AnonymousUser());

        Assert.Null(await new RecommendationSubjectResolver(currentUser).ResolveAsync());
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
