using Authentication.Service.Models;
using AuthenticationApplication.Models;
using AuthenticationApplication.Service;
using Infrastructure.Cache.Models;
using Infrastructure.Cache.Services;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Shared.Services;

namespace AuthenticationApplication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly ICacheService _cacheService;
    
    public AuthController(ILogger<AuthController> logger, IAuthService authService, ICacheService cacheService)
    : base(logger)
    {
        _authService = authService;
        _cacheService = cacheService;
    }

    [HttpGet("session")]
    public async Task<IActionResult> CreateSession()
    {
        var session = GetUserSession();
        await RefreshSession(session);
        return Ok();
    }

    [HttpPost("create")]
    [Produces(typeof(AuthResponse))]
    public async Task<IActionResult> CreateUser(RegisterModel registerModel)
    {
        var response = await _authService.Register(registerModel);
        return Ok(response);
    }

    [HttpPost("login")]
    [Produces(typeof(AuthResponse))]
    public async Task<IActionResult> Login(LoginModel loginModel)
    {
        var hasSession = Request.Cookies.TryGetValue("sessionId", out var session);
        var response = await _authService.Authenticate(loginModel);
        await RefreshSession(session, response.AccessToken);
        return Ok(response);
    }


    [HttpPost("refresh")]
    [Produces(typeof(AuthResponse))]
    public async Task<IActionResult> Refresh(string refreshToken)
    {
        var hasSession = Request.Cookies.TryGetValue("sessionId", out var session);
        var response = await _authService.Refresh(refreshToken);
        await RefreshSession(session);
        return Ok(response);
    }

    private async Task RefreshSession(string? session, string? token = null)
    {
        if (session != null)
        {
            var data = await _cacheService.GetCachedDataAsync<UserSession>($"Session:{session}")!;
            if (data == null)
            {
                await _cacheService.RemoveCachedDataAsync($"Session:{session}");
                Response.Cookies.Delete("sessionId");
            }
            else
            {
                if (token != null)
                {
                    data!.UserId = JwtUtils.GetTokenRepresentaion(token).UserId;
                }
                await _cacheService.SetCachedDataAsync($"Session:{session}", data!, TimeSpan.FromHours(1));
            }   
        }
        else
        {
            var sessionId = GuidService.GetNewGuid().ToString();
            await _cacheService.SetCachedDataAsync($"Session:{sessionId}", new UserSession(), TimeSpan.FromHours(1));
            Response.Cookies.Append("sessionId", sessionId, new CookieOptions
            {
                SameSite = SameSiteMode.Strict,
                HttpOnly = true
            });
        }
    }

}