using Authentication.Contract.Constants;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Music.Contract.Models;
using Music.Domain.Entities;
using Music.Domain.Services;

namespace Music.API.Controllers
{
    public class ProfilePlayListController : BaseController
    {
        private readonly IMusicPlayListService _musicPlayListService;
        public ProfilePlayListController(ILogger<BaseController> logger, IMusicPlayListService musicPlayListService) : base(logger)
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
                BadRequest(result.Error);
        }

        [HttpGet("{id}")]
        [Produces<IReadOnlyList<TrackViewItem>>]
        public async Task<IActionResult> GetPlayListTracks(Guid id)
        {
            var result = await _musicPlayListService.GetPlayListInfoAsync(id);
            if (result.IsSuccess)
                return Ok(result.Value);
            return
                BadRequest(result.Error);
        }

        [HttpGet("{id}/tracks")]
        [Produces<IReadOnlyList<TrackViewItem>>]
        public async Task<IActionResult> GetPlayListTracks(Guid id, int? page, int? size)
        {
            var result = await _musicPlayListService.GetPlayListTrackListAsync(id, page ?? 1, size ?? 10);
            if (result.IsSuccess)
                return Ok(result.Value);
            return
                BadRequest(result.Error);
        }

        [HttpPost("{id}/tracks/{trackId}/delete")]
        public async Task<IActionResult> RemoveTrackFromPlayList(Guid id, Guid trackId)
        {
            var result = await _musicPlayListService.RemoveTrackFromPlayListAsync(id, trackId);
            if (result.IsSuccess)
                return Ok();
            return
                BadRequest(result.Error);
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
        public async Task<IActionResult> AddTrackToLikedPlayList(Guid trackId)
        {
            var result = await _musicPlayListService.AddTrackToPlayList(trackId, ConstPlayListType.Liked);
            return Ok();
        }     
        [HttpPost("unliked")]
        public async Task<IActionResult> RemoveTrackToLikedPlayList(Guid trackId)
        {
            var playlists = await _musicPlayListService.GetCurrentUserPlayListsAsync();
            var result = await _musicPlayListService.RemoveTrackFromPlayListAsync(
                playlists.Value.First(x=>x.Type == ConstPlayListType.Liked.ToString()).Id,
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
        public async Task<IActionResult> GetFavouritePlayLists(int page, int size)
        {
            return Ok();
        }



    }
}
