using Comments.Domain.Models;
using Comments.Domain.Services;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comments.API.Controllers;

public class CommentsController : BaseController
{
    private readonly ICommentService _commentService;
    public CommentsController(ILogger<CommentsController> logger, ICommentService commentService) : base(logger)
    {
        _commentService = commentService;
    }


    [HttpGet("list")]
    public async Task<IActionResult> GetPostCommentsList(Guid postId)
    {
        var result = await _commentService.GetCommentsListByPostAsync(postId);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> CreateComment(CommentCreateRequest createRequest)
    {
        var result = await _commentService.CreateCommentAsync(createRequest);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }
}
