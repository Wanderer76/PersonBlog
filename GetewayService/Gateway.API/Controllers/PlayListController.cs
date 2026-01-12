using Authentication.Contract.Constants;
using Blog.Contracts;
using Blog.Contracts.Models;
using Infrastructure.Extensions;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using PlayListService.Services.Models;
using PlayListService.Services.Services;
using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace Gateway.API.Controllers;

public class PlayListController : BaseApiController
{
    private readonly IPlayListService _playListService;
    private readonly BlogApiClient blogApiClient;

    public PlayListController(ILogger<BaseApiController> logger, IPlayListService playListService, BlogApiClient blogApiClient) : base(logger)
    {
        _playListService = playListService;
        this.blogApiClient = blogApiClient;
    }

    [HttpGet("item/{id:guid}")]
    public async Task<ActionResult<PlayListWithPostsViewModel>> GetPlayListViewModel(Guid id)
    {
        var playListTask = _playListService.GetPlayListAsync(id);
        var postsPageTask = _playListService.GetPlayListPostPagedAsync(id, 1, 20);
        await Task.WhenAll([playListTask, postsPageTask]);

        var playList = await playListTask;
        var postsPage = await postsPageTask;

        if (playList.IsFailure)
        {
            return BadRequest(playList.Errors.ToValidationProblem());
        }

        return Ok(new PlayListWithPostsViewModel(playList.Value, postsPage));
    }

    [HttpGet("list")]
    [Produces(typeof(IReadOnlyList<PlayListListItem>))]
    public async Task<ActionResult<IReadOnlyList<PlayListListItem>>> GetAllPlayLists([Required] Guid blogId)
    {
        var result = await _playListService.GetPlayListsByBlogIdAsync(blogId);

        if (result.IsFailure)
            return BadRequest(result.Errors.ToValidationProblem());

        return Ok(result.Value);
    }
    
    [HttpGet("availableVideos")]
    [Produces(typeof(IReadOnlyList<PostCommonModel>))]
    public async Task<ActionResult<IReadOnlyList<PlayListListItem>>> GetAllPlayLists()
    {
        var result = await blogApiClient.GetCurrentUserPostListAsync();

        if (result.IsFailure)
            return BadRequest(result.Errors.ToValidationProblem());

        return Ok(result.Value);
    }

    [HttpPost("create")]
    [AuthFilter(Roles.User, Roles.Blogger)]
    public async Task<ActionResult<PlayListWithPostsViewModel>> CreatePlayList([FromBody] CreatePlayListRequest form)
    {
        var result = await _playListService.CreatePlayListAsync(form);
        if (result.IsFailure)
            return BadRequest(result.Errors.ToValidationProblem());

        var postsPage = await _playListService.GetPlayListPostPagedAsync(result.Value.Id, 1, form.PostIds.Count);
        return Ok(new PlayListWithPostsViewModel(result.Value, postsPage));
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
    [AuthFilter(Roles.User, Roles.Blogger)]
    public async Task<ActionResult<PlayListListItem>> AddVideoToPlayList([FromBody] PlayListItemAddRequest form)
    {
        var result = await _playListService.AddVideoAsync(form);
        if (result.IsFailure)
            return BadRequest(result.Errors.ToValidationProblem());
        return Ok(result.Value);
    }

    [HttpPost("updatePositions")]
    [AuthFilter(Roles.User, Roles.Blogger)]
    public async Task<ActionResult<PlayListListItem>> UpdatePostPositions([FromBody] ChangePostPositionRequest form)
    {
        var result = await _playListService.ChangePostPositionAsync(form);
        if (result.IsFailure)
            return BadRequest(result.Errors.ToValidationProblem());
        return Ok(result.Value);
    }

    [HttpPost("removePlaylist/{id:guid}")]
    [AuthFilter(Roles.User, Roles.Blogger)]
    public async Task<ActionResult> RemovePlayList(Guid id)
    {
        var result = await _playListService.RemovePlayListAsync(id);
        if (result.IsFailure)
            return BadRequest(result.Errors.ToValidationProblem());
        return Ok();
    }

    [HttpPost("removeVideo")]
    [AuthFilter(Roles.User,Roles.Blogger)]
    public async Task<ActionResult> RemoveVideoFromPlayList([FromBody] PlayListItemRemoveRequest form)
    {
        var result = await _playListService.RemoveVideoAsync(form);
        if (result.IsFailure)
            return BadRequest(result.Errors.ToValidationProblem());
        return Ok();
    }
}

public record PlayListWithPostsViewModel(
    PlayListListItem PlayList,
    PagedListViewModel<PostCommonModel> PostPage
);