using Blog.Domain.Services;
using Blog.Domain.Services.Models.Playlist;
using FileStorage.Service.Service;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Services;

namespace Blog.API.Controllers;

public class PlayListController : BaseController
{
    private readonly IPlayListService _playListService;
    private readonly IFileStorage _fileStorage;

    public PlayListController(ILogger<PlayListController> logger, IPlayListService playListService, IFileStorageFactory fileStorageFactory)
        : base(logger)
    {
        _playListService = playListService;
        _fileStorage = fileStorageFactory.CreateFileStorage();
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

    [HttpGet("availableVideos")]
    [Produces(typeof(IReadOnlyList<PlayListViewModel>))]
    public async Task<IActionResult> GetAvailablePostsToPlayList(Guid? playlistId)
    {
        var result = await _playListService.GetAvailablePostsToPlayListByIdAsync(playlistId);

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
        var result = await _playListService.CreatePlayListAsync(form);
        return Ok(result.Value);
    }

    [HttpPost("update")]
    [Authorize]
    [Produces(typeof(PlayListDetailViewModel))]
    public async Task<IActionResult> UpdatePlayList([FromBody] PlayListUpdateRequest form)
    {
        var result = await _playListService.UpdatePlayListCommonDataAsync(form);
        return Ok(result.Value);
    }

    [HttpPost("addVideo")]
    [Authorize]
    [Produces(typeof(PlayListViewModel))]

    public async Task<IActionResult> AddVideoToPlayList([FromBody] PlayListItemAddRequest form)
    {
        var result = await _playListService.AddVideoToPlayListAsync(form);
        return Ok(result.Value);
    }
    
    [HttpPost("updatePositions")]
    [Authorize]
    [Produces(typeof(PlayListViewModel))]
    public async Task<IActionResult> UpdatePostPositions([FromBody] ChangePostPositionRequest form)
    {
        var result = await _playListService.ChangePostPositionAsync(form);
        return Ok(result.Value);
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
        return Ok(result.Value);
    }

    [HttpPost("loadThumbnail")]
    [Authorize]
    [Produces(typeof(string))]
    public async Task<IActionResult> AddVideoToPlayList([FromForm] IFormFile thumbnail)
    {
        var userId = HttpContext.GetUserFromContext();
        var thumbnailId = await _fileStorage.PutFileAsync(userId, GuidService.GetNewGuid(), thumbnail.OpenReadStream());
        var url = await _fileStorage.GetFileUrlAsync(userId, thumbnailId);
        return Ok(new
        {
            ThumbnailId = thumbnailId,
            ThumbnailUrl = url
        });
    }
}
