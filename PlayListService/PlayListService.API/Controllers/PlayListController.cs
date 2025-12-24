using Authentication.Contract.Constants;
using Blog.Contracts.Models;
using Infrastructure.Extensions;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using PlayListService.Services.Models;
using PlayListService.Services.Services;
using Shared.Models;

namespace PlayListService.API.Controllers;

public class PlayListController : BaseApiController
{
    private readonly CrudPlayListService _playListService;
    public PlayListController(ILogger<BaseApiController> logger, CrudPlayListService playListService) : base(logger)
    {
        _playListService = playListService;
    }

    [HttpGet("list")]
    [Produces(typeof(IReadOnlyList<PlayListListItem>))]
    public async Task<ActionResult<PlayListListItem>> GetAllPlayLists(Guid blogId)
    {
        var result = await _playListService.GetPlayListsByBlogId(blogId);
        if (result.IsFailure)
            return BadRequest(result.Errors.ToValidationProblem());
        return Ok(result.Value);
    }

    //TODO возможно сюда перенести
    //[HttpGet("availableVideos")]
    //[AuthFilter(Roles.Blogger)]
    //[Produces(typeof(IReadOnlyList<PlayListViewModel>))]
    //public async Task<IActionResult> GetAvailablePostsToPlayList(IEnumerable<Guid> playlistId)
    //{
    //    var result = await _playListService.GetAvailablePostsToPlayListByIdAsync(playlistId);

    //    if (result.IsFailure)
    //        return BadRequest(result.Errors);

    //    return Ok(result.Value);
    //}


    [HttpGet("item/{id:guid}")]
    [Produces(typeof(PlayListListItem))]
    public async Task<ActionResult<PlayListListItem>> GetPlayList(Guid id)
    {
        var playList = await _playListService.GetPlayListAsync(id);

        if (playList.IsFailure)
            return BadRequest(playList.Errors.ToValidationProblem());
        return Ok(playList.Value);
    }

    [HttpGet("item/{id:guid}/postList")]
    [Produces(typeof(PlayListListItem))]
    public async Task<ActionResult<PagedListViewModel<PostCommonModel>>> GetPlayList(Guid id, int page, int pageSize)
    {
        var playList = await _playListService.GetPlayListPostPagedAsync(id, page, pageSize);
        return Ok(playList);
    }

    [HttpPost("create")]
    [AuthFilter(Roles.User)]
    [Produces(typeof(PlayListListItem))]
    public async Task<ActionResult<PlayListListItem>> CreatePlayList([FromForm] CreatePlayListRequest form)
    {
        var result = await _playListService.CreatePlayListAsync(form);
        if (result.IsFailure)
            return BadRequest(result.Errors.ToValidationProblem());
        return Ok(result.Value);
    }

    //[HttpPost("update")]
    //[Authorize]
    //[Produces(typeof(PlayListDetailViewModel))]
    //public async Task<IActionResult> UpdatePlayList([FromBody] PlayListUpdateRequest form)
    //{
    //    var result = await _playListService.UpdatePlayListCommonDataAsync(form);
    //    if (result.IsFailure)
    //        return BadRequest(result.Errors);
    //    return Ok(result.Value);
    //}

    [HttpPost("addVideo")]
    [AuthFilter(Roles.User)]
    [Produces(typeof(PlayListListItem))]
    public async Task<ActionResult<PlayListListItem>> AddVideoToPlayList([FromBody] PlayListItemAddRequest form)
    {
        var result = await _playListService.AddVideoAsync(form);
        if (result.IsFailure)
            return BadRequest(result.Errors.ToValidationProblem());
        return Ok(result.Value);
    }

    [HttpPost("updatePositions")]
    [AuthFilter(Roles.User)]
    [Produces(typeof(PlayListListItem))]
    public async Task<ActionResult<PlayListListItem>> UpdatePostPositions([FromBody] ChangePostPositionRequest form)
    {
        var result = await _playListService.ChangePostPositionAsync(form);
        if (result.IsFailure)
            return BadRequest(result.Errors.ToValidationProblem());
        return Ok(result.Value);
    }

    [HttpPost("removePlaylist/{id:guid}")]
    [AuthFilter(Roles.User)]
    [Produces(typeof(PlayListListItem))]
    public async Task<ActionResult<PlayListListItem>> RemovePlayList(Guid id)
    {
        var result = await _playListService.RemovePlayListAsync(id);
        if (result.IsFailure)
            return BadRequest(result.Errors.ToValidationProblem());
        return Ok();
    }

    [HttpPost("removeVideo")]
    [AuthFilter(Roles.User)]
    [Produces(typeof(PlayListListItem))]
    public async Task<ActionResult<PlayListListItem>> RemoveVideoFromPlayList([FromBody] PlayListItemRemoveRequest form)
    {
        var result = await _playListService.RemoveVideoAsync(form);
        if (result.IsFailure)
            return BadRequest(result.Errors.ToValidationProblem());
        return Ok(result.Value);
    }

    //TODO Возможно будет потом
    //[HttpPost("loadThumbnail")]
    //[AuthFilter(Roles.User)]
    //public async Task<ActionResult<string>> UploadThumbnail([FromForm] IFormFile thumbnail)
    //{
    //    var user = await _currentUserService.GetCurrentUserAsync();
    //    var thumbnailId = await _fileStorage.PutFileAsync(user.BlogId, $"playListThumbnails/{GuidService.GetNewGuid()}", thumbnail.OpenReadStream());
    //    var url = await _fileStorage.GetFileUrlAsync(user.UserId, thumbnailId);
    //    return Ok(new
    //    {
    //        ThumbnailId = thumbnailId,
    //        ThumbnailUrl = url
    //    });
    //}
}
