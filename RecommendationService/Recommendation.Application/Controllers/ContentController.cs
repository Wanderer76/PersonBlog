using Microsoft.AspNetCore.Mvc;
using Recommendation.Application.Services;
using Recommendation.Services.Abstractions;
using Recommendation.Services.Models;

namespace Recommendation.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ContentController(
    IRecommendationFeedService feedService,
    IRecommendationCatalogService catalogService,
    IRecommendationSubjectResolver subjectResolver) : ControllerBase
{
    [HttpGet("recommendations")]
    [Obsolete("Use GET /api/v1/feed with cursor pagination.")]
    public async Task<ActionResult<RecommendationFeedResponse>> GetRecommendations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? currentPostId = null,
        CancellationToken cancellationToken = default)
    {
        var subject = await subjectResolver.ResolveAsync(cancellationToken);
        if (subject is null)
            throw new InvalidOperationException("A recommendation subject could not be resolved.");
        if (page < 1) return BadRequest(new ProblemDetails { Title = "Page must be positive." });

        int offset;
        try
        {
            offset = checked((page - 1) * pageSize);
            var response = await feedService.GetFeedAsync(new RecommendationFeedRequest(
                subject.UserId,
                subject.AnonymousSessionId,
                pageSize,
                null,
                currentPostId,
                offset), cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid recommendation request", Detail = exception.Message });
        }
        catch (OverflowException)
        {
            return BadRequest(new ProblemDetails { Title = "Requested page is too large." });
        }
    }

    [HttpPost("postListByIds")]
    [Obsolete("Move this hydration operation to Blog Service bulk API.")]
    public async Task<ActionResult<IReadOnlyList<RecommendationPostSummary>>> GetPostsByIds(
        [FromBody] PostListByIdsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await catalogService.GetPostsByIdsAsync(request.PostIds, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid post list", Detail = exception.Message });
        }
    }
}

public sealed record PostListByIdsRequest(IReadOnlyList<Guid> PostIds);
