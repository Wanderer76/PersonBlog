using Blog.Service.Models.Blog;
using Blog.Service.Models.Post;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using PlayListService.Services.Models;
using Profile.Domain.Models;

namespace Gateway.API.Controllers
{
    public class ChannelController : BaseApiController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ChannelController(ILogger<BaseApiController> logger, IHttpClientFactory httpClientFactory) : base(logger)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("{channelId}")]
        public async Task<IActionResult> GetChannelInfo(Guid channelId)
        {
            using var client = _httpClientFactory.CreateClient("Profile");
            var blog = (await client.GetFromJsonAsync<BlogModel>($"api/Blog/blog/{channelId}"))!;
            var hasSubscription = (await _httpClientFactory.CreateClient("Reacting")
                .GetFromJsonAsync<HasSubscriptionModel>($"Subscriber/hasSubscription/{channelId}"))!;
            return Ok(new
            {
                blog.Name,
                blog.Description,
                blog.PhotoUrl,
                blog.CreatedAt,
                blog.Id,
                blog.UserId,
                blog.SubscribersCount,
                IsSubscribed = hasSubscription.HasSubscription
            });
        }

        [HttpGet("posts/{channelId}")]
        public async Task<IActionResult> GetChannelPosts(Guid channelId, int page, int size)
        {
            using var client = _httpClientFactory.CreateClient("Profile");
            var blog = await client.GetFromJsonAsync<PostPagedListViewModel>($"api/Post/list?blogId={channelId}&page={page}&limit={size}");
            return Ok(blog);
        }

        [HttpGet("playLists/{channelId}")]
        public async Task<IActionResult> GetChannelPlaylists(Guid channelId)
        {
            using var client = _httpClientFactory.CreateClient("Profile");
            var blog = await client.GetFromJsonAsync<IReadOnlyList<PlayListListItem>>($"api/PlayList/list?blogId={channelId}");
            return Ok(blog);
        }
    }
}
