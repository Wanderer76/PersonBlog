using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using Shared.Services;
using ViewReacting.Domain.Models;
using ViewReacting.Domain.Services;

namespace VideoReacting.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriberController : BaseController
    {
        private readonly ISubscribeService _subscribeService;

        public SubscriberController(ILogger<SubscriberController> logger, ISubscribeService subscribeService) : base(logger)
        {
            _subscribeService = subscribeService;
        }

        [HttpPost("subscribe/{blogId:guid}")]
        [Authorize]
        public async Task<IActionResult> SubscribeToBlog(Guid blogId)
        {
            try
            {
                var userId = HttpContext.GetUserFromContext();
                await _subscribeService.SubscribeToBlogAsync(blogId);
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
                var userId = HttpContext.GetUserFromContext();
                await _subscribeService.UnSubscribeToBlogAsync(blogId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("subscriptions")]
        [Authorize]
        [Produces<PagedViewModel<SubscribeViewModel>>]
        public async Task<IActionResult> GetUserSubscriptionList(int page, int size)
        {
            try
            {
                var userId = HttpContext.GetUserFromContext();
                var result = await _subscribeService.GetUserSubscriptionListAsync(userId,page,size);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("hasSubscription/{blogId}")]
        [Produces<HasSubscriptionModel>]
        public async Task<IActionResult> HasSubscription(Guid blogId)
        {
            try
            {
                var result = await _subscribeService.CheckCurrentUserToSubscriptionAsync(blogId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
