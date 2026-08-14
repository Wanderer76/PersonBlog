using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Recommendation.Application.Controllers;
using Recommendation.Application.Services;
using Recommendation.Services.Abstractions;
using Recommendation.Services.Models;

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
        var controller = new FeedController(
            feedService,
            new SubjectResolver(new RecommendationSubject(null, "session")))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.GetFeed(20, null, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, ok.Value);
        Assert.Equal("session", feedService.LastRequest!.AnonymousSessionId);
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

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public void SubjectResolver_UsesAnonymousSessionHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RecommendationSubjectResolver.AnonymousSessionHeader] = "session-42";

        var resolved = new RecommendationSubjectResolver().TryResolve(
            context, out var subject, out var error);

        Assert.True(resolved, error);
        Assert.Equal("session-42", subject.AnonymousSessionId);
        Assert.Null(subject.UserId);
    }

    [Fact]
    public void SubjectResolver_PrefersAuthenticatedUserClaim()
    {
        var userId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                "Bearer"))
        };
        context.Request.Headers[RecommendationSubjectResolver.AnonymousSessionHeader] = "ignored-session";

        var resolved = new RecommendationSubjectResolver().TryResolve(
            context, out var subject, out var error);

        Assert.True(resolved, error);
        Assert.Equal(userId, subject.UserId);
        Assert.Null(subject.AnonymousSessionId);
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
        public bool TryResolve(
            HttpContext httpContext,
            out RecommendationSubject resolvedSubject,
            out string? error)
        {
            resolvedSubject = subject!;
            error = subject is null ? "Subject is missing." : null;
            return subject is not null;
        }
    }
}
