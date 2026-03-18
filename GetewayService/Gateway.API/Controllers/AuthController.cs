using Authentication.Domain.Entities;
using Authentication.Service.Models;
using Authentication.Service.Service;
using AuthenticationApplication.Models;
using Gateway.API.Api;
using Infrastructure.Extensions;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : BaseApiController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public AuthController(ILogger<AuthController> logger, IHttpClientFactory httpClientFactory) : base(logger)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("create")]
        [Produces(typeof(AuthCodeResponse))]
        public async Task<IActionResult> CreateUser([FromBody] RegisterModel registerModel)
        {
            var result = await _httpClientFactory.CreateUserAsync(registerModel);
            if (result.IsFailure)
            {
                return BadRequest(result.Errors);
            }
            return Ok(result.Value);
        }

        [HttpPost("login")]
        [Produces(typeof(AuthCodeResponse))]
        public async Task<IActionResult> Login([FromBody] LoginPasswordModel loginModel)
        {
            var response = await _httpClientFactory.AuthenticateAsync(loginModel);
            if (!response.IsFailure)
            {
                return Ok(response.Value);
            }
            else
            {
                return BadRequest(response.Errors.ToValidationProblem());
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
                return Unauthorized(response.Errors);
            }
        }


        [HttpGet("authorize")]
        public async Task<IActionResult> Authorize(string clientId, string redirectUri, string response_type, string state, string returnUrl)
        {
            using var client = _httpClientFactory.CreateClient("Auth");
            var result = await client.GetFromJsonAsync<RedirectResponse>($"OAuth/authorize?clientId={clientId}&redirectUri={redirectUri}&response_type={response_type}&state={state}&returnUrl={returnUrl}");
            return Ok(result);
        }

        // 2. Обмен кода на токен (делается с бэкенда React приложения или напрямую, если SPA)
        [HttpPost("token")]
        public async Task<IActionResult> Token([FromBody]TokenRequest body)
        {
            using var client = _httpClientFactory.CreateClient("Auth");
            var result = await client.PostAsync($"OAuth/token?grant_type={body.grant_type}&code={body.code}&client_id={body.client_id}&client_secret={body.client_secret}&redirect_uri={body.redirect_uri}",null);
            if (result.IsSuccessStatusCode)
            {
                return Ok(await result.Content.ReadFromJsonAsync<AuthResponse>());
            }
            return BadRequest(result.Content);
        }
    }
}

public class TokenRequest
{
    public string grant_type { get; set; }
    public string code { get; set; }
    public string client_id { get; set; }
    public string client_secret { get; set; }
    public string redirect_uri { get; set; }
}

public class RedirectResponse
{
    public string RedirectUrl { get; set; } = null!;
    public RedirectResponse()
    {

    }
    public RedirectResponse(string redirectUrl)
    {
        RedirectUrl = redirectUrl;
    }
}