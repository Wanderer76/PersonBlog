using FileStorage.Service.Service;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Profile.Service.Interface;
using Shared.Utils;

namespace ProfileApplication.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VideoController : BaseController
    {
        private readonly IFileStorage storage;
        private readonly IPostService _postService;

        public VideoController(ILogger<BaseController> logger, IFileStorageFactory factory, IPostService postService)
            : base(logger)
        {
            storage = factory.CreateFileStorage();
            _postService = postService;
        }

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
        public async Task<IActionResult> GetVideoChunk2(Guid postId, string? file)
        {
            if (!await _postService.HasVideoExistByPostIdAsync(postId))
            {
                return NotFound();
            }
            const int ChunkSize = 1024 * 1024 * 1;
            var fileMetadata = await _postService.GetVideoFileMetadataByPostIdAsync(postId);

            var fileName = file ?? fileMetadata.ObjectName;

            var result = new MemoryStream();
            await storage.ReadFileAsync(postId, fileName, result);

            result.Position = 0;
            return File(result, "application/x-mpegURL");
        }

        [HttpGet("video/v2/{postId}/chunks/{file}/{playlist}")]
        public async Task<IActionResult> GetVideoChunk3(Guid postId, string? file, string playlist)
        {
            const int ChunkSize = 1024 * 1024 * 1;

            var result = new MemoryStream();
            await storage.ReadFileAsync(postId,
                              $"{file}/{playlist}",
                              result);

            result.Position = 0;
            return File(result, "application/x-mpegURL");
        }


    }
}
