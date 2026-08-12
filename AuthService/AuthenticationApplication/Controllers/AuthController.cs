using Authentication.Service.Models;
using Authentication.Service.Service;
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
    [Produces(typeof(AuthCodeResponse))]
    public async Task<Result<AuthCodeResponse>> CreateUser([FromBody] RegisterRequest registerModel)
    {
        var response = await _authService.Register(registerModel);
        if (response.IsSuccess)
        {
            var authResult = await _authService.Authenticate(new LoginPasswordModel(registerModel.Login, registerModel.Password));
            return authResult;
        }
        else
            return Result<AuthCodeResponse>.Failure(response.Errors);
    }

    [HttpPost("login")]
    [Produces(typeof(Result<AuthCodeResponse>))]
    public async Task<Result<AuthCodeResponse>> Login([FromBody] LoginPasswordModel loginModel)
    {
        var response = await _authService.Authenticate(loginModel);
        return response;
    }

    [HttpPost("refresh")]
    [Produces(typeof(AuthResponse))]
    public async Task<Result<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var response = await _authService.Refresh(request.RefreshToken);
        return response;
    }

    [HttpGet("/me")]
    public async Task<ActionResult<UserModel>> GetCurrentUser()
    {
        var token = HttpContext!.Request.Headers.Authorization.FirstOrDefault()?["Bearer ".Length..];
        return Ok(await _authService.GetCurrentUserAsync(token));
    }
}
