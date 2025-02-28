using Authentication.Service.Models;
using AuthenticationApplication.Models;
using AuthenticationApplication.Service;
using Infrastructure.Cache.Models;
using Infrastructure.Cache.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AuthenticationApplication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICacheService _cacheService;
    public AuthController(IAuthService authService, ICacheService cacheService)
    {
        _authService = authService;
        _cacheService = cacheService;
    }


    [HttpGet("session")]
    public async Task<IActionResult> CreateSession()
    {
        var hasSession = Request.Cookies.TryGetValue("sessionId", out var session);
        await RefreshSession(session);

        return Ok();
    }

    private async Task RefreshSession(string? session, string? token = null)
    {
        if (session != null)
        {
            var data = await _cacheService.GetCachedData<UserSession>($"Session:{session}")!;
            if (token != null)
            {
                data!.UserId = JwtUtils.GetTokenRepresentaion(token).UserId;
            }
            await _cacheService.SetCachedData($"Session:{session}", data!, TimeSpan.FromHours(1));
        }
        else
        {
            var sessionId = GuidService.GetNewGuid().ToString();
            await _cacheService.SetCachedData($"Session:{sessionId}", new UserSession(), TimeSpan.FromHours(1));
            Response.Cookies.Append("sessionId", sessionId, new CookieOptions
            {
                SameSite = SameSiteMode.Strict,
                HttpOnly = true
            });
        }
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
}