using FileStorage.Service.Service;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Profile.Service.Interface;
using Shared.Utils;

namespace Video.Application.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VideoController : BaseController
    {
        private const string HLSType = "application/x-mpegURL";
        private readonly IFileStorage storage;
        private readonly IPostService _postService;

        public VideoController(ILogger<BaseController> logger, IFileStorageFactory factory, IPostService postService)
            : base(logger)
        {
            storage = factory.CreateFileStorage();
            _postService = postService;
        }

        /// <summary>
        /// Пригодится разве что для .mp4
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        //TODO добавить кэш
        [HttpGet("video/chunks")]
        public async Task<IActionResult> GetVideoChunk(Guid postId, int resolution = 480)
        {
            if (!await _postService.HasVideoExistByPostIdAsync(postId))
            {
                return NotFound();
            }
            const int ChunkSize = 1024 * 1024 * 1;
            var fileMetadata = await _postService.GetVideoFileMetadataByPostIdAsync(postId, resolution);

            var (startPosition, endPosition) = Request.GetHeaderRangeParsedValues(ChunkSize);
            using var stream = new MemoryStream();
            await _postService.GetVideoChunkStreamByPostIdAsync(postId, fileMetadata.Id, startPosition, endPosition, stream);
            var sendSize = endPosition < fileMetadata.Length - 1 ? endPosition : fileMetadata.Length - 1;
            FillHeadersForVideoStreaming(startPosition, fileMetadata.Length, stream.Length, sendSize, fileMetadata.ContentType);
            using var outputStream = Response.Body;
            await outputStream.WriteAsync(stream.GetBuffer().AsMemory(0, (int)stream.Length));
            return Ok();
        }

        [HttpGet("video/v2/{postId}/chunks/{file}")]
        public async Task<IActionResult> GetVideoSegmentsOrManifest(Guid postId, string? file)
        {
            var fileName = file ?? (await _postService.GetVideoFileMetadataByPostIdAsync(postId)).ObjectName;
            var result = new MemoryStream();
            await storage.ReadFileAsync(postId, fileName, result);
            result.Position = 0;
            return File(result, HLSType);
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
        public async Task<IActionResult> GetVideoData(Guid postId)
        {
            var post = await _postService.GetDetailPostByIdAsync(postId);

            return Ok(new
            {
                Post = post,
                Comment = new List<string>()
            });
        }
    }
}
