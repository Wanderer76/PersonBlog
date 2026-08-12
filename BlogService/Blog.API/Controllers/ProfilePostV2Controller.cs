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
    public async Task<ActionResult<UserPostInfoModel>> CreatePost([FromForm] VideoPostCreateRequest request)
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
            postEditDto.Categories ?? [],
            postEditDto.Visibility
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

    [HttpPost("createTextPost")]
    [AuthFilter(Roles.Blogger)]
    public async Task<IActionResult> CreateTextPost([FromForm] TextPostCreateForm textPostCreateForm)
    {
        var postCreateResult = await profilePostService.CreatePostAsync(new PostCreateCommand(
            PostType.Text,
            textPostCreateForm.Title,
            textPostCreateForm.Visibility,
            null,
            textPostCreateForm.Text,
            [],
            textPostCreateForm.Media?.Select(x => x.ConvertToFileMetadata()).ToList(),
            null));

        return Ok(postCreateResult);
    }

    [HttpGet("textEdit/{postId:guid}")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<TextPostEditViewModel>> GetTextPostEditModel(Guid postId)
    {
        var result = await profilePostService.GetTextPostEditViewModelAsync(postId);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpPost("textEdit")]
    [AuthFilter(Roles.Blogger)]
    public async Task<IActionResult> EditTextPost([FromForm] TextPostEditDto request)
    {
        var result = await profilePostService.UpdateTextPostAsync(request);
        return result.IsSuccess ? Ok() : BadRequest(result.Errors);
    }

    private static PostCreateCommand MapToCommand(VideoPostCreateRequest request) => new(
            Type: PostType.Video,
            Title: request.Title,
            Visibility: request.Visibility,
            Description: request.VideoPostData.Description,
            TextContent: null,
            CategoryIds: request.VideoPostData?.Categories,
            TextFiles: null,
            Thumbnail: request.VideoPostData!.Thumbnail?.ConvertToFileMetadata()
        );
}
