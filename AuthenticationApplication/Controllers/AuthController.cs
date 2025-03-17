using Authentication.Service.Models;
using AuthenticationApplication.Models;
using AuthenticationApplication.Service;
using Infrastructure.Cache.Services;
using Infrastructure.Interface;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;

namespace AuthenticationApplication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly ICacheService _cacheService;
    private readonly IUserSession _userSession;

    public AuthController(ILogger<AuthController> logger, IAuthService authService, ICacheService cacheService, IUserSession userSession)
    : base(logger)
    {
        _authService = authService;
        _cacheService = cacheService;
        _userSession = userSession;
    }

    [HttpGet("session")]
    public async Task<IActionResult> CreateSession()
    {
        var session = GetUserSession();
        await _userSession.UpdateUserSession(session);
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
        var hasSession = Request.Cookies.TryGetValue(SessionKey.Key, out var session);
        var response = await _authService.Authenticate(loginModel);
        await _userSession.UpdateUserSession(session);
        return Ok(response);
    }


    [HttpPost("refresh")]
    [Produces(typeof(AuthResponse))]
    public async Task<IActionResult> Refresh(string refreshToken)
    {
        var hasSession = Request.Cookies.TryGetValue(SessionKey.Key, out var session);
        var response = await _authService.Refresh(refreshToken);
        await _userSession.UpdateUserSession(session);
        return Ok(response);
    }
}