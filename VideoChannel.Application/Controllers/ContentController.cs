using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Application.Controllers
{
    /// <summary>
    /// Тут будут методы для просмотра/комментирования контента
    /// </summary>
    public class ContentController : BaseController
    {
        public ContentController(ILogger<BaseController> logger) : base(logger)
        {
        }

        //  получить ленту постов(в перемешку видео/текст, есть возможность сразу посмотреть видео или перейти к блогу)
        [HttpGet("/recommendations")]
        public async Task<IActionResult> GetRecommendedPosts(int page, int offset)
        {
            return Ok();
        }

        //Получение информации о блоге от лица пользователя
        [HttpGet("blog/{id:guid}")]
        public async Task<IActionResult> GetBlog(Guid id)
        {
            return Ok();
        }

        //public async Task<IActionResult> AddCommentToPost(CommentCreateForm form)
        //{
        //    return Ok();
        //}

        //public async Task<IActionResult> EditPostComment(CommentCreateForm form)
        //{
        //    return Ok();
        //}

        //public async Task<IActionResult> DeletePostComment(Guid id)
        //{
        //    return Ok();
        //}

        //public async Task AddReactionToPost(Guid id, ReactionType reactionType)
        //{

        //}
        //public async Task DeleteReactionFromPost(Guid id, ReactionType reactionType)
        //{

        //}


    }
}
