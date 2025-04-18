using Blog.Service.Models;
using Blog.Service.Models.Blog;
using FileStorage.Service.Service;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using Shared.Services;
using Video.Service.Interface;
using VideoView.Application.Api;

namespace VideoView.Application.Controllers
{
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

        /// <summary>
        /// Пригодится разве что для .mp4
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        //TODO добавить кэш
        //[HttpGet("video/chunks")]
        //[Obsolete]
        //public async Task<IActionResult> GetVideoChunk(Guid postId, int resolution = 480)
        //{
        //    if (!await _postService.HasVideoExistByPostIdAsync(postId))
        //    {
        //        return NotFound();
        //    }
        //    const int ChunkSize = 1024 * 1024 * 1;
        //    var fileMetadata = await _postService.GetVideoFileMetadataByPostIdAsync(postId, resolution);

        //    var (startPosition, endPosition) = Request.GetHeaderRangeParsedValues(ChunkSize);
        //    using var stream = new MemoryStream();
        //    await _postService.GetVideoChunkStreamByPostIdAsync(postId, fileMetadata.Id, startPosition, endPosition, stream);
        //    var sendSize = endPosition < fileMetadata.Length - 1 ? endPosition : fileMetadata.Length - 1;
        //    FillHeadersForVideoStreaming(startPosition, fileMetadata.Length, stream.Length, sendSize, fileMetadata.ContentType);
        //    using var outputStream = Response.Body;
        //    await outputStream.WriteAsync(stream.GetBuffer().AsMemory(0, (int)stream.Length));
        //    return Ok();
        //}

        [HttpGet("video/v2/{postId}/chunks/{file}")]
        public async Task<IActionResult> GetVideoSegmentsOrManifest(Guid postId, string? file)
        {
            try
            {
                var fileName = file;
                if (file == null)
                {
                    var result = (await _httpClientFactory.GetFileMetadataAsync(postId));
                    if (result.IsFailure)
                        return BadRequest(result.Error!.Message);

                    fileName = result.Value.ObjectName;
                }

                var data = await _cache.GetCachedDataAsync<byte[]?>(fileName!);
                if (data == null)
                {
                    var result = new MemoryStream();
                    await storage.ReadFileAsync(postId, fileName!, result);
                    result.Position = 0;
                    data = result.ToArray();
                    await _cache.SetCachedDataAsync(fileName!, data, TimeSpan.FromMinutes(5));
                }
                return File(data, HLSType);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("video/v2/{postId}/chunks/{file}/{segment}")]
        public async Task<IActionResult> GetVideoSegment(Guid postId, string file, string segment)
        {
            var result = new MemoryStream();
            await storage.ReadFileAsync(postId, $"{file}/{segment}", result);
            result.Position = 0;
            return File(result, HLSType);
        }

        [HttpGet("video/{postId:guid}")]
        public async Task<IActionResult> GetPostData(Guid postId)
        {
            try
            {
                var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                HttpContext.TryGetUserFromContext(out var userId);
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
                    postResult.Value,
                    blogResult.Value,
                    userResult.Value,
                    []);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return BadRequest(ex);
            }
        }

        [HttpPost("setView/{postId:guid}")]
        public async Task<IActionResult> SetViewToVideo(Guid postId)
        {
            HttpContext.TryGetUserFromContext(out var userId);
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _videoService.SetViewToPost(postId, userId, remoteIp);
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
}

internal record VideoDataViewModel(PostDetailViewModel? Post, BlogModel? Blog, UserViewInfo? UserPostInfo, List<string> Comment);