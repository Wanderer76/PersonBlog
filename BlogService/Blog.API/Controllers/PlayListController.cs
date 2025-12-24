using Authentication.Contract.Constants;
using Blog.Domain.Services;
using Blog.Domain.Services.Models.Playlist;
using Infrastructure.Extensions;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Services;

namespace Blog.API.Controllers;

[Obsolete]
public class PlayListController : BaseApiController
{
    private readonly IPlayListService _playListService;
    private readonly IFileStorage _fileStorage;
    private readonly ICurrentUserService _currentUserService;

    public PlayListController(ILogger<PlayListController> logger, IPlayListService playListService, IFileStorageFactory fileStorageFactory, ICurrentUserService currentUserService)
        : base(logger)
    {
        _playListService = playListService;
        _fileStorage = fileStorageFactory.CreateFileStorage();
        _currentUserService = currentUserService;
    }

    [HttpGet("list")]
    [Produces(typeof(IReadOnlyList<PlayListViewModel>))]
    public async Task<IActionResult> GetAllPlayLists(Guid blogId)
    {
        var result = await _playListService.GetBlogPlayListsAsync(blogId);

        if (result.IsFailure)
            return BadRequest(result.Errors);

        return Ok(result.Value);
    }

    [HttpGet("availableVideos")]
    [AuthFilter(Roles.Blogger)]
    [Produces(typeof(IReadOnlyList<PlayListViewModel>))]
    public async Task<IActionResult> GetAvailablePostsToPlayList(IEnumerable<Guid> playlistId)
    {
        var result = await _playListService.GetAvailablePostsToPlayListByIdAsync(playlistId);

        if (result.IsFailure)
            return BadRequest(result.Errors);

        return Ok(result.Value);
    }


    [HttpGet("item/{id:guid}")]
    [Produces(typeof(PlayListDetailViewModel))]
    public async Task<IActionResult> GetPlayList(Guid id)
    {
        var result = await _playListService.GetPlayListDetailAsync(id);

        if (result.IsFailure)
            return BadRequest(result.Errors);
        return Ok(result.Value);
    }

    [HttpPost("create")]
    [Authorize]
    [Produces(typeof(PlayListDetailViewModel))]
    public async Task<IActionResult> CreatePlayList([FromBody] PlayListCreateRequest form)
    {
        var result = await _playListService.CreatePlayListAsync(form);
        if (result.IsFailure)
            return BadRequest(result.Errors);
        return Ok(result.Value);
    }

    [HttpPost("update")]
    [Authorize]
    [Produces(typeof(PlayListDetailViewModel))]
    public async Task<IActionResult> UpdatePlayList([FromBody] PlayListUpdateRequest form)
    {
        var result = await _playListService.UpdatePlayListCommonDataAsync(form);
        if (result.IsFailure)
            return BadRequest(result.Errors);
        return Ok(result.Value);
    }

    [HttpPost("addVideo")]
    [Authorize]
    [Produces(typeof(PlayListViewModel))]

    public async Task<IActionResult> AddVideoToPlayList([FromBody] PlayListItemAddRequest form)
    {
        var result = await _playListService.AddVideoToPlayListAsync(form);
        if (result.IsFailure)
            return BadRequest(result.Errors);
        return Ok(result.Value);
    }
    
    [HttpPost("updatePositions")]
    [Authorize]
    [Produces(typeof(PlayListViewModel))]
    public async Task<IActionResult> UpdatePostPositions([FromBody] ChangePostPositionRequest form)
    {
        var result = await _playListService.ChangePostPositionAsync(form);
        if (result.IsFailure)
            return BadRequest(result.Errors);
        return Ok(result.Value);
    }

    [HttpPost("removePlaylist/{id:guid}")]
    [Authorize]
    [Produces(typeof(PlayListViewModel))]
    public async Task<IActionResult> RemovePlayList(Guid id)
    {
        var result = await _playListService.RemovePlayListAsync(id);
        if (result.IsFailure)
            return BadRequest(result.Errors);
        return Ok();
    }

    [HttpPost("removeVideo")]
    [Authorize]
    [Produces(typeof(PlayListViewModel))]
    public async Task<IActionResult> RemoveVideoFromPlayList([FromBody] PlayListItemRemoveRequest form)
    {
        var result = await _playListService.RemoveVideoFromPlayListAsync(new PlayListItemRemoveRequest
        {
            PlayListId = form.PlayListId,
            PostId = form.PostId,
        });
        if (result.IsFailure)
            return BadRequest(result.Errors);
        return Ok(result.Value);
    }

    [HttpPost("loadThumbnail")]
    [AuthFilter(Roles.Blogger)]
    [Produces(typeof(string))]
    public async Task<ActionResult<string>> AddVideoToPlayList([FromForm] IFormFile thumbnail)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var thumbnailId = await _fileStorage.PutFileAsync(user.BlogId, $"playListThumbnails/{GuidService.GetNewGuid()}", thumbnail.OpenReadStream());
        var url = await _fileStorage.GetFileUrlAsync(user.UserId, thumbnailId);
        return Ok(new
        {
            ThumbnailId = thumbnailId,
            ThumbnailUrl = url
        });
    }
}
