using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Profile.Domain.Entities;
using Profile.Service.Interface;
using Profile.Service.Models.Post;
using ProfileApplication.Models;
using Shared.Services;
using Shared.Utils;
using Profile.Service.Models.Blog;
using Profile.Service.Models.File;
using Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using FileStorage.Service.Service;
using System.IO;

namespace ProfileApplication.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BlogController : BaseController
    {
        private readonly IPostService _postService;
        private readonly IBlogService _blogService;
        private readonly IVideoService _videoService;
        private readonly IReadWriteRepository<IProfileEntity> _context;
        private readonly IFileStorageFactory fileStorageFactory;
        private static IFileStorage storage;
        public BlogController(ILogger<BaseController> logger, IPostService postService, IBlogService blogService, IVideoService videoService, IReadWriteRepository<IProfileEntity> context, IFileStorageFactory fileStorageFactory) : base(logger)
        {
            _postService = postService;
            _blogService = blogService;
            _videoService = videoService;
            _context = context;
            storage = fileStorageFactory.CreateFileStorage();
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateBlog([FromBody] BlogCreateForm form)
        {
            var userId = HttpContext.GetUserFromContext();
            try
            {
                var result = await _blogService.CreateBlogAsync(new BlogCreateDto(userId, form.Title, form.Description, form.PhotoUrl));
                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("detail")]
        [Authorize]
        public async Task<IActionResult> GetBlogDetail()
        {
            var userId = HttpContext.GetUserFromContext();
            var result = await _blogService.GetBlogByUserIdAsync(userId);
            return Ok(result);
        }

        [HttpGet("posts/list")]
        public async Task<IActionResult> GetBlogPostPagedList(Guid blogId, int page, int limit)
        {
            var result = await _postService.GetPostsByBlogIdPagedAsync(blogId, page, limit);
            return Ok(result);
        }

        //[HttpPut("edit")]
        //public async Task<IActionResult> EditBlog([FromBody] BlogCreateForm form)
        //{
        //    return Ok();
        //}

        [HttpPost("post/create")]
        [Authorize]
        public async Task<IActionResult> AddPostToBlog([FromForm] PostCreateForm form)
        {
            var userId = HttpContext.GetUserFromContext();
            var result = await _postService.CreatePostAsync(new PostCreateDto
            {
                UserId = userId,
                Type = PostType.Video,
                Text = form.Text,
                Title = form.Title,
                Video = form.Video,
                Photos = form.Files,
                IsPartial = form.IsPartial,
            });

            return Ok(result);
        }

        //public async Task<IActionResult> EditPost([FromBody] PostCreateForm form)
        //{
        //    return Ok();
        //}

        [HttpDelete("/post/delete/{id:guid}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(Guid id)
        {
            await _postService.RemovePostByIdAsync(id);
            return Ok();
        }

        [HttpPost("/post/uploadChunk")]
        [Authorize]
        public async Task<IActionResult> UploadVideoChunk([FromForm] UploadVideoChunkForm uploadVideoChunk)
        {
            var userId = HttpContext.GetUserFromContext();
            try
            {
                var metadata = await _videoService.GetOrCreateVideoMetadata(uploadVideoChunk.ToUploadVideoChunkModel());

                using var data = uploadVideoChunk.ChunkData.OpenReadStream();
                await _postService.UploadVideoChunkAsync(new UploadVideoChunkDto
                {
                    UserId = userId,
                    ChunkNumber = uploadVideoChunk.ChunkNumber,
                    TotalChunkCount = uploadVideoChunk.TotalChunkCount,
                    ChunkData = data,
                    PostId = uploadVideoChunk.PostId
                });
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
            return Ok();

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

        [HttpGet("video/v2/{postId}/{resolution}/chunks/{file}")]
        public async Task<IActionResult> GetVideoChunk2(Guid postId, string? file, int resolution)
        {
            if (!await _postService.HasVideoExistByPostIdAsync(postId))
            {
                return NotFound();
            }
            const int ChunkSize = 1024 * 1024 * 1;
            var fileMetadata = await _postService.GetVideoFileMetadataByPostIdAsync(postId);

            var fileName = file ?? fileMetadata.ObjectName;

            var result = new MemoryStream();
            await storage.ReadFileAsync(postId,
                              fileName.Contains(".ts") ? $"{fileMetadata.Id}_{resolution}/{fileName}" : fileName,
                              result);

            result.Position = 0;
            return File(result, "application/x-mpegURL");
        }

        [HttpGet("video/v2/{postId}/{resolution}/chunks/{file}/{playlist}")]
        public async Task<IActionResult> GetVideoChunk3(Guid postId, string? file, int resolution, string playlist)
        {
            const int ChunkSize = 1024 * 1024 * 1;

            var result = new MemoryStream();
            await storage.ReadFileAsync(postId,
                              $"{file}/{playlist}",
                              result);

            result.Position = 0;
            return File(result, "application/x-mpegURL");
        }


        //public async Task<IActionResult> DeleteVideo(Guid id)
        //{
        //    return Ok();
        //}

        ////Получение контента своего блога
        //public async Task<IActionResult> GetPagedContent(int page, int limit)
        //{
        //    return Ok();
        //}

        private void FillHeadersForVideoStreaming(long startPosition, long originalFileSize, long streamLength, long sendSize, string contentType)
        {
            Response.StatusCode = StatusCodes.Status206PartialContent;
            Response.Headers["Accept-Ranges"] = "bytes";
            Response.Headers["Content-Range"] = $"bytes {startPosition}-{sendSize}/{originalFileSize}";
            Response.Headers["Content-Length"] = $"{streamLength}";
            Response.ContentType = contentType;
        }
    }
}
