using Blog.API.Models;
using Blog.Contracts.Models;
using Blog.Domain.Entities;
using Blog.Domain.Services;
using Blog.Domain.Services.Models;
using Blog.Service.Models;
using Blog.Service.Models.File;
using Blog.Service.Models.Post;
using Blog.Service.Services;
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
    private readonly IVideoService _videoService;
    private readonly ISubscriptionLevelService _subscriptionLevelService;
    private readonly ICategoryService _categoryService;
    private readonly ICurrentUserService _currentUserService;
    public PostController(ILogger<PostController> logger, IPostService postService, IUserPostService userPostService, IVideoService videoService, ISubscriptionLevelService subscriptionLevelService, ICurrentUserService currentUserService, ICategoryService categoryService) : base(logger)
    {
        _postService = postService;
        _userPostService = userPostService;
        _videoService = videoService;
        _subscriptionLevelService = subscriptionLevelService;
        _currentUserService = currentUserService;
        _categoryService = categoryService;
    }

    [HttpGet("manifest/{postId:guid}")]
    [Produces(typeof(PostFileMetadataModel))]
    public async Task<ActionResult<PostFileMetadataModel>> GetVideoFileMetadataByPostIdAsync(Guid postId)
    {
        var result = await _postService.GetVideoFileMetadataByPostIdAsync(postId);
        if (result.IsSuccess)
            return Ok(result.Value);
        else return BadRequest(result.Error);
    }

    [HttpGet("detail/{postId:guid}")]
    [Produces(typeof(PostDetailViewModel))]
    public async Task<ActionResult<PostDetailViewModel>> GetDetailPostByIdAsync(Guid postId)
    {
        var result = await _postService.GetDetailPostByIdAsync(postId);
        return Ok(result);
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

    [HttpGet("list")]
    public async Task<ActionResult<PostPagedListViewModel>> GetBlogPostPagedList(Guid blogId, int page, int limit)
    {
        var result = await _postService.GetPostsByBlogIdPagedAsync(blogId, page, limit);
        return Ok(result);
    }

    [HttpGet("create")]
    public async Task<ActionResult<CreatePostModelViewModel>> GetCreatePostModel()
    {
        var subscriptionLevels = await _subscriptionLevelService.GetAllSubscriptionsAsync();
        var visibilityList = await _postService.GetPostVisibilityListAsync();
        var categoryList = await _categoryService.GetAllCategoriesAsync();
        return Ok(new CreatePostModelViewModel(subscriptionLevels, visibilityList, categoryList));
    }

    [HttpPost("create")]
    [Authorize]
    public async Task<ActionResult<Guid>> AddPostToBlog([FromForm] PostCreateForm form)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var result = await _postService.CreatePostAsync(new PostCreateDto
        {
            UserId = user.UserId,
            Type = PostType.Video,
            Text = form.Description?.Trim(),
            Title = form.Title.Trim(),
            Video = form.Video,
            Photos = form.Files,
            IsPartial = form.IsPartial,
            Visibility = form.Visibility,
            Thumbnail = form.Thumbnail,
            Categories = form.Categories ?? []
        });
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        else
        {
            return BadRequest(result.Error);
        }
    }

    [HttpGet("edit/{postId:guid}")]
    [Authorize]
    public async Task<ActionResult<PostEditViewModel>> EditPost(Guid postId)
    {
        var result = await _postService.GetPostUpdateModelAsync(postId);
        if (result.IsSuccess)
            return Ok(result.Value);
        return BadRequest(result.Errors);
    }

    [HttpPost("edit")]
    [Authorize]
    public async Task<ActionResult<PostModel>> EditPost([FromForm] PostEditForm form)
    {
        var userId = HttpContext.GetUserFromContext();

        var result = await _postService.UpdatePostAsync(new PostEditDto
        (
            form.Id,
            userId,
            form.Description,
            form.Title,
            form.PreviewId,
            form.Categories ?? []
        ));
        return Ok(result);
    }

    [HttpDelete("delete/{id:guid}")]
    [Authorize]
    public async Task<ActionResult> DeletePost(Guid id)
    {
        await _postService.RemovePostByIdAsync(id);
        return Ok();
    }

    [HttpGet("uploadProgress")]
    [Authorize]
    public async Task<ActionResult<UploadVideoProgress>> GetPostVideoUploadProgress(Guid fileId)
    {
        var result = await _videoService.GetUploadVideoMetadata(fileId);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(result.Errors);
    }

    [HttpPost("uploadProgress")]
    [Authorize]
    public async Task<ActionResult<UploadVideoProgress>> CreatePostVideoUploadProgress(CreateUploadVideoProgressRequest request)
    {
        var result = await _videoService.CreateUploadVideoMetadata(request);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(result.Errors);
    }

    [HttpPost("uploadChunk")]
    public async Task<ActionResult> UploadVideoChunk([FromForm] UploadVideoChunkForm uploadVideoChunk)
    {
        try
        {
            var metadata = await _videoService.GetOrCreateVideoMetadata(uploadVideoChunk.ToUploadVideoChunkModel());
            using var data = uploadVideoChunk.ChunkData.OpenReadStream();
            await _postService.UploadVideoChunkAsync(new UploadVideoChunkDto
            {
                ChunkNumber = uploadVideoChunk.ChunkNumber,
                TotalChunkCount = uploadVideoChunk.TotalChunkCount,
                ChunkData = data,
                PostId = uploadVideoChunk.PostId
            });
        }
        catch (Exception ex)
        {
            return BadRequest();
        }
        return Ok();

    }

    [HttpGet("hasView")]
    [Produces(typeof(bool))]
    public async Task<ActionResult<bool>> CheckForViewAsync(Guid? userId, string? ipAddress)
    {
        var result = await _postService.CheckForViewAsync(userId, ipAddress);
        return Ok(result);
    }

    [HttpPost("commonByIds")]
    [Produces(typeof(PostCommonModel))]
    public async Task<ActionResult<IReadOnlyList<PostCommonModel>>> GetPostCommonModel([FromBody]List<Guid> ids)
    {
        return Ok(await _postService.GetPostCommonModelAsync(ids));
    }
}
