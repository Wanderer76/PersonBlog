using Authentication.Domain.Entities;
using Authentication.Service.Models;
using AuthenticationApplication.Models;
using AuthenticationApplication.Service;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;

namespace AuthenticationApplication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _userSession;
    private readonly IReadRepository<IAuthEntity> _readAuth;
    public AuthController(ILogger<AuthController> logger, IAuthService authService, ICurrentUserService userSession, IReadRepository<IAuthEntity> readAuth)
    : base(logger)
    {
        _authService = authService;
        _userSession = userSession;
        _readAuth = readAuth;
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
        var tokenRepr = token == null ? null : JwtUtils.GetTokenRepresentaion(token);
        if (tokenRepr == null || tokenRepr.IsFailure)
            return UserModel.AnonymousUser();

        var tokenData = tokenRepr.Value;

        var userRoles = await _readAuth.Get<AppUserRole>()
            .Where(x => x.AppUserId == tokenData.UserId)
            .Select(x => x.UserRoleId)
            .ToListAsync();

        var model = new UserModel(tokenData.UserId, tokenData.Login, null, tokenData.BlogId, userRoles);
        return Ok(model);
    }

}