using Gateway.API.Api;
using Gateway.API.Models.Recommendation;
using Gateway.API.Services;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Controllers;

[ApiController]
public sealed class RecommendationController(
    ILogger<RecommendationController> logger,
    RecommendationFeedGateway feedGateway) : BaseApiController(logger)
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
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            () => feedGateway.GetFeedAsync(limit, cursor, currentPostId, cancellationToken),
            response => Ok(response),
            cancellationToken);

    private async Task<IActionResult> ExecuteAsync(
        Func<Task<RecommendationFeedResponse>> action,
        Func<RecommendationFeedResponse, IActionResult> onSuccess,
        CancellationToken cancellationToken)
    {
        try
        {
            return onSuccess(await action());
        }
        catch (DownstreamApiException exception)
        {
            logger.LogWarning(
                "{DownstreamService} rejected recommendation feed request with status {StatusCode}",
                exception.ServiceName,
                (int)exception.StatusCode);

            if (!string.IsNullOrWhiteSpace(exception.ResponseBody))
            {
                return new ContentResult
                {
                    StatusCode = (int)exception.StatusCode,
                    Content = exception.ResponseBody,
                    ContentType = exception.ContentType ?? "application/problem+json"
                };
            }

            return Problem(
                statusCode: (int)exception.StatusCode,
                title: $"{exception.ServiceName} request failed");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Recommendation feed request timed out");
            return Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "Recommendation feed request timed out");
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Recommendation feed downstream service is unavailable");
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Recommendation feed service is unavailable");
        }
        catch (InvalidDataException exception)
        {
            logger.LogError(exception, "Recommendation returned an invalid response");
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Recommendation returned an invalid response");
        }
    }
}
