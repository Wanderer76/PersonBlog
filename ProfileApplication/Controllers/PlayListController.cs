using Blog.Domain.Services;
using Blog.Domain.Services.Models.Playlist;
using FileStorage.Service.Service;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Services;
using Shared.Utils;

namespace Blog.API.Controllers
{
    public class PlayListController : BaseController
    {
        private readonly IPlayListService _playListService;
        private readonly IFileStorage _fileStorageFactory;
        public PlayListController(ILogger<PlayListController> logger, IPlayListService playListService, IFileStorageFactory fileStorageFactory) : base(logger)
        {
            _playListService = playListService;
            _fileStorageFactory = fileStorageFactory.CreateFileStorage();
        }

        [HttpGet("list")]
        [Produces(typeof(IReadOnlyList<PlayListViewModel>))]
        public async Task<IActionResult> GetAllPlayLists(Guid blogId)
        {

            var result = await _playListService.GetBlogPlayListsAsync(blogId);

            if (result.IsFailure)
                return BadRequest(result.Error);
            
            return Ok(result.Value);
        }

        [HttpGet("item/{id:guid}")]
        [Produces(typeof(PlayListDetailViewModel))]

        public async Task<IActionResult> GetPlayList(Guid id)
        {
            var result = await _playListService.GetPlayListDetailAsync(id);

            if (result.IsFailure)
                return BadRequest(result.Error);
            return Ok(result.Value);
        }

        [HttpPost("create")]
        [Authorize]
        [Produces(typeof(PlayListDetailViewModel))]
        public async Task<IActionResult> CreatePlayList([FromBody] PlayListCreateRequest form)
        {
            var result = await _playListService.CreatePlayListAsync(new PlayListCreateRequest
            {
                Title = form.Title,
                PostIds = form.PostIds,
                ThumbnailId = form.ThumbnailId
            });
            return Ok(result.Value);
        }


        [HttpPost("addVideo")]
        [Authorize]
        [Produces(typeof(PlayListViewModel))]

        public async Task<IActionResult> AddVideoToPlayList([FromBody] PlayListItemAddRequest form)
        {
            var result = await _playListService.AddVideoToPlayListAsync(new PlayListItemAddRequest
            {
                PlayListId = form.PlayListId,
                PostId = form.PostId,
                Position = form.Position,
            });
            return Ok(result.Value);
        }

        [HttpPost("loadThumbnail")]
        [Authorize]
        [Produces(typeof(string))]
        public async Task<IActionResult> AddVideoToPlayList([FromBody] IFormFile from)
        {
            var userId = HttpContext.GetUserFromContext();
            var thumbnailId = await _fileStorageFactory.PutFileAsync(userId, from.Name, from.OpenReadStream());
            return Ok(thumbnailId);
        }
    }
}
