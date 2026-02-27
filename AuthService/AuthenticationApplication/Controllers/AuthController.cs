using Authentication.Service.Models;
using Authentication.Service.Service;
using AuthenticationApplication.Models;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;

namespace AuthenticationApplication.Controllers;

public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(ILogger<AuthController> logger, IAuthService authService)
    : base(logger)
    {
        _authService = authService;
    }

    [HttpPost("create")]
    [Produces(typeof(AuthResponse))]
    public async Task<IActionResult> CreateUser([FromBody] RegisterModel registerModel)
    {
        var response = await _authService.Register(registerModel);
        if (response.IsSuccess)
        {
            return Ok(response.Value);
        }
        else
        {
            return BadRequest(response.Error);
        }
    }

    [HttpPost("login")]
    [Produces(typeof(AuthResponse))]
    public async Task<IActionResult> Login(LoginPasswordModel loginModel)
    {
        var response = await _authService.Authenticate(loginModel);
        if (response.IsSuccess)
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
        var response = await _authService.Refresh(refreshToken);
        if (response.IsSuccess)
        {
            return Ok(response.Value);
        }
        else
        {
            return Unauthorized(response.Error);
        }
    }

    [HttpGet("/me")]
    public async Task<ActionResult<UserModel>> GetCurrentUser()
    {
        var token = HttpContext!.Request.Headers.Authorization.FirstOrDefault()?["Bearer ".Length..];
        return Ok(await _authService.GetCurrentUserAsync(token));
    }
}