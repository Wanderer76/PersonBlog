using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Profile.Domain.Services;

namespace Profile.API.Controllers
{
    [ApiController]
    public class ViewHistoryController : BaseApiController
    {
        private readonly IViewHistoryService _viewHistoryService;

        public ViewHistoryController(ILogger<ViewHistoryController> logger, IViewHistoryService viewHistoryService) : base(logger)
        {
            _viewHistoryService = viewHistoryService;
        }

        [HttpGet("list/{userId:guid}")]
        public async Task<IActionResult> GetUserHistoryList(Guid userId)
        {
            var result = await _viewHistoryService.GetUserViewHistoryListAsync(userId);
            if (result.IsFailure)
            {
                return BadRequest(result.Errors);
            }

            return Ok(result.Value);
        }

        [HttpGet("item/{postId:guid}/{userId:guid}")]
        public async Task<IActionResult> GetUserHistoryList(Guid postId, Guid userId)
        {
            var result = await _viewHistoryService.GetUserViewHistoryItemAsync(postId, userId);
            if (result.IsFailure)
            {
                return BadRequest(result.Errors);
            }

            return Ok(result.Value);
        }

        [HttpGet("userReaction/{postId:guid}/{userId:guid}")]
        public async Task<IActionResult> GetUserPostReaction(Guid postId, Guid userId,Guid?blogId)
        {
            var result = await _viewHistoryService.GetUserPostReactionAsync(postId, userId,blogId);
            if (result.IsFailure)
            {
                return BadRequest(result.Errors);
            }

            return Ok(result.Value);
        }
    }
}
