using Authentication.Contract.Constants;
using Blog.Contracts.Models;
using Blog.Contracts.Models.Post;
using Blog.Contracts.Services;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Services;

namespace Blog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostController : BaseApiController
{
    private readonly IPostService _postService;
    private readonly IUserPostService _userPostService;
    public PostController(
        ILogger<PostController> logger,
        IPostService postService,
        IUserPostService userPostService)
        : base(logger)
    {
        _postService = postService;
        _userPostService = userPostService;
    }

    [HttpGet("detail/{postId:guid}")]
    [Produces(typeof(PostDetailViewModel))]
    public async Task<ActionResult<PostDetailViewModel>> GetDetailPostByIdAsync(Guid postId)
    {
        var result = await _postService.GetDetailPostByIdAsync(postId);
        return Ok(result);
    }

    [HttpGet("video-access/{blogId:guid}/{postId:guid}")]
    public async Task<IActionResult> CheckVideoAccessAsync(Guid blogId, Guid postId)
    {
        return await _postService.CanAccessVideoAsync(blogId, postId)
            ? NoContent()
            : NotFound();
    }

    [HttpGet("userInfo/{postId:guid}")]
    [Produces(typeof(UserViewInfo))]
    public async Task<ActionResult<UserViewInfo>> GetDetailPostByIdAsync(Guid postId, Guid? userId, string? address)
    {
        var result = await _userPostService.GetUserViewPostInfoAsync(postId, userId, address);
        return Ok(result);
    }

    [HttpPost("setReaction/{postId:guid}")]
    public async Task<ActionResult> SetReactionToVideo(Guid postId, bool? isLike)
    {
        HttpContext.TryGetUserFromContext(out var userId);
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _postService.SetReactionToPost(new ReactionCreateModel
        {
            IsLike = isLike,
            PostId = postId,
            RemoteIp = remoteIp,
            UserId = userId
        });
        return Ok();
    }


    [HttpDelete("delete/{id:guid}")]
    [Authorize]
    public async Task<ActionResult> DeletePost(Guid id)
    {
        await _postService.RemovePostByIdAsync(id);
        return Ok();
    }

    [HttpPost("commonByIds")]
    [Produces(typeof(IReadOnlyList<PostCommonModel>))]
    public async Task<ActionResult<IReadOnlyList<PostCommonModel>>> GetPostCommonModel([FromBody] List<Guid> ids)
    {
        return Ok(await _postService.GetPostCommonModelAsync(ids));
    }

    [HttpPost("commonWithExcludeIds")]
    [Produces(typeof(IReadOnlyList<PostCommonModel>))]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<IReadOnlyList<PostCommonModel>>> GetPostCommonModelByExcludeIds([FromBody] List<Guid> excludeIds)
    {
        return Ok(await _postService.GetPostCommonModelWithExcludeIdsAsync(excludeIds));
    }

    [HttpGet("my/list")]
    [Produces(typeof(PostCommonModel))]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<IReadOnlyList<PostCommonModel>>> GetCurrentUserPostListAsync()
    {
        return Ok(await _postService.GetCurrentUserPostListAsync());
    }
}
