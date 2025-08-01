using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace VideoView.Application.Controllers
{
    public class HealthController : BaseController
    {
        public HealthController(ILogger<BaseController> logger) : base(logger)
        {
        }

        [HttpGet("/health")]
        public IActionResult Get()
        {
            return Ok("success");
        }
    }
}
