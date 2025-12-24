using Blog.Service.Models.Blog;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Profile.Domain.Models;
using Shared.Models;

namespace Gateway.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriberController : BaseApiController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public SubscriberController(ILogger<BaseApiController> logger, IHttpClientFactory httpClientFactory) : base(logger)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("subscribe/{blogId:guid}")]
        [Authorize]
        public async Task<IActionResult> SubscribeToBlog(Guid blogId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("Reacting");
                await client.PostAsync($"Subscriber/subscribe/{blogId}", null);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("unsubscribe/{blogId:guid}")]
        [Authorize]
        public async Task<IActionResult> UnSubscribeToBlog(Guid blogId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("Reacting");
                await client.PostAsync($"Subscriber/unsubscribe/{blogId}", null);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("subscriptions")]
        [Authorize]
        public async Task<IActionResult> SubscriptionsList(int page, int size)
        {
            var client = _httpClientFactory.CreateClient("Reacting");
            var subscriptions = await client.GetFromJsonAsync<PagedViewModel<SubscribeViewModel>>($"Subscriber/subscriptions?page={page}&size={size}");

            if (subscriptions.Items.Count > 0)
            {
                var blogs = await Task.WhenAll(subscriptions.Items
                    .Select(x => _httpClientFactory.CreateClient("Profile").GetFromJsonAsync<BlogModel>($"api/Blog/blog/{x.BlogId}")));
                return Ok(new PagedViewModel<BlogModel>(subscriptions.TotalPageCount, subscriptions.TotalPostsCount, blogs));
            }
            else
                return Ok(new PagedViewModel<BlogModel>(subscriptions.TotalPageCount, subscriptions.TotalPostsCount, []));
        }
    }
}
