using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Profile.Domain.Services;

namespace Profile.API.Controllers
{
    public class LikedHistoryController : BaseApiController
    {
        private readonly IViewHistoryService _viewHistoryService;
        public LikedHistoryController(ILogger<BaseApiController> logger, IViewHistoryService viewHistoryService) : base(logger)
        {
            _viewHistoryService = viewHistoryService;
        }

        [HttpGet("list/{userId:guid}")]
        public async Task<IActionResult> GetLikedHistoryList(Guid userId)
        {
            var result = await _viewHistoryService.GetUserLikedHistoryListAsync(userId);
            if (result.IsFailure)
            {
                return BadRequest(result.Errors);
            }

            return Ok(result.Value);
        }
    }
}
