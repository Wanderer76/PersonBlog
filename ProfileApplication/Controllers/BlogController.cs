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

namespace ProfileApplication.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BlogController : BaseController
    {
        private readonly IPostService _postService;
        private readonly IBlogService _blogService;
        public BlogController(ILogger<BaseController> logger, IPostService postService, IBlogService blogService) : base(logger)
        {
            _postService = postService;
            _blogService = blogService;
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
            var result = await _postService.CreatePost(new PostCreateDto
            {
                UserId = userId,
                Type = PostType.Video,
                Text = form.Text,
                Title = form.Title,
                Video = form.Video,
                Photos = form.Files,
            });

            return Ok(result);
        }

        //public async Task<IActionResult> EditPost([FromBody] PostCreateForm form)
        //{
        //    return Ok();
        //}

        [HttpDelete("/post/delete/{id:guid}")]
        public async Task<IActionResult> DeletePost(Guid id)
        {
            await _postService.RemovePostByIdAsync(id);
            return Ok();
        }

        [HttpPut("/post/uploadChunk")]
        public async Task<IActionResult> UploadVideoChunk()
        {
            return Ok();
        }


        [HttpGet("video/chunks")]
        public async Task<IActionResult> GetVideoChunk(Guid postId)
        {
            if (!await _postService.HasVideoExistByPostIdAsync(postId))
            {
                return NotFound();
            }
            const int ChunkSize = 1024 * 1024 * 4;
            var (startPosition, endPosition) = Request.GetHeaderRangeParsedValues(ChunkSize);
            var fileMetadata = await _postService.GetVideoFileMetadataByPostIdAsync(postId);
            using var stream = new MemoryStream();
            await _postService.GetVideoChunkStreamByPostIdAsync(postId, startPosition, endPosition, stream);
            var sendSize = endPosition < fileMetadata.Length - 1 ? endPosition : fileMetadata.Length - 1;
            FillHeadersForVideoStreaming(startPosition, fileMetadata.Length, stream.Length, sendSize, fileMetadata.ContentType);
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

        private void FillHeadersForVideoStreaming(long startPosition, long originalFileSize, long streamLength, long sendSize, string contentType)
        {
            Response.StatusCode = StatusCodes.Status206PartialContent;
            Response.Headers["Accept-Ranges"] = "bytes";
            Response.Headers["Content-Range"] = $"bytes {startPosition}-{sendSize}/{originalFileSize}";
            Response.Headers["Content-Length"] = $"{startPosition + streamLength}";
            Response.ContentType = contentType;
        }
    }
}
