using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using ViewReacting.Domain.Services;

namespace VideoReacting.API.Controllers
{
    [ApiController]
    public class ViewHistoryController : BaseController
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
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }
    }
}
