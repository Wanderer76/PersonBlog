using Authentication.Contract.Constants;
using Blog.Contracts.Models;
using Blog.Contracts.Models.Post;
using Blog.Contracts.Services;
using Blog.Domain.Entities;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;

namespace Blog.API.Controllers;

public class ProfilePostV2Controller(
    ILogger<BaseApiController> logger,
    IProfilePostV2Service profilePostService,
    ICurrentUserService currentUserService)
    : BaseApiController(logger)
{
    [HttpGet("my")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<PagedListViewModel<UserPostInfoModel>>> GetCurrentUserPostPaged(int page, int pageSize, PostType postType = PostType.Video)
    {
        var user = await currentUserService.GetCurrentUserAsync();
        var result = await profilePostService.GetCurrentUserPostsAsync(user.BlogId, page, pageSize, postType);
        return Ok(result);
    }

    [HttpGet("create")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<CreatePostModelViewModel>> GetPostCreateModel()
    {
        var model = await profilePostService.GetPostCreateModelAsync();
        return Ok(model);
    }

    [HttpPost("create")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<UserPostInfoModel>> CreatePost([FromForm] PostCreateRequest request)
    {

        var command = MapToCommand(request);
        var result = await profilePostService.CreatePostAsync(command);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpGet("availablePostByBlogId/{blogId:guid}")]
    public async Task<ActionResult<PagedListViewModel<PostCommonModelV2>>> GetAvailablePostPagedByBlogId(Guid blogId, int page, int pageSize, PostType postType = PostType.Video)
    {
        var result = await profilePostService.GetAvailablePostsByBlogIdAsync(blogId, page, pageSize, postType);
        return Ok(result);
    }

    [HttpPost("remove/{postId:guid}")]
    [AuthFilter(Roles.Blogger)]
    public async Task<IActionResult> RemovePost(Guid postId)
    {
        var result = await profilePostService.RemovePostAsync(postId);
        if (result.IsSuccess)
        {
            return Ok();
        }
        else
        {
            return BadRequest(result.Errors);
        }
    }

    [HttpGet("edit/{postId:guid}")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<PostEditViewModel>> EditPostViewModel(Guid postId)
    {
        var result = await profilePostService.GetPostEditViewModelAsync(postId);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        else
        {
            return BadRequest(result.Errors);
        }
    }

    [HttpPost("edit")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult> EditPost(PostEditDto postEditDto)
    {
        var result = await profilePostService.UpdatePostAsync(new PostUpdateRequest(
            postEditDto.Id, 
            postEditDto.Description, 
            postEditDto.Title, 
            postEditDto.Preview?.ConvertToFileMetadata(),
            postEditDto.Categories ?? []
            ));
        if (result.IsSuccess)
        {
            return Ok();
        }
        else
        {
            return BadRequest(result.Errors);
        }
    }

    private PostCreateCommand MapToCommand(PostCreateRequest request)
    {
        return new PostCreateCommand(
            Type: request.Type,
            Title: request.Title,
            Visibility: request.Visibility,
            Description: request.Type == PostType.Video ? request.VideoPostData?.Description : null,
            TextContent: request.Type == PostType.Text ? request.TextPostData?.Text.Trim() : null,
            CategoryIds: request.VideoPostData?.Categories,
            TextFiles: request.Type == PostType.Text
                ? request.TextPostData?.Files?.Where(f => f.Length > 0)
                    .Select(f => f.ConvertToFileMetadata())
                    .ToList()
                : null,
            Thumbnail: request.Type == PostType.Video
                ? request.VideoPostData!.Thumbnail?.ConvertToFileMetadata()
                : null
        );
    }
}