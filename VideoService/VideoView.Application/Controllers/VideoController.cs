using Blog.Domain.Services.Models;
using Blog.Service.Models.Blog;
using FileStorage.Service.Service;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using Shared.Services;
using System.Security.AccessControl;
using Video.Service.Interface;
using VideoView.Application.Api;
using VideoView.Application.Services;
using ViewReacting.Domain.Models;

namespace VideoView.Application.Controllers;

[ApiController]
[Route("[controller]")]
public class VideoController : BaseController
{
    private const string HLSType = "application/x-mpegURL";
    private readonly IFileStorage storage;
    private readonly IReactionService _videoService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICacheService _cache;

    public VideoController(ILogger<VideoController> logger, IFileStorageFactory factory, IReactionService videoService, IHttpClientFactory httpClientFactory, ICacheService cache)
        : base(logger)
    {
        storage = factory.CreateFileStorage();
        _videoService = videoService;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }
    

    [HttpGet("video/v2/{postId}/chunks/{*file}")]
    public async Task<IActionResult> GetVideoSegmentsOrManifest(Guid postId, string file)
    {
        if (file.EndsWith("playlist.m3u8"))
        {
            var playlistParsed = await _cache.GetCachedDataAsync<string>(file);
            if (playlistParsed == null)
            {
                playlistParsed = await storage.ProcessManifestAsync(postId, file);
                await _cache.SetCachedDataAsync(file, playlistParsed, TimeSpan.FromMinutes(15));
            }

            return Content(playlistParsed, HLSType);
        }
        else
        {
            var result = new MemoryStream();
            await storage.ReadFileAsync(postId, file!, result);
            result.Position = 0;
            return File(result, HLSType);
        }
    }

    [HttpGet("video/{postId:guid}")]
    public async Task<IActionResult> GetPostData(Guid postId)
    {
        try
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var hasUser = HttpContext.TryGetUserFromContext(out var userId);

            var session = GetUserSession();
            var userInfoCache = session == null ? null : await _cache.GetCachedDataAsync<UserSession>(GetSessionKey(session!));

            var post = _httpClientFactory.GetPostDetailViewAsync(postId);
            var blog = _httpClientFactory.GetBlogModelAsync(postId);
            var userInfo = _httpClientFactory.GetUserViewInfoAsync(postId, userId, remoteIp!);

            await Task.WhenAll(post, blog, userInfo).ConfigureAwait(false);

            var postResult = post.Result;
            var blogResult = blog.Result;
            var userResult = userInfo.Result;

            var result = new VideoDataViewModel(
                postResult.IsFailure ? null : postResult.Value,
                blogResult.IsFailure ? null : blogResult.Value,
                userResult.IsFailure ? null : userResult.Value,
                []);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest(ex);
        }
    }

    [HttpPost("setView")]
    [Authorize]
    public async Task<IActionResult> SetViewToVideo([FromBody] SetViewRequest viewRequest)
    {
        HttpContext.TryGetUserFromContext(out var userId);

        await _videoService.SetViewToPost(new ViewReacting.Domain.Events.VideoViewEvent
        {
            UserId = userId.Value,
            IsCompleteWatch = viewRequest.IsComplete,
            PostId = viewRequest.PostId,
            WatchedTime = viewRequest.Time,
        });
        return Ok();
    }

    [HttpPost("setReaction/{postId:guid}")]
    public async Task<IActionResult> SetReactionToVideo(Guid postId, bool? isLike)
    {
        HttpContext.TryGetUserFromContext(out var userId);
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _videoService.SetReactionToPost(new ReactionCreateModel
        {
            IsLike = isLike,
            PostId = postId,
            RemoteIp = remoteIp,
            UserId = userId
        });

        //var session = GetUserSession();
        //if (session != null)
        //{
        //    var userSession = await _cache.GetCachedDataAsync<UserSession>(GetSessionKey(session!));
        //    if (userSession != null)
        //    {
        //        var postViewed = userSession.PostViews.Where(x => x.PostId == postId).FirstOrDefault();
        //        if (postViewed != null)
        //        {
        //            postViewed.IsViewed = true;
        //            postViewed.IsLike = isLike;
        //            await _cache.SetCachedDataAsync(GetSessionKey(session), userSession, TimeSpan.FromMinutes(10));
        //        }
        //    }
        //}

        return Ok();
    }
}

public class SetViewRequest
{
    public Guid PostId { get; set; }
    public double Time { get; set; }
    public bool IsComplete { get; set; }
}

internal record VideoDataViewModel(PostDetailViewModel? Post, BlogUserInfoViewModel? Blog, HistoryViewItem? UserPostInfo, List<string> Comment);