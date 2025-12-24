using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Blog.API.Models;
using Blog.Domain.Services.Models;
using Blog.Domain.Services;
using Blog.Service.Models.Blog;
using Blog.Service.Services;
using Infrastructure.Middleware;
using Authentication.Contract.Constants;
using Infrastructure.Services;

namespace Blog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlogController : BaseApiController
{
    private readonly IBlogService _blogService;
    private readonly ISubscriptionLevelService _subscriptionLevelService;
    private readonly ICurrentUserService _currentUserService;
    public BlogController(ILogger<BaseApiController> logger, IBlogService blogService, ISubscriptionLevelService subscriptionLevelService, ICurrentUserService currentUserService) : base(logger)
    {
        _blogService = blogService;
        _subscriptionLevelService = subscriptionLevelService;
        _currentUserService = currentUserService;
    }

    [HttpPost("create")]
    [AuthFilter(Roles.User)]
    public async Task<ActionResult<BlogModel>> CreateBlog([FromForm] BlogCreateForm form)
    {
        var result = await _blogService.CreateBlogAsync(new BlogCreateDto(form.Title, form.Description, form.PhotoUrl));
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        else
        {
            return BadRequest(result.Errors);
        }
    }

    [HttpGet("hasBlog/{userId:guid}")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<Guid?>> GetBlogDetail(Guid userId)
    {
        var result = await _blogService.HasUserBlogAsync(userId);
        return Ok(result);
    }

    [HttpGet("hasUserBlog")]
    [AuthFilter(Roles.User)]
    public async Task<ActionResult<HasBlogResponse>> HasUserBlog()
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var result = await _blogService.HasUserBlogAsync(user.UserId!);
        return Ok(new HasBlogResponse { HasBlog = result });
    }

    [HttpGet("detail")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<BlogModel>> GetBlogDetail()
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var result = await _blogService.GetBlogByUserIdAsync(user.UserId);
        return Ok(result);
    }

    [HttpGet("subscriptionLevelCreate")]
    [AuthFilter(Roles.Blogger)]
    public async Task<IActionResult> CreateSubscriptionLevel()
    {
        var result = await _subscriptionLevelService.GetAllSubscriptionsAsync();
        return Ok(new
        {
            SubscriptionLevels = result
        });
    }

    [HttpPost("subscriptionLevelCreate")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<SubscriptionLevelModel>> CreateSubscriptionLevel([FromBody] SubscriptionCreateDto form)
    {
        var result = await _subscriptionLevelService.CreateSubscriptionAsync(form);
        return Ok(result);
    }

    [HttpGet("blogByPost/{postId:guid}")]
    [Produces(typeof(BlogModel))]
    public async Task<ActionResult<BlogModel>> GetBlogInfoByPostId(Guid postId)
    {
        var result = await _blogService.GetBlogByPostIdAsync(postId);
        return Ok(result);
    }


    [HttpGet("blogViewerInfoByPost/{postId:guid}")]
    [AuthFilter]
    public async Task<ActionResult<BlogUserInfoViewModel>> GetBlogViewerInfoByPostId(Guid postId)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var result = await _blogService.GetBlogByPostIdAsync(postId, user.UserId);
        return Ok(result);
    }

    [HttpGet("blog/{blogId:guid}")]
    [Produces(typeof(BlogModel))]
    public async Task<ActionResult<BlogModel>> GetBlogById(Guid blogId)
    {
        var result = await _blogService.GetBlogByIdAsync(blogId);
        return Ok(result);
    }
}

public class HasBlogResponse
{
    public Guid? HasBlog { get; set; }
}
