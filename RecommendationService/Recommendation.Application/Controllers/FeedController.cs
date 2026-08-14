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
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RecommendationFeedResponse>> GetFeed(
        [FromQuery] int limit = 20,
        [FromQuery] string? cursor = null,
        [FromQuery] Guid? currentPostId = null,
        CancellationToken cancellationToken = default)
    {
        var subject = await subjectResolver.ResolveAsync(cancellationToken);
        if (subject is null)
            return Unauthorized(CreateProblem(
                "Authenticated user is required.",
                StatusCodes.Status401Unauthorized,
                "Authentication required"));

        try
        {
            return Ok(await feedService.GetFeedAsync(new RecommendationFeedRequest(
                subject.UserId,
                null,
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
