using Blog.Domain.Services.Models;
using Blog.Service.Models.Blog;
using FileStorage.Service.Service;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using Shared.Services;
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICacheService _cache;

    public VideoController(ILogger<VideoController> logger, IFileStorageFactory factory, IHttpClientFactory httpClientFactory, ICacheService cache)
        : base(logger)
    {
        storage = factory.CreateFileStorage();
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
        var client = _httpClientFactory.CreateClient("Reacting");
        foreach (var i in HttpContext.Request.Headers)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(i.Key, i.Value.ToArray());
            
        }
        var result = await client.PostAsJsonAsync("Reaction/setView", viewRequest);
        if (!result.IsSuccessStatusCode)
            return BadRequest(result.Content);
        return Ok();
    }

    [HttpPost("setReaction/{postId:guid}")]
    public async Task<IActionResult> SetReactionToVideo(Guid postId, bool? isLike)
    {
        var client = _httpClientFactory.CreateClient("Reacting");
        foreach (var i in HttpContext.Request.Headers)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(i.Key, i.Value.ToArray());
        }
        var result = await client.PostAsync($"Reaction/setReaction/{postId}?isLike={isLike}", null);
        if (!result.IsSuccessStatusCode)
            return BadRequest(result.Content);
        return Ok();
    }
}

internal record VideoDataViewModel(PostDetailViewModel? Post, BlogUserInfoViewModel? Blog, HistoryViewItem? UserPostInfo, List<string> Comment);