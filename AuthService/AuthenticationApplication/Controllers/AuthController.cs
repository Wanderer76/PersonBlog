using Authentication.Contract.Models;
using Authentication.Service.Models;
using Authentication.Service.Service;
using Blog.Contracts.Models.Blog;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using System.Net.Http.Json;

namespace AuthenticationApplication.Controllers;

public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly IBlogUserProvisioningService _blogUserProvisioningService;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(
        ILogger<AuthController> logger,
        IAuthService authService,
        IBlogUserProvisioningService blogUserProvisioningService,
        IHttpClientFactory httpClientFactory)
        : base(logger)
    {
        _authService = authService;
        _blogUserProvisioningService = blogUserProvisioningService;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("create")]
    [Produces(typeof(AuthCodeResponse))]
    public async Task<Result<AuthCodeResponse>> CreateUser([FromBody] RegisterRequest registerModel)
    {
        var response = await _authService.Register(registerModel);
        if (response.IsSuccess)
        {
            return await _authService.Authenticate(
                new LoginPasswordModel(registerModel.Login, registerModel.Password));
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

    [HttpPost("blog-context")]
    [Authorize]
    [ProducesResponseType(typeof(UserModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserModel>> SetBlogContext([FromBody] BlogContextRequest request)
    {
        var token = HttpContext.Request.Headers.Authorization.FirstOrDefault()?["Bearer ".Length..];
        var currentUserResult = await _authService.GetCurrentUserAsync(token);
        if (currentUserResult.IsFailure || currentUserResult.Value.IsAnonymous)
        {
            return Unauthorized();
        }

        var blogClient = _httpClientFactory.CreateClient("Blog");
        var blogResponse = await blogClient.GetAsync($"Blog/blog/{request.BlogId}");
        if (!blogResponse.IsSuccessStatusCode)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                "Не удалось подтвердить владельца блога");
        }

        var blog = await blogResponse.Content.ReadFromJsonAsync<BlogModel>();
        if (blog is null || blog.UserId != currentUserResult.Value.UserId)
        {
            return Forbid();
        }

        var session = await _blogUserProvisioningService.ProvisionBlogAsync(
            currentUserResult.Value.UserId,
            request.BlogId);

        return Ok(session);
    }
}
