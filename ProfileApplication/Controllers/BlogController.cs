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
        //[Authorize]
        public async Task<IActionResult> GetVideoChunk(Guid postId)
        {
            const long ChunkSize = 1024 * 1024 * 4;
            var range = Request.Headers["Range"].ToString();

            //    var userId = Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d1e"); // HttpContext.GetUserFromContext();
            //  var rangeArray = range.Split(new char[] { '=', '-' });

            var dashIndex = range.IndexOf('-');
            var startPosition = long.Parse(range.Substring(6, dashIndex - 6).ToString());
            var endPosition = startPosition + ChunkSize;
            var endRangeString = range.Substring(dashIndex + 1);
            if (!string.IsNullOrWhiteSpace(endRangeString))
            {
                endPosition = long.Parse(endRangeString);
            }


            var originalFileSize = await _context.Get<VideoMetadata>()
                .Where(x => x.FileId == Guid.Parse("5ce1c7bb-d7e7-497c-8a20-2b8c503d4426"))
                .Select(x => x.Length)
                .FirstAsync();

            var buffer = new byte[ChunkSize];

            using var outputStream = Response.Body;
            //var chunkSize = (int)await _postService.GetVideoStream(postId, startPosition, buffer.Length, buffer);

            var post = await _context.Get<Post>()
                        .Select(x => new
                        {
                            x.Id,
                            x.Blog.ProfileId,
                            x.FileId
                        })
                        .FirstAsync(x => x.Id == postId);

            var storage = _fileStorageFactory.CreateFileStorage();
            using var stream = new MemoryStream();
            var chunkSize = (int)await storage.ReadFileByChunksAsync(post.ProfileId, post.FileId.Value, startPosition, buffer.Length, stream);



            var fileSize = endPosition < originalFileSize - 1 ? endPosition : originalFileSize - 1;

            Response.StatusCode = StatusCodes.Status206PartialContent;
            Response.Headers["Accept-Ranges"] = "bytes";
            Response.Headers["Content-Range"] = $"bytes {startPosition}-{fileSize}/{originalFileSize}";
            Response.Headers["Content-Length"] = $"{(startPosition + chunkSize)}";
            Response.ContentType = "video/mp4";
            await outputStream.WriteAsync(stream.GetBuffer(), 0, (int)stream.Length);
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

    }

    public class PostCreateForm
    {
        public string Title { get; set; }
        public string? Text { get; set; }
        public PostType Type { get; set; }
        public IFormFile? Video { get; set; }
    }
}
