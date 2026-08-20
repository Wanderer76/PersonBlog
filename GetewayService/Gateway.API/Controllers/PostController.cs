using Authentication.Contract.Constants;
using Blog.Contracts;
using Blog.Contracts.Models;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Gateway.API.Controllers;

public sealed class PostController(
    ILogger<BaseApiController> logger,
    PostApiClient postApiClient,
    ICurrentUserService currentUserService) : BaseApiController(logger)
{
    [HttpGet("detail/{postId:guid}")]
    public async Task<ActionResult<PostDetailViewModel>> GetDetail(Guid postId)
    {
        return ToActionResult(await postApiClient.GetPostDetailAsync(postId));
    }

    [HttpGet("video-access/{blogId:guid}/{postId:guid}")]
    public async Task<IActionResult> CheckVideoAccess(Guid blogId, Guid postId)
    {
        var status = await postApiClient.CheckVideoAccessAsync(blogId, postId);
        return status switch
        {
            HttpStatusCode.NoContent => NoContent(),
            HttpStatusCode.NotFound => NotFound(),
            HttpStatusCode.Forbidden => Forbid(),
            _ => StatusCode((int)status)
        };
    }

    [HttpGet("userInfo/{postId:guid}")]
    public async Task<ActionResult<UserViewInfo>> GetUserInfo(Guid postId)
    {
        var user = await currentUserService.GetCurrentUserAsync();
        Guid? userId = user.IsAnonymous ? null : user.UserId;
        var address = user.IsAnonymous
            ? AnonymousSession.GetOrCreate(HttpContext)
            : null;

        return ToActionResult(await postApiClient.GetUserViewInfoAsync(postId, userId, address));
    }

    [HttpPost("setReaction/{postId:guid}")]
    public async Task<ActionResult> SetReaction(Guid postId, bool? isLike)
    {
        return ToActionResult(await postApiClient.SetReactionAsync(postId, isLike));
    }

    [HttpDelete("delete/{postId:guid}")]
    [Authorize]
    public async Task<ActionResult> DeletePost(Guid postId)
    {
        var result = await postApiClient.DeletePostAsync(postId);
        return result.IsFailure && result.Errors.Any(error => error.Key == "Forbidden")
            ? Forbid()
            : ToActionResult(result);
    }

    [HttpPost("commonByIds")]
    public async Task<ActionResult<IReadOnlyList<PostCommonModel>>> GetCommonByIds([FromBody] List<Guid> ids)
    {
        return Ok(await postApiClient.GetPostCommonModelAsync(ids));
    }

    [HttpPost("commonWithExcludeIds")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<IReadOnlyList<PostCommonModel>>> GetCommonWithExcludeIds(
        [FromBody] List<Guid> excludeIds)
    {
        return Ok(await postApiClient.GetCurrentUserPostCommonModelWithExcludeIdsAsync(excludeIds));
    }

    [HttpGet("my/list")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<IReadOnlyList<PostCommonModel>>> GetCurrentUserPosts()
    {
        return ToActionResult(await postApiClient.GetCurrentUserPostListAsync());
    }
}
