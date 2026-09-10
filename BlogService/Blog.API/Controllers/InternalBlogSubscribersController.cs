using Blog.Contracts.Models.Blog;
using Blog.Contracts.Services;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Blog.API.Controllers;

[ApiController]
[Route("api/internal/blogs")]
public sealed class InternalBlogSubscribersController(ISubscriberDirectoryService subscribers) : ControllerBase
{
    [HttpGet("{blogId:guid}/subscribers")]
    [ProducesResponseType<SubscriberPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SubscriberPage>> GetSubscribers(
        Guid blogId,
        string? cursor,
        int limit = 500,
        DateTimeOffset? cutoff = null,
        CancellationToken cancellationToken = default)
    {
        if (cutoff is null)
            return BadRequest(new[] { new Shared.Utils.Error(nameof(cutoff), "Cutoff is required.") }
                .ToValidationProblem());

        var result = await subscribers.GetPageAsync(blogId, cursor, limit, cutoff.Value, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Errors.ToValidationProblem());
    }
}
