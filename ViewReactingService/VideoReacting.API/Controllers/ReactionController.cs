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
        private readonly IReactionService _videoService;

        public ReactionController(ILogger<ReactionController> logger, IReactionService videoService) : base(logger)
        {
            _videoService = videoService;
        }

        [HttpPost("setView")]
        //[Authorize]
        public async Task<IActionResult> SetViewToVideo([FromBody] SetViewRequest viewRequest)
        {
            HttpContext.TryGetUserFromContext(out var userId);

            await _videoService.SetViewToPost(new VideoViewEvent
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
            await _videoService.SetReactionToPost(new ReactionCreateModel
            {
                IsLike = isLike,
                PostId = postId,
                RemoteIp = remoteIp,
                UserId = userId
            });

            //var session = GetUserSession();
            //if (session != null)
            //{
            //    var userSession = await _cache.GetCachedDataAsync<UserSession>(GetSessionKey(session!));
            //    if (userSession != null)
            //    {
            //        var postViewed = userSession.PostViews.Where(x => x.PostId == postId).FirstOrDefault();
            //        if (postViewed != null)
            //        {
            //            postViewed.IsViewed = true;
            //            postViewed.IsLike = isLike;
            //            await _cache.SetCachedDataAsync(GetSessionKey(session), userSession, TimeSpan.FromMinutes(10));
            //        }
            //    }
            //}

            return Ok();
        }
    }
}
