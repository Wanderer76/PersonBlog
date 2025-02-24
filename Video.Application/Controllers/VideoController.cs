using FileStorage.Service.Service;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Profile.Service.Models;
using Profile.Service.Models.Blog;
using Profile.Service.Models.File;
using Shared.Services;
using Video.Service.Interface;

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

        private const string postManifest = "api/Post/manifest";
        private const string detailPost = "api/Post/detail";
        private const string commonBlog = "api/Blog/blogByPost";

        public VideoController(ILogger<VideoController> logger, IFileStorageFactory factory, IReactionService videoService, IHttpClientFactory httpClientFactory)
            : base(logger)
        {
            storage = factory.CreateFileStorage();
            _videoService = videoService;
            _httpClientFactory = httpClientFactory;
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
            var fileName = file ?? (await _httpClientFactory.CreateClient("Profile").GetFromJsonAsync<FileMetadataModel>($"{postManifest}/{postId}"))!.ObjectName;
            var result = new MemoryStream();
            await storage.ReadFileAsync(postId, fileName, result);
            result.Position = 0;
            return File(result, HLSType);
        }

        [HttpGet("video/v2/{postId}/chunks/{file}/{segment}")]
        public async Task<IActionResult> GetVideoSegment(Guid postId, string file, string segment)
        {
            HttpContext.TryGetUserFromContext(out var userId);
            var result = new MemoryStream();
            await storage.ReadFileAsync(postId, $"{file}/{segment}", result);
            result.Position = 0;
            return File(result, HLSType);
        }

        [HttpGet("video/{postId:guid}")]
        public async Task<IActionResult> GetVideoData(Guid postId)
        {
            HttpContext.TryGetUserFromContext(out var userId);
            using var client = _httpClientFactory.CreateClient("Profile");
            var post = client.GetFromJsonAsync<PostDetailViewModel>($"{detailPost}/{postId}");
            var blog = client.GetFromJsonAsync<BlogModel>($"{commonBlog}/{postId}");
            try
            {
                await Task.WhenAll([post, blog]).ConfigureAwait(false);
                return Ok(new
                {
                    Post = await post.ConfigureAwait(false),
                    Blog = await blog.ConfigureAwait(false),
                    Comment = new List<string>()
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return BadRequest(ex.Message);
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
    }
}
