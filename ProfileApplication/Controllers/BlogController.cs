using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace ProfileApplication.Controllers
{
    public class BlogController : BaseController
    {
        public BlogController(ILogger<BaseController> logger) : base(logger)
        {
        }

        public async Task<IActionResult> CreateBlog([FromBody] BlogCreateForm form)
        {
            return Ok();
        }

        public async Task<IActionResult> DeleteBlog()
        {
            return Ok();
        }

        public async Task<IActionResult> EditBlog([FromBody] BlogCreateForm form)
        {
            return Ok();
        }

        //Текстовые посты, можно добавлять картинки
        public async Task<IActionResult> AddPostToBlog([FromBody] PostCreateForm form)
        {
            return Ok();
        }

        public async Task<IActionResult> EditPost([FromBody] PostCreateForm form)
        {
            return Ok();
        }

        public async Task<IActionResult> DeletePost(Guid id)
        {
            return Ok();
        }

        public async Task<IActionResult> UploadVideo(IFormFile file)
        {
            return Ok();
        }

        public async Task<IActionResult> GetVideoChunk(Guid id, [FromHeader(Name = "Range")] string range)
        {
            return Ok();
        }
        public async Task<IActionResult> DeleteVideo(Guid id)
        {
            return Ok();
        }

        //Получение контента своего блога
        public async Task<IActionResult> GetPagedContent(int page, int limit)
        {
            return Ok();
        }

    }
}
