using Comments.Domain.Models;
using Infrastructure.Extensions;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace VideoView.Application.Controllers
{
    public class CommentsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CommentsController(ILogger<BaseController> logger, IHttpClientFactory httpClientFactory) : base(logger)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetCommentsList(Guid postId)
        {
            using var client = _httpClientFactory.CreateClientContextHeaders("Comments", HttpContext);
            var result = await client.GetAsync($"Comments/list?postId={postId}");
            return Ok(await result.Content.ReadAsStringAsync());
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateComment(CommentCreateRequest createRequest)
        {
            using var client = _httpClientFactory.CreateClientContextHeaders("Comments", HttpContext);
            var result = await client.PostAsJsonAsync($"Comments/create",createRequest);
            return Ok(await result.Content.ReadAsStringAsync());
        }
    }
}
