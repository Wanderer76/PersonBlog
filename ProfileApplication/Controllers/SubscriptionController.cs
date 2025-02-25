using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Profile.Service.Interface;
using Shared.Services;

namespace ProfileApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : BaseController
    {
        private readonly ISubscriptionService _subscriptionService;
        public SubscriptionController(ILogger<SubscriptionController> logger, ISubscriptionService subscriptionService) : base(logger)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpPost("subscribe/{blogId:guid}")]
        [Authorize]
        public async Task<IActionResult> SubscribeToBlog(Guid blogId)
        {
            try
            {
                var userId = HttpContext.GetUserFromContext();
                await _subscriptionService.SubscribeToBlogAsync(blogId, userId);
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
                await _subscriptionService.UnSubscribeToBlogAsync(blogId, userId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //получение всего контента от блогов
        [Authorize]
        public async Task<IActionResult> GetSubscriptionsContent(int page, int offset)
        {
            return Ok();
        }
    }
}
