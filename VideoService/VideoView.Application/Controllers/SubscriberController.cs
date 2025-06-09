using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VideoView.Application.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriberController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public SubscriberController(ILogger<BaseController> logger, IHttpClientFactory httpClientFactory) : base(logger)
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
                foreach (var i in HttpContext.Request.Headers)
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(i.Key, i.Value.ToArray());

                }
                await client.PostAsync($"Subscriber/subscribe/{blogId}",null);
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
                foreach (var i in HttpContext.Request.Headers)
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(i.Key, i.Value.ToArray());

                }
                await client.PostAsync($"Subscriber/unsubscribe/{blogId}", null);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
