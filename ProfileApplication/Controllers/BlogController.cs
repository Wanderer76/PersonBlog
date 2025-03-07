using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileApplication.Models;
using Shared.Services;
using Profile.Service.Models.Blog;
using Profile.Service.Services;

namespace ProfileApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogController : BaseController
    {
        private readonly IBlogService _blogService;
        public BlogController(ILogger<BaseController> logger, IBlogService blogService) : base(logger)
        {
            _blogService = blogService;
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


        [HttpGet("blogByPost/{postId:guid}")]
        [Produces(typeof(BlogModel))]
        public async Task<IActionResult> GetDetailPostByIdAsync(Guid postId)
        {
            var result = await _blogService.GetBlogByPostIdAsync(postId);
            return Ok(result);
        }
    }
}
