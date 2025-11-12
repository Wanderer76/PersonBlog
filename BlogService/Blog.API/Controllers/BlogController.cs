using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blog.API.Models;
using Blog.Domain.Services.Models;
using Blog.Domain.Services;
using Blog.Service.Models.Blog;
using Blog.Service.Services;
using Infrastructure.Middleware;
using Authentication.Contract.Constants;
using Infrastructure.Services;

namespace Blog.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogController : BaseController
    {
        private readonly IBlogService _blogService;
        private readonly ISubscriptionLevelService _subscriptionLevelService;
        private readonly ICurrentUserService _currentUserService;
        public BlogController(ILogger<BaseController> logger, IBlogService blogService, ISubscriptionLevelService subscriptionLevelService, ICurrentUserService currentUserService) : base(logger)
        {
            _blogService = blogService;
            _subscriptionLevelService = subscriptionLevelService;
            _currentUserService = currentUserService;
        }

        [HttpPost("create")]
        [AuthFilter(Roles.User)]
        public async Task<IActionResult> CreateBlog([FromForm] BlogCreateForm form)
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            var result = await _blogService.CreateBlogAsync(new BlogCreateDto(user.UserId!.Value, form.Title, form.Description, form.PhotoUrl));
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result.Error);
            }
        }

        [HttpGet("hasBlog/{userId:guid}")]
        [AuthFilter(Roles.Blogger)]
        public async Task<IActionResult> GetBlogDetail(Guid userId)
        {
            var result = await _blogService.HasUserBlogAsync(userId);
            return Ok(result == null ? null : result.Value);
        }

        [HttpGet("hasUserBlog")]
        [Authorize]
        public async Task<IActionResult> HasUserBlog()
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            var result = await _blogService.HasUserBlogAsync(user.UserId!.Value);
            return Ok(new HasBlogResponse { HasBlog = result });
        }

        [HttpGet("detail")]
        [AuthFilter(Roles.Blogger)]
        public async Task<IActionResult> GetBlogDetail()
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            var result = await _blogService.GetBlogByUserIdAsync(user.UserId!.Value);
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
        public async Task<IActionResult> CreateSubscriptionLevel([FromBody] SubscriptionCreateDto form)
        {
            var result = await _subscriptionLevelService.CreateSubscriptionAsync(form);
            return Ok(result);
        }

        [HttpGet("blogByPost/{postId:guid}")]
        [Produces(typeof(BlogModel))]
        public async Task<IActionResult> GetBlogInfoByPostId(Guid postId)
        {
            var result = await _blogService.GetBlogByPostIdAsync(postId);
            return Ok(result);
        }


        [HttpGet("blogViewerInfoByPost/{postId:guid}")]
        [AuthFilter()]
        public async Task<IActionResult> GetBlogViewerInfoByPostId(Guid postId)
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            var result = await _blogService.GetBlogByPostIdAsync(postId, user.UserId);
            return Ok(result);
        }

        [HttpGet("blog/{blogId:guid}")]
        [Produces(typeof(BlogModel))]
        public async Task<IActionResult> GetBlogById(Guid blogId)
        {
            var result = await _blogService.GetBlogByIdAsync(blogId);
            return Ok(result);
        }
    }
    class HasBlogResponse
    {
        public Guid? HasBlog { get; set; }
    }
}
