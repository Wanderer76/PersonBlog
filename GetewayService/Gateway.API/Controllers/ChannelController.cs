using Blog.Contracts.Models.Blog;
using Blog.Contracts;
using Blog.Contracts.Models;
using Blog.Domain.Entities;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using PlayListService.Services.Models;
using Profile.Domain.Models;
using Shared.Models;

namespace Gateway.API.Controllers
{
    public class ChannelController : GatewayApiController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PostApiClient _postApiClient;

        public ChannelController(
            ILogger<ChannelController> logger,
            IHttpClientFactory httpClientFactory,
            PostApiClient postApiClient) : base(logger)
        {
            _httpClientFactory = httpClientFactory;
            _postApiClient = postApiClient;
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
        public async Task<ActionResult<PagedListViewModel<PostCommonModelV2>>> GetChannelPosts(
            Guid channelId,
            int page,
            int size)
        {
            var posts = await _postApiClient.GetAvailablePostsByBlogIdAsync(
                channelId,
                page,
                size,
                PostType.Video);

            return Ok(posts);
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
