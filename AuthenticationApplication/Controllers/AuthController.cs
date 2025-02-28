using Authentication.Service.Models;
using AuthenticationApplication.Models;
using AuthenticationApplication.Service;
using Microsoft.AspNetCore.Mvc;
using Shared.Services;

namespace AuthenticationApplication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }


    [HttpGet("session")]
    public async Task<IActionResult> CreateSession()
    {
        Response.Cookies.Append("sessionId", GuidService.GetNewGuid().ToString(), new CookieOptions
        {
        });
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
        var response = await _authService.Authenticate(loginModel);
        return Ok(response);
    }


    [HttpPost("refresh")]
    [Produces(typeof(AuthResponse))]
    public async Task<IActionResult> Refresh(string refreshToken)
    {
        var response = await _authService.Refresh(refreshToken);
        return Ok(response);
    }
}