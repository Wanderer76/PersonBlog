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
        var subject = await subjectResolver.ResolveAsync(cancellationToken);
        if (subject is null)
            throw new InvalidOperationException("A recommendation subject could not be resolved.");

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

    private static ProblemDetails CreateProblem(
        string detail,
        int status = StatusCodes.Status400BadRequest,
        string title = "Invalid recommendation feed request") => new()
    {
        Status = status,
        Title = title,
        Detail = detail
    };
}
