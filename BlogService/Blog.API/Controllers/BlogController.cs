using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Services;
using Blog.API.Models;
using Blog.Domain.Services.Models;
using Blog.Domain.Services;
using Blog.Service.Models.Blog;
using Blog.Service.Services;

namespace Blog.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogController : BaseController
    {
        private readonly IBlogService _blogService;
        private readonly ISubscriptionLevelService _subscriptionLevelService;
        public BlogController(ILogger<BaseController> logger, IBlogService blogService, ISubscriptionLevelService subscriptionLevelService) : base(logger)
        {
            _blogService = blogService;
            _subscriptionLevelService = subscriptionLevelService;
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateBlog([FromForm] BlogCreateForm form)
        {
            var userId = HttpContext.GetUserFromContext();
            try
            {
                var result = await _blogService.CreateBlogAsync(new BlogCreateDto(userId, form.Title, form.Description, form.PhotoUrl));
                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("hasBlog/{userId:guid}")]
        [Authorize]
        public async Task<IActionResult> GetBlogDetail(Guid userId)
        {
            var result = await _blogService.HasUserBlogAsync(userId);
            return Ok(result == null ? null : result.Value);
        }

        [HttpGet("hasUserBlog")]
        [Authorize]
        public async Task<IActionResult> HasUserBlog()
        {
            var userId = HttpContext.GetUserFromContext();
            var result = await _blogService.HasUserBlogAsync(userId);
            return Ok(new HasBlogResponse { HasBlog = result});
        }

        [HttpGet("detail")]
        [Authorize]
        public async Task<IActionResult> GetBlogDetail()
        {
            var userId = HttpContext.GetUserFromContext();
            var result = await _blogService.GetBlogByUserIdAsync(userId);
            return Ok(result);
        }

        //public async Task<IActionResult> DeleteVideo(Guid id)
        //{
        //    return Ok();
        //}

        ////Получение контента своего блога
        //public async Task<IActionResult> GetPagedContent(int page, int limit)
        //{
        //    return Ok();
        //}

        [HttpGet("subscriptionLevelCreate")]
        [Authorize]
        public async Task<IActionResult> CreateSubscriptionLevel()
        {
            var result = await _subscriptionLevelService.GetAllSubscriptionsAsync();
            return Ok(new
            {
                SubscriptionLevels = result
            });
        }

        [HttpPost("subscriptionLevelCreate")]
        [Authorize]
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
        [Produces(typeof(BlogModel))]
        public async Task<IActionResult> GetBlogViewerInfoByPostId(Guid postId)
        {
            HttpContext.TryGetUserFromContext(out var userId);
            var result = await _blogService.GetBlogByPostIdAsync(postId, userId);
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
