using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using ViewReacting.Domain.Services;

namespace VideoReacting.API.Controllers
{
    public class LikedHistoryController : BaseController
    {
        private readonly IViewHistoryService _viewHistoryService;
        public LikedHistoryController(ILogger<BaseController> logger, IViewHistoryService viewHistoryService) : base(logger)
        {
            _viewHistoryService = viewHistoryService;
        }

        [HttpGet("list/{userId:guid}")]
        public async Task<IActionResult> GetLikedHistoryList(Guid userId)
        {
            var result = await _viewHistoryService.GetUserLikedHistoryListAsync(userId);
            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }
    }
}
