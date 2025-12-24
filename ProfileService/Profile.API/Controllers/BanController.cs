using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Profile.Domain.Models;
using Profile.Domain.Services;

namespace Profile.API.Controllers
{
    public class BanController : BaseApiController
    {
        private readonly IBanService _banService;
        public BanController(ILogger<BaseApiController> logger, IBanService banService) : base(logger)
        {
            _banService = banService;
        }

        [HttpPost("sendPostBanRequest")]
        public async Task<IActionResult> SendBanRequest([FromBody] PostReport postReport)
        {
            var result = await _banService.SendPostBanRequest(postReport);
            if (result.IsSuccess)
            {
                return Ok();
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

    }
}
