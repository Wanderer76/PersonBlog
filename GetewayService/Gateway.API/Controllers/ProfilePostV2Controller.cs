using Authentication.Contract.Constants;
using Blog.Contracts;
using Blog.Contracts.Models.Post;
using Blog.Contracts.Services;
using Blog.Domain.Entities;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;

namespace Gateway.API.Controllers;

public sealed class ProfilePostV2Controller(
    ILogger<BaseApiController> logger,
    PostApiClient postApiClient) : BaseApiController(logger)
{
    [HttpGet("my")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<PagedListViewModel<UserPostInfoModel>>> GetCurrentUserPosts(
        int page,
        int pageSize,
        PostType postType = PostType.Video)
    {
        return Ok(await postApiClient.GetCurrentUserPostsAsync(page, pageSize, postType));
    }

    [HttpGet("create")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<CreatePostModelViewModel>> GetPostCreateModel()
    {
        return Ok(await postApiClient.GetPostCreateModelAsync());
    }

    [HttpPost("create")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<UserPostInfoModel>> CreatePost([FromForm] VideoPostCreateRequest request)
    {
        return ToActionResult(await postApiClient.CreatePostAsync(request));
    }

    [HttpPost("remove/{postId:guid}")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult> RemovePost(Guid postId)
    {
        return ToActionResult(await postApiClient.RemovePostAsync(postId));
    }

    [HttpGet("edit/{postId:guid}")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<PostEditViewModel>> GetPostEditModel(Guid postId)
    {
        return ToActionResult(await postApiClient.GetPostEditModelAsync(postId));
    }

    [HttpPost("edit")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult> UpdatePost([FromForm] PostEditDto request)
    {
        return ToActionResult(await postApiClient.UpdatePostAsync(request));
    }
}
