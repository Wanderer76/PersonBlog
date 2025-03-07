using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Profile.Domain.Entities;
using Profile.Service.Models;
using Profile.Service.Models.File;
using Profile.Service.Models.Post;
using Profile.Service.Services;
using ProfileApplication.Models;
using Shared.Services;

namespace ProfileApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostController : BaseController
    {
        private readonly IPostService _postService;
        private readonly IUserPostService _userPostService;
        private readonly IVideoService _videoService;
        public PostController(ILogger<PostController> logger, IPostService postService, IUserPostService userPostService, IVideoService videoService) : base(logger)
        {
            _postService = postService;
            _userPostService = userPostService;
            _videoService = videoService;
        }

        [HttpGet("manifest/{postId:guid}")]
        [Produces(typeof(FileMetadataModel))]
        public async Task<IActionResult> GetVideoFileMetadataByPostIdAsync(Guid postId)
        {
            var result = await _postService.GetVideoFileMetadataByPostIdAsync(postId);
            return Ok(result);
        }

        [HttpGet("detail/{postId:guid}")]
        [Produces(typeof(PostDetailViewModel))]
        public async Task<IActionResult> GetDetailPostByIdAsync(Guid postId)
        {
            var result = await _postService.GetDetailPostByIdAsync(postId);
            return Ok(result);
        }

        [HttpGet("userInfo/{postId:guid}")]
        [Produces(typeof(UserViewInfo))]
        public async Task<IActionResult> GetDetailPostByIdAsync(Guid postId, Guid? userId, string? address)
        {
            var result = await _userPostService.GetUserViewPostInfoAsync(postId, userId, address);
            return Ok(result);
        }

        [HttpPost("setReaction/{postId:guid}")]
        public async Task<IActionResult> SetReactionToVideo(Guid postId, bool? isLike)
        {
            HttpContext.TryGetUserFromContext(out var userId);
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _postService.SetReactionToPost(new ReactionCreateModel
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

        [HttpGet("list")]
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

        [HttpPost("create")]
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

        [HttpPost("edit")]
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

        [HttpDelete("delete/{id:guid}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(Guid id)
        {
            await _postService.RemovePostByIdAsync(id);
            return Ok();
        }

        [HttpPost("uploadChunk")]
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
    }
}
