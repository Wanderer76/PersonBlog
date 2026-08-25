using Blog.Contracts.Models;
using Blog.Contracts.Models.Blog;
using Gateway.API.Api;
using Gateway.API.Services;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Profile.Domain.Models;
using Shared.Services;
using System.Net;

namespace Gateway.API.Controllers;

[ApiController]
[Route("[controller]")]
public class VideoController : BaseApiController
{
    private const string HlsManifestType = "application/vnd.apple.mpegurl";
    private const string HlsSegmentType = "video/mp2t";
    private readonly IFileStorageFactory _storageFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public VideoController(ILogger<VideoController> logger, IFileStorageFactory factory, IHttpClientFactory httpClientFactory)
        : base(logger)
    {
        _storageFactory = factory;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("{blogId}/{postId}/{*file}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetVideoSegmentsOrManifest(
        Guid blogId,
        Guid postId,
        string file,
        CancellationToken cancellationToken)
    {
        if (!IsPostFile(postId, file))
        {
            return NotFound();
        }

        var accessStatus = await _httpClientFactory.CheckVideoAccessAsync(
            blogId,
            postId,
            cancellationToken);

        if (accessStatus == HttpStatusCode.NotFound || accessStatus == HttpStatusCode.Forbidden)
        {
            return NotFound();
        }

        if (accessStatus != HttpStatusCode.NoContent)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        using var storage = _storageFactory.CreateFileStorage();
        if (file.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
        {
            var playlist = await storage.ReadHlsManifestAsync(blogId, file, cancellationToken);
            return Content(playlist, HlsManifestType);
        }

        if (!file.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        Response.ContentType = HlsSegmentType;
        await storage.ReadFileAsync(blogId, file, Response.Body, cancellationToken);
        return new EmptyResult();
    }

    private static bool IsPostFile(Guid postId, string file)
    {
        if (string.IsNullOrWhiteSpace(file) || file.Contains('\\'))
        {
            return false;
        }

        var parts = file.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            && Guid.TryParse(parts[0], out var filePostId)
            && filePostId == postId
            && parts.All(part => part is not "." and not "..");
    }

    [HttpGet("video/{postId:guid}")]
    public async Task<IActionResult> GetPostData(Guid postId, bool? fromPlaylist = false)
    {
        try
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            HttpContext.TryGetUserFromContext(out var userId);

            var blog = await _httpClientFactory.GetBlogModelAsync(postId);
            var post = _httpClientFactory.GetPostDetailViewAsync(HttpContext, postId);
            var userInfo = _httpClientFactory.GetUserViewInfoAsync(postId, userId, remoteIp!, blog.IsSuccess ? blog.Value?.Id : null);

            await Task.WhenAll(post, userInfo).ConfigureAwait(false);

            var postResult = post.Result;
            var blogResult = blog;
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
            _logger.LogError(ex, "Failed to load video data for post {PostId}", postId);
            return BadRequest(ex);
        }
    }

    [HttpPost("setView")]
    [AuthFilter]
    public async Task<IActionResult> SetViewToVideo([FromBody] SetViewRequest viewRequest)
    {
        var client = _httpClientFactory.CreateClient("Reacting");
        var result = await client.PostAsJsonAsync("Reaction/setView", viewRequest);
        if (!result.IsSuccessStatusCode)
            return BadRequest(result.Content);
        return Ok();
    }

    [HttpPost("setReaction/{postId:guid}")]
    public async Task<IActionResult> SetReactionToVideo(Guid postId, bool? isLike)
    {
        var client = _httpClientFactory.CreateClient("Reacting");

        var result = await client.PostAsync($"Reaction/setReaction/{postId}?isLike={isLike}", null);
        if (!result.IsSuccessStatusCode)
            return BadRequest(result.Content);
        return Ok();
    }
}

internal record VideoDataViewModel(PostDetailViewModel? Post, BlogUserInfoViewModel? Blog, ReactionHistoryViewItem? UserPostInfo, List<string> Comment);
