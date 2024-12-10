using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Profile.Domain.Entities;
using Profile.Service.Interface;
using Profile.Service.Models.Post;
using Shared.Services;

namespace ProfileApplication.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BlogController : BaseController
    {
        private readonly IPostService _postService;

        public BlogController(ILogger<BaseController> logger, IPostService postService) : base(logger)
        {
            _postService = postService;
        }

        //public async Task<IActionResult> CreateBlog([FromBody] BlogCreateForm form)
        //{
        //    return Ok();
        //}

        //public async Task<IActionResult> DeleteBlog()
        //{
        //    return Ok();
        //}

        //public async Task<IActionResult> EditBlog([FromBody] BlogCreateForm form)
        //{
        //    return Ok();
        //}

        //Текстовые посты, можно добавлять картинки
        [HttpPost("post/create")]
        [Authorize]
        public async Task<IActionResult> AddPostToBlog([FromForm] PostCreateForm form)
        {

            var userId = HttpContext.GetUserFromContext();
            var result = await _postService.CreatePost(new PostCreateDto
            {
                UserId = userId,
                Type = PostType.Media,
                Text = form.Text,
                Title = form.Title,
                Video = form.Video,
            });

            return Ok(result);
        }

        //public async Task<IActionResult> EditPost([FromBody] PostCreateForm form)
        //{
        //    return Ok();
        //}

        //public async Task<IActionResult> DeletePost(Guid id)
        //{
        //    return Ok();
        //}

        //public async Task<IActionResult> UploadVideo(IFormFile file)
        //{
        //    return Ok();
        //}

        //public async Task<IActionResult> GetVideoChunk(Guid id, [FromHeader(Name = "Range")] string range)
        //{
        //    return Ok();
        //}
        //public async Task<IActionResult> DeleteVideo(Guid id)
        //{
        //    return Ok();
        //}

        ////Получение контента своего блога
        //public async Task<IActionResult> GetPagedContent(int page, int limit)
        //{
        //    return Ok();
        //}

    }

    public class PostCreateForm
    {
        public string Title { get; set; }
        public string? Text { get; set; }
        public PostType Type { get; set; }
        public IFormFile? Video { get; set; }
    }
}
