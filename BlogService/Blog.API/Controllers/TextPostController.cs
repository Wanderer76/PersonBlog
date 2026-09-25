using Blog.Contracts.Models.TextPost;
using Blog.Contracts.Services;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace Blog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TextPostController : BaseApiController
{
    private readonly IPostService postService;

    public TextPostController(
        ILogger<TextPostController> logger,
        IPostService postService) : base(logger)
    {
        this.postService = postService;
    }

    [HttpGet("detail/{postId:guid}")]
    [ProducesResponseType<TextPostDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TextPostDetailResponse>> GetDetailAsync(Guid postId)
    {
        var result = await postService.GetTextPostDetailAsync(postId);

        if (result.IsFailure && result.Errors.Any(error => error.Key == "Forbidden"))
        {
            return Forbid();
        }

        return ToActionResult(result);
    }
}
