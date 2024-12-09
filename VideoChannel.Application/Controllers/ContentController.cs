using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace VideoChannel.Application.Controllers
{
    /// <summary>
    /// Тут будут методы для просмотра/комментирования контента
    /// </summary>
    public class ContentController : BaseController
    {
        public ContentController(ILogger<BaseController> logger) : base(logger)
        {
        }

        public async Task<IActionResult> GetVideoChunk(Guid id, [FromHeader(Name = "Range")] string range)
        {
            return Ok();
        }

        public async Task<IActionResult> GetTextPost(Guid id)
        {
            return BadRequest();
        }

    }
}
