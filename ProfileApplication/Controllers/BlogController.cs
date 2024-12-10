using FileStorage.Service.Service;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Profile.Service.Interface;
using Profile.Service.Models.Post;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;
using System;
using System.Net.Mime;

namespace ProfileApplication.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BlogController : BaseController
    {
        private readonly IPostService _postService;
        private readonly IFileStorageFactory _fileStorageFactory;
        private readonly IReadWriteRepository<IProfileEntity> _context;
        public BlogController(ILogger<BaseController> logger, IPostService postService, IReadWriteRepository<IProfileEntity> context, IFileStorageFactory fileStorageFactory) : base(logger)
        {
            _postService = postService;
            _context = context;
            _fileStorageFactory = fileStorageFactory;
        }

        //public async Task<IActionResult> CreateBlog([FromBody] BlogCreateForm form)
        //{
        //    return Ok();
        //}

        //public async Task<IActionResult> DeleteBlog()
        //{
        //    return Ok();
        //}

        //public async Task<IActionResult> EditBlog([FromBody] BlogCreateForm form)
        //{
        //    return Ok();
        //}

        //Текстовые посты, можно добавлять картинки
        [HttpPost("post/create")]
        [Authorize]
        public async Task<IActionResult> AddPostToBlog([FromForm] PostCreateForm form)
        {

            var userId = HttpContext.GetUserFromContext();
            var result = await _postService.CreatePost(new PostCreateDto
            {
                UserId = userId,
                Type = PostType.Media,
                Text = form.Text,
                Title = form.Title,
                Video = form.Video,
            });

            return Ok(result);
        }

        //public async Task<IActionResult> EditPost([FromBody] PostCreateForm form)
        //{
        //    return Ok();
        //}

        //public async Task<IActionResult> DeletePost(Guid id)
        //{
        //    return Ok();
        //}

        //public async Task<IActionResult> UploadVideo(IFormFile file)
        //{
        //    return Ok();
        //}


        [HttpGet("video/chunks")]
        public async Task<IActionResult> GetVideoChunk(Guid postId)
        {
            const int ChunkSize = 1024 * 1024 * 4;

            var (startPosition, endPosition) = Request.GetHeaderRangeParsedValues(ChunkSize);
            var fileMetadata = await _postService.GetFileMetadataByPostId(postId);
            using var stream = new MemoryStream();
            await _postService.GetVideoChunkStreamByPostId(postId, startPosition, endPosition, stream);
            var sendSize = endPosition < fileMetadata.Length - 1 ? endPosition : fileMetadata.Length - 1;
            FillHeadersForVideoStreaming(startPosition, fileMetadata.Length, stream, sendSize, fileMetadata.ContentType);
            using var outputStream = Response.Body;
            await outputStream.WriteAsync(stream.GetBuffer().AsMemory(0, (int)stream.Length));
            return Ok();
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

        private void FillHeadersForVideoStreaming(long startPosition, long originalFileSize, Stream stream, long sendSize, string contentType)
        {
            Response.StatusCode = StatusCodes.Status206PartialContent;
            Response.Headers["Accept-Ranges"] = "bytes";
            Response.Headers["Content-Range"] = $"bytes {startPosition}-{sendSize}/{originalFileSize}";
            Response.Headers["Content-Length"] = $"{startPosition + stream.Length}";
            Response.ContentType = contentType;
        }
    }

    public class PostCreateForm
    {
        public string Title { get; set; }
        public string? Text { get; set; }
        public PostType Type { get; set; }
        public IFormFile? Video { get; set; }
    }
}
