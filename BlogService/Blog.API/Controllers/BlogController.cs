using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Blog.API.Models;
using Infrastructure.Middleware;
using Authentication.Contract.Constants;
using Infrastructure.Services;
using Blog.Contracts.Models.Blog;
using Blog.Contracts.Services;
using Blog.Contracts.Models;

namespace Blog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BlogController(
    ILogger<BaseApiController> logger,
    IBlogService blogService,
    ISubscriptionLevelService subscriptionLevelService,
    ICurrentUserService currentUserService)
    : BaseApiController(logger)
{
    [HttpPost("create")]
    [AuthFilter(Roles.User)]
    public async Task<ActionResult<BlogModel>> CreateBlog([FromForm] BlogCreateForm form)
    {
        var result = await blogService.CreateBlogAsync(new BlogCreateRequest(form.Title, form.Description, form.PhotoUrl));
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        else
        {
            return BadRequest(result.Errors);
        }
    }

    [HttpPut("{blogId:guid}")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<BlogModel>> UpdateBlog(Guid blogId, [FromForm] BlogEditRequest request)
    {
        request.Id = blogId;
        var result = await blogService.UpdateBlogAsync(request);
        if (result.IsSuccess)
            return Ok(result.Value);

        if (result.Errors.Any(error => error.Key == "NotFound"))
            return NotFound(result.Errors);

        if (result.Errors.Any(error => error.Key == "Forbidden"))
            return Forbid();

        return BadRequest(result.Errors);
    }

    [HttpGet("hasBlog/{userId:guid}")]
    [Produces<bool>]
    public async Task<ActionResult<bool>> GetBlogDetail(Guid userId)
    {
        var result = await blogService.HasUserBlogAsync(userId);
        return Ok(result);
    }

    [HttpGet("hasUserBlog")]
    [AuthFilter(Roles.User)]
    public async Task<ActionResult<HasBlogResponse>> HasUserBlog()
    {
        var user = await currentUserService.GetCurrentUserAsync();
        var result = await blogService.HasUserBlogAsync(user.UserId!);
        return Ok(new HasBlogResponse { HasBlog = result });
    }

    [HttpGet("detail")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<BlogModel>> GetBlogDetail()
    {
        var user = await currentUserService.GetCurrentUserAsync();
        var result = await blogService.GetBlogByUserIdAsync(user.UserId);
        return Ok(result);
    }

    [HttpGet("subscriptionLevelCreate")]
    [HttpGet("subscription-levels")]
    [AuthFilter(Roles.Blogger)]
    public async Task<IActionResult> CreateSubscriptionLevel()
    {
        var result = await subscriptionLevelService.GetAllSubscriptionsAsync();
        return Ok(result);
    }

    [HttpPost("subscriptionLevelCreate")]
    [HttpPost("subscription-levels")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<SubscriptionLevelModel>> CreateSubscriptionLevel([FromBody] SubscriptionCreateDto form)
    {
        var result = await subscriptionLevelService.CreateSubscriptionAsync(form);
        if (result.IsSuccess)
            return Ok(result.Value);

        if (result.Errors.Any(error => error.Key == "NotFound"))
            return NotFound(result.Errors);

        if (result.Errors.Any(error => error.Key == "Forbidden"))
            return Forbid();

        return BadRequest(result.Errors);
    }

    [HttpGet("subscription-levels/blog/{blogId:guid}")]
    public async Task<ActionResult<IEnumerable<SubscriptionLevelModel>>> GetSubscriptionLevelsByBlog(Guid blogId)
    {
        return Ok(await subscriptionLevelService.GetAllSubscriptionsByBlogIdAsync(blogId));
    }

    [HttpGet("subscription-levels/{id:guid}")]
    public async Task<ActionResult<SubscriptionLevelModel>> GetSubscriptionLevel(Guid id)
    {
        var result = await subscriptionLevelService.GetSubscriptionByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Errors);
    }

    [HttpPut("subscription-levels/{id:guid}")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<SubscriptionLevelModel>> UpdateSubscriptionLevel(
        Guid id,
        [FromBody] SubscriptionUpdateDto form)
    {
        form.Id = id;
        var result = await subscriptionLevelService.UpdateSubscriptionAsync(form);
        if (result.IsSuccess)
            return Ok(result.Value);

        if (result.Errors.Any(error => error.Key == "NotFound"))
            return NotFound(result.Errors);

        if (result.Errors.Any(error => error.Key == "Forbidden"))
            return Forbid();

        return BadRequest(result.Errors);
    }

    [HttpDelete("subscription-levels/{id:guid}")]
    [AuthFilter(Roles.Blogger)]
    public async Task<IActionResult> DeleteSubscriptionLevel(Guid id)
    {
        var result = await subscriptionLevelService.DeleteSubscriptionAsync(id);
        if (result.IsSuccess)
            return NoContent();

        if (result.Errors.Any(error => error.Key == "NotFound"))
            return NotFound(result.Errors);

        if (result.Errors.Any(error => error.Key == "Forbidden"))
            return Forbid();

        return BadRequest(result.Errors);
    }

    [HttpGet("blogByPost/{postId:guid}")]
    [Produces(typeof(BlogModel))]
    public async Task<ActionResult<BlogModel>> GetBlogInfoByPostId(Guid postId)
    {
        var result = await blogService.GetBlogByPostIdAsync(postId);
        return Ok(result);
    }


    [HttpGet("blogViewerInfoByPost/{postId:guid}")]
    public async Task<ActionResult<BlogUserInfoViewModel>> GetBlogViewerInfoByPostId(Guid postId)
    {
        var user = await currentUserService.GetCurrentUserAsync();
        Guid? viewerId = user.IsAnonymous ? null : user.UserId;
        var result = await blogService.GetBlogByPostIdAsync(postId, viewerId);
        return Ok(result);
    }

    [HttpGet("blog/{blogId:guid}")]
    [Produces(typeof(BlogModel))]
    public async Task<ActionResult<BlogModel>> GetBlogById(Guid blogId)
    {
        var result = await blogService.GetBlogByIdAsync(blogId);
        return Ok(result);
    }
}

