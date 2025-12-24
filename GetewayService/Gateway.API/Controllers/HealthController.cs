using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Controllers
{
    public class HealthController : BaseApiController
    {
        public HealthController(ILogger<BaseApiController> logger) : base(logger)
        {
        }

        [HttpGet("/health")]
        public IActionResult Get()
        {
            return Ok("success");
        }
    }
}
