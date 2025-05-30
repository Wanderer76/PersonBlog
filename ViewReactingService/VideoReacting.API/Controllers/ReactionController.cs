using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Services;
using ViewReacting.Domain.Events;
using ViewReacting.Domain.Models;
using ViewReacting.Domain.Services;

namespace VideoReacting.API.Controllers
{
    [ApiController]
    public class ReactionController : BaseController
    {
        private readonly IReactionService _reactionService;

        public ReactionController(ILogger<ReactionController> logger, IReactionService videoService) : base(logger)
        {
            _reactionService = videoService;
        }

        [HttpPost("setView")]
        [Authorize]
        public async Task<IActionResult> SetViewToVideo([FromBody] SetViewRequest viewRequest)
        {
            HttpContext.TryGetUserFromContext(out var userId);

            await _reactionService.SetViewToPost(new VideoViewEvent
            {
                UserId = userId.Value,
                IsCompleteWatch = viewRequest.IsComplete,
                PostId = viewRequest.PostId,
                WatchedTime = viewRequest.Time,
            });
            return Ok();
        }


        [HttpPost("setReaction/{postId:guid}")]
        public async Task<IActionResult> SetReactionToVideo(Guid postId, bool? isLike)
        {
            HttpContext.TryGetUserFromContext(out var userId);
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _reactionService.SetReactionToPost(new ReactionCreateModel
            {
                IsLike = isLike,
                PostId = postId,
                RemoteIp = remoteIp,
                UserId = userId
            });
            return Ok();
        }
    }
}
