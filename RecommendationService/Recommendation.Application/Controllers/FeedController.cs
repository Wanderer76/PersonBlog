using Microsoft.AspNetCore.Mvc;
using Recommendation.Application.Services;
using Recommendation.Services.Abstractions;
using Recommendation.Services.Models;

namespace Recommendation.Application.Controllers;

[ApiController]
public sealed class FeedController(
    IRecommendationFeedService feedService,
    IRecommendationSubjectResolver subjectResolver) : ControllerBase
{
    [HttpGet("api/v1/feed")]
    [ProducesResponseType<RecommendationFeedResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecommendationFeedResponse>> GetFeed(
        [FromQuery] int limit = 20,
        [FromQuery] string? cursor = null,
        [FromQuery] Guid? currentPostId = null,
        CancellationToken cancellationToken = default)
    {
        if (!subjectResolver.TryResolve(HttpContext, out var subject, out var error))
            return BadRequest(CreateProblem(error!));

        try
        {
            return Ok(await feedService.GetFeedAsync(new RecommendationFeedRequest(
                subject.UserId,
                subject.AnonymousSessionId,
                limit,
                cursor,
                currentPostId), cancellationToken));
        }
        catch (InvalidRecommendationCursorException exception)
        {
            return BadRequest(CreateProblem(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(CreateProblem(exception.Message));
        }
    }

    private static ProblemDetails CreateProblem(string detail) => new()
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Invalid recommendation feed request",
        Detail = detail
    };
}
