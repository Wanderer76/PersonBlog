using Authentication.Service.Models;
using AuthGateway.API.Services;
using Infrastructure.Extensions;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuthGateway.API.Controllers;

public class AuthController(ILogger<AuthController> logger, IHttpClientFactory _httpClientFactory) : BaseApiController(logger)
{
    [HttpPost("create")]
    [Produces(typeof(AuthCodeResponse))]
    public async Task<IActionResult> CreateUser([FromBody] RegisterRequest registerModel)
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
    public async Task<ActionResult<AuthCodeResponse>> Login([FromBody] LoginPasswordModel loginModel)
    {
        var response = await _httpClientFactory.AuthenticateAsync(loginModel);
        return ToActionResult(response);
    }

    [HttpPost("refresh")]
    [Produces(typeof(AuthResponse))]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var response = await _httpClientFactory.RefreshAsync(HttpContext, request.RefreshToken);
        return ToActionResult(response);
    }

    [HttpGet("authorize")]
    public async Task<ActionResult<RedirectResponse>> Authorize(string clientId, string redirectUri, string response_type, string state, string returnUrl)
    {
        using var client = _httpClientFactory.CreateClient("Auth");
        var result = await client.GetFromJsonAsync<Result<RedirectResponse>>($"OAuth/authorize?clientId={clientId}&redirectUri={redirectUri}&response_type={response_type}&state={state}&returnUrl={returnUrl}");
        return ToActionResult(result);
    }

    // 2. Обмен кода на токен (делается с бэкенда React приложения или напрямую, если SPA)
    [HttpPost("token")]
    public async Task<IActionResult> Token([FromBody] TokenRequest body)
    {
        using var client = _httpClientFactory.CreateClient("Auth");
        var result = await client.PostAsync($"OAuth/token?grant_type={body.grant_type}&code={body.code}&client_id={body.client_id}&client_secret={body.client_secret}&redirect_uri={body.redirect_uri}", null);
        if (result.IsSuccessStatusCode)
        {
            return Ok((await result.Content.ReadFromJsonAsync<Result<AuthResponse>>())!.Value);
        }
        return BadRequest(result.Content);
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
