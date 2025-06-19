using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Recommendation.Service.Service;

namespace Blog.Application.Controllers
{
    /// <summary>
    /// Тут будут методы для просмотра/комментирования контента
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ContentController : BaseController
    {
        private readonly IRecommendationService _recommendationService;
        public ContentController(ILogger<BaseController> logger, IRecommendationService recommendationService) : base(logger)
        {
            _recommendationService = recommendationService;
        }

        //  получить ленту постов(в перемешку видео/текст, есть возможность сразу посмотреть видео или перейти к блогу)


        //Получение информации о блоге от лица пользователя
        [HttpGet("blog/{id:guid}")]
        public async Task<IActionResult> GetBlog(Guid id)
        {
            return Ok();
        }

        [HttpGet("recommendations")]
        public async Task<IActionResult> GetRecommendations(int page, int pageSize, Guid? currentPostId)
        {
            return Ok(await _recommendationService.GetRecommendationsAsync(page, pageSize, currentPostId));
        }

        [HttpPost("postListByIds")]
        public async Task<IActionResult> GetRecommendations([FromBody] PostForm form)
        {
            return Ok(await _recommendationService.GetRecommendationsAsync(form.PostIds));
        }
    }

    public class PostForm
    {
        public List<Guid> PostIds { get; set; }
    }
}
