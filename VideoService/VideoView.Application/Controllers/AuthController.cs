using Authentication.Service.Models;
using AuthenticationApplication.Models;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using VideoView.Application.Api;

namespace VideoView.Application.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public AuthController(ILogger<AuthController> logger, IHttpClientFactory httpClientFactory) : base(logger)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("create")]
        [Produces(typeof(AuthResponse))]
        public async Task<IActionResult> CreateUser([FromBody] RegisterModel registerModel)
        {
            var result = await _httpClientFactory.CreateUserAsync(registerModel);
            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }
            return Ok(result.Value);
        }

        [HttpPost("login")]
        [Produces(typeof(AuthResponse))]
        public async Task<IActionResult> Login([FromBody] LoginPasswordModel loginModel)
        {
            var response = await _httpClientFactory.AuthenticateAsync(loginModel);
            if (!response.IsFailure)
            {
                return Ok(response.Value);
            }
            else
            {
                return BadRequest(response.Error);
            }
        }

        [HttpPost("refresh")]
        [Produces(typeof(AuthResponse))]
        public async Task<IActionResult> Refresh(string refreshToken)
        {
            var response = await _httpClientFactory.RefreshAsync(HttpContext, refreshToken);
            if (!response.IsFailure)
            {
                return Ok(response.Value);
            }
            else
            {
                return BadRequest(response.Error);
            }
        }

        [HttpGet("session")]
        public async Task<IActionResult> CreateSession()
        {
            return Ok(await _httpClientFactory.UpdateSessionAsync(HttpContext));
        }
    }
}
