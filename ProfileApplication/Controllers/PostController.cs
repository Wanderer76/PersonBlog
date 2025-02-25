using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Profile.Service.Models;
using Profile.Service.Models.File;
using Profile.Service.Services;
using System.Text;

namespace ProfileApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostController : BaseController
    {
        private readonly IPostService _postService;
        private readonly IUserPostService _userPostService;
        public PostController(ILogger<BaseController> logger, IPostService postService, IUserPostService userPostService) : base(logger)
        {
            _postService = postService;
            _userPostService = userPostService;
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
        public async Task<IActionResult> GetDetailPostByIdAsync(Guid postId,Guid? userId, string?address)
        {
            var result = await _userPostService.GetUserViewPostInfoAsync(postId,userId,address);
            return Ok(result);
        }
    }
}
