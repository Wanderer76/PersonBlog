using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace VideoChannel.Application.Controllers
{

    //тут будут методы для поиска блогов
    public class UserController : BaseController
    {
        public UserController(ILogger<BaseController> logger) : base(logger)
        {
        }

        //получить ленту постов(в перемешку видео/текст, есть возможность сразу посмотреть видео или перейти к блогу)
        public async Task<IActionResult> GetRecommendedPosts(int page,int offset)
        {
            return Ok();
        }

        //Получение информации о блоге от лица пользователя
        public async Task<IActionResult> GetBlog(Guid id)
        {

        }

        public async Task<IActionResult> AddCommentToPost(CommentCreateForm form)
        {
            return Ok();
        }

        public async Task<IActionResult> EditPostComment(CommentCreateForm form)
        {
            return Ok();
        }

        public async Task<IActionResult> DeletePostComment(Guid id)
        {
            return Ok();
        }

        public async Task AddReactionToPost(Guid id, ReactionType reactionType)
        {

        }
        public async Task DeleteReactionFromPost(Guid id, ReactionType reactionType)
        {

        }

    }
}
