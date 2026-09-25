using Gateway.API.Models.Recommendation;
using Gateway.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Controllers;

[ApiController]
public sealed class RecommendationController(
    ILogger<RecommendationController> logger,
    RecommendationFeedGateway feedGateway) : GatewayApiController(logger)
{
    [HttpGet("/api/v1/feed")]
    [ProducesResponseType<RecommendationFeedResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public Task<IActionResult> GetFeed(
        [FromQuery] int limit = 20,
        [FromQuery] string? cursor = null,
        [FromQuery] Guid? currentPostId = null,
        [FromQuery] RecommendationPostType postType = RecommendationPostType.Video,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            () => feedGateway.GetFeedAsync(limit, cursor, currentPostId, postType, cancellationToken),
            response => Ok(response),
            cancellationToken,
            "Recommendation feed request");
}
