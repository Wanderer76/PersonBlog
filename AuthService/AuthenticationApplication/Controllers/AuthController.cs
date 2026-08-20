using Authentication.Contract.Models;
using Authentication.Service.Models;
using Authentication.Service.Service;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;

namespace AuthenticationApplication.Controllers;

public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly IUserContextService _userContextService;

    public AuthController(
        ILogger<AuthController> logger,
        IAuthService authService,
        IUserContextService userContextService)
        : base(logger)
    {
        _authService = authService;
        _userContextService = userContextService;
    }

    [HttpPost("create")]
    [Produces(typeof(AuthCodeResponse))]
    public async Task<Result<AuthCodeResponse>> CreateUser([FromBody] RegisterRequest registerModel)
    {
        var response = await _authService.Register(registerModel);
        if (response.IsSuccess)
        {
            return await _authService.Authenticate(
                new LoginPasswordModel(registerModel.Login, registerModel.Password)
                {
                    ClientId = registerModel.ClientId,
                    RedirectUrl = registerModel.RedirectUrl
                });
        }

        return Result<AuthCodeResponse>.Failure(response.Errors);
    }

    [HttpPost("login")]
    [Produces(typeof(Result<AuthCodeResponse>))]
    public async Task<Result<AuthCodeResponse>> Login([FromBody] LoginPasswordModel loginModel)
    {
        return await _authService.Authenticate(loginModel);
    }

    [HttpPost("refresh")]
    [Produces(typeof(AuthResponse))]
    public async Task<Result<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        return await _authService.Refresh(request.RefreshToken);
    }

    [HttpGet("/me")]
    public async Task<ActionResult<UserModel>> GetCurrentUser()
    {
        var token = HttpContext.Request.Headers.Authorization.FirstOrDefault()?["Bearer ".Length..];
        return Ok(await _authService.GetCurrentUserAsync(token));
    }

    [HttpPost("context")]
    [Authorize]
    [ProducesResponseType(typeof(UserModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<UserModel>> ActivateContext(
        [FromBody] ActivateUserContextRequest request,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirst(AppClaimTypes.UserId)?.Value;
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        if (request.ContextId == Guid.Empty || string.IsNullOrWhiteSpace(request.ContextType))
            return BadRequest("ContextType and ContextId are required.");

        if (request.UserId != userId)
            return Forbid();

        var result = await _userContextService.GrantAndActivateAsync(
            request.UserId,
            request.ContextType,
            request.ContextId,
            request.RoleId,
            cancellationToken);

        if (result.IsSuccess)
            return Ok(result.Value);

        return BadRequest(result.Errors);
    }

}
