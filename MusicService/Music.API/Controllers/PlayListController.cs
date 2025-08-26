using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace Music.API.Controllers
{
    public class ProfilePlayListController : BaseController
    {
        public ProfilePlayListController(ILogger<BaseController> logger) : base(logger)
        {
        }

        /// <summary>
        /// общий список плейлистов пользователя
        /// </summary>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        [HttpGet("list")]
        public async Task<IActionResult> GetAllPlayList(int page, int size)
        {
            return Ok();
        }

        /// <summary>
        /// плейлист с создаными пользователем треками
        /// </summary>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyTracksPlayList()
        {
            return Ok();
        }

        /// <summary>
        /// плейлист с понравившимися треками
        /// </summary>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        [HttpGet("liked")]
        public async Task<IActionResult> GetLikedTracksPlayList()
        {
            return Ok();
        }

        /// <summary>
        /// плейлисты которые понравились пользователю
        /// </summary>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        [HttpGet("favourite")]
        public async Task<IActionResult> GetFavouritePlayLists(int page, int size)
        {
            return Ok();
        }



    }
}
