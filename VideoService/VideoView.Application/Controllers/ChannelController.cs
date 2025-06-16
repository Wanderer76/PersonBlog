using Blog.Service.Models.Blog;
using Blog.Service.Models.Post;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Shared.Utils;
using ViewReacting.Domain.Models;

namespace VideoView.Application.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChannelController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ChannelController(ILogger<BaseController> logger, IHttpClientFactory httpClientFactory) : base(logger)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("{channelId}")]
        public async Task<IActionResult> GetChannelInfo(Guid channelId)
        {
            using var client = _httpClientFactory.CreateClientContextHeaders("Profile", HttpContext);
            var blog = await client.GetFromJsonAsync<BlogModel>($"api/Blog/blog/{channelId}");
            var hasSubscription = await _httpClientFactory.CreateClientContextHeaders("Reacting", HttpContext)
                .GetFromJsonAsync<HasSubscriptionModel>($"Subscriber/hasSubscription/{channelId}");
            return Ok(new
            {
                blog.Name,
                blog.Description,
                blog.PhotoUrl,
                blog.CreatedAt,
                blog.Id,
                blog.ProfileId,
                blog.SubscribersCount,
                IsSubscribed = hasSubscription.HasSubscription
            });
        }

        [HttpGet("posts/{channelId}")]
        public async Task<IActionResult> GetChannelPosts(Guid channelId, int page, int size)
        {
            using var client = _httpClientFactory.CreateClientContextHeaders("Profile", HttpContext);
            var blog = await client.GetFromJsonAsync<PostPagedListViewModel>($"api/Post/list?blogId={channelId}&page={page}&limit={size}");
            return Ok(blog);
        }
    }
}
