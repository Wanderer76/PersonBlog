using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Profile.Service.Interface;
using Profile.Service.Models;
using Profile.Service.Models.File;

namespace ProfileApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostController : BaseController
    {
        private readonly IPostService _postService;
        public PostController(ILogger<BaseController> logger, IPostService postService) : base(logger)
        {
            _postService = postService;
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

    }
}
