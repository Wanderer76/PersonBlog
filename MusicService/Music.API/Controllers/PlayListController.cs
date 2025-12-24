using Authentication.Contract.Constants;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Music.Contract.Models;
using Music.Contract.Models.PlayList;
using Music.Domain.Entities;
using Music.Domain.Services;

namespace Music.API.Controllers
{
    public class ProfilePlayListController : BaseApiController
    {
        private readonly IMusicPlayListService _musicPlayListService;
        public ProfilePlayListController(ILogger<BaseApiController> logger, IMusicPlayListService musicPlayListService) : base(logger)
        {
            _musicPlayListService = musicPlayListService;
        }

        /// <summary>
        /// общий список плейлистов пользователя
        /// </summary>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        [HttpGet("list")]
        [AuthFilter(Roles.User)]
        public async Task<IActionResult> GetAllPlayList(int page, int size)
        {
            var result = await _musicPlayListService.GetCurrentUserPlayListsAsync();
            if (result.IsSuccess)
                return Ok(result.Value);
            return
                BadRequest(result.Errors);
        }

        [HttpGet("{id}")]
        [Produces<IReadOnlyList<TrackViewItem>>]
        public async Task<IActionResult> GetPlayListTracks(Guid id)
        {
            var result = await _musicPlayListService.GetPlayListInfoAsync(id);
            if (result.IsSuccess)
                return Ok(result.Value);
            return
                BadRequest(result.Errors);
        }

        [HttpGet("{id}/tracks")]
        [Produces<IReadOnlyList<TrackViewItem>>]
        public async Task<IActionResult> GetPlayListTracks(Guid id, int? page, int? size)
        {
            var result = await _musicPlayListService.GetPlayListTrackListAsync(id, page ?? 1, size ?? 10);
            if (result.IsSuccess)
                return Ok(result.Value);
            return
                BadRequest(result.Errors);
        }

        [HttpPost("{id}/tracks/{trackId}/delete")]
        [AuthFilter(Roles.User)]
        public async Task<IActionResult> RemoveTrackFromPlayList(Guid id, Guid trackId)
        {
            var result = await _musicPlayListService.RemoveTrackFromPlayListAsync(id, trackId);
            if (result.IsSuccess)
                return Ok();
            return
                BadRequest(result.Errors);
        }

        [HttpPost("{id}/tracks/{trackId}/add")]
        [AuthFilter(Roles.User)]
        public async Task<IActionResult> AddTrackToPlayList(Guid id, Guid trackId)
        {
            var result = await _musicPlayListService.AddTrackToPlayListAsync(id, trackId);
            if (result.IsSuccess)
                return Ok();
            return
                BadRequest(result.Errors);
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
        [HttpPost("liked")]
        [AuthFilter(Roles.User)]
        public async Task<IActionResult> AddTrackToLikedPlayList(Guid trackId)
        {
            var result = await _musicPlayListService.AddTrackToPlayListAsync(trackId, ConstPlayListType.Liked);
            return Ok();
        }
        [HttpPost("unliked")]
        [AuthFilter(Roles.User)]
        public async Task<IActionResult> RemoveTrackToLikedPlayList(Guid trackId)
        {
            var playlists = await _musicPlayListService.GetCurrentUserPlayListsAsync();
            var result = await _musicPlayListService.RemoveTrackFromPlayListAsync(
                playlists.Value.First(x => x.Type == ConstPlayListType.Liked.ToString()).Id,
                trackId);
            return Ok();
        }

        /// <summary>
        /// плейлисты которые понравились пользователю
        /// </summary>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        [HttpGet("favourite")]
        [AuthFilter(Roles.User)]
        public async Task<IActionResult> GetFavouritePlayLists(int page, int size)
        {
            return Ok();
        }

        [HttpPost("create")]
        [AuthFilter(Roles.User)]
        public async Task<IActionResult> CreatePlayList(CreatePlayListRequest createPlayList)
        {
            var result = await _musicPlayListService.CreatePlayListsAsync(createPlayList);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return BadRequest(result.Errors);
        }

        [HttpPost("remove/{id}")]
        [AuthFilter(Roles.User)]
        public async Task<IActionResult> RemovePlayList(Guid id)
        {
            var result = await _musicPlayListService.RemovePlayListAsync(id);
            if (result.IsSuccess)
            {
                return Ok();
            }
            return BadRequest(result.Errors);
        }
    }
}
