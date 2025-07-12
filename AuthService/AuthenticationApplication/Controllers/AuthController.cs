using Authentication.Service.Models;
using AuthenticationApplication.Models;
using AuthenticationApplication.Service;
using Infrastructure.Interface;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationApplication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _userSession;

    public AuthController(ILogger<AuthController> logger, IAuthService authService, ICurrentUserService userSession)
    : base(logger)
    {
        _authService = authService;
        _userSession = userSession;
    }

    [HttpPost("create")]
    [Produces(typeof(AuthResponse))]
    public async Task<IActionResult> CreateUser(RegisterModel registerModel)
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
            await _userSession.UpdateCurrentUserAsync(response.Value.AccessToken);
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
            await _userSession.UpdateCurrentUserAsync(response.Value.AccessToken);
            return Ok(response.Value);
        }
        else
        {
            return BadRequest(response.Error);
        }
    }
}