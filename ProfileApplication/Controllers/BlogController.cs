using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Profile.Domain.Entities;
using Profile.Service.Models.Post;
using ProfileApplication.Models;
using Shared.Services;
using Profile.Service.Models.Blog;
using Profile.Service.Models.File;
using Shared.Persistence;
using FileStorage.Service.Service;
using Profile.Service.Models;
using Profile.Service.Services;

namespace ProfileApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogController : BaseController
    {
        private readonly IPostService _postService;
        private readonly IBlogService _blogService;
        private readonly IVideoService _videoService;
        public BlogController(ILogger<BaseController> logger, IPostService postService, IBlogService blogService, IVideoService videoService, IReadWriteRepository<IProfileEntity> context, IFileStorageFactory fileStorageFactory) : base(logger)
        {
            _postService = postService;
            _blogService = blogService;
            _videoService = videoService;
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateBlog([FromForm] BlogCreateForm form)
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
                Text = form.Description,
                Title = form.Title,
                Video = form.Video,
                Photos = form.Files,
                IsPartial = form.IsPartial,
            });

            return Ok(result);
        }

        [HttpPost("post/edit")]
        [Authorize]
        public async Task<IActionResult> EditPost([FromForm] PostEditForm form)
        {
            var userId = HttpContext.GetUserFromContext();

            var result = await _postService.UpdatePostAsync(new PostEditDto
            (
                form.Id,
                userId,
                form.Description,
                form.Title,
                form.PreviewId
            ));
            return Ok(result);
        }

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

        //public async Task<IActionResult> DeleteVideo(Guid id)
        //{
        //    return Ok();
        //}

        ////Получение контента своего блога
        //public async Task<IActionResult> GetPagedContent(int page, int limit)
        //{
        //    return Ok();
        //}


        [HttpGet("blogByPost/{postId:guid}")]
        [Produces(typeof(BlogModel))]
        public async Task<IActionResult> GetDetailPostByIdAsync(Guid postId)
        {
            var result = await _blogService.GetBlogByPostIdAsync(postId);
            return Ok(result);
        }
    }
}
