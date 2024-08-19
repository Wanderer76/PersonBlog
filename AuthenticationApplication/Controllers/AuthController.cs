using AuthenticationApplication.Models;
using AuthenticationApplication.Service;
using Microsoft.AspNetCore.Mvc;

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

    [HttpPost("create")]
    public async Task<IActionResult> CreateUser(RegisterModel registerModel)
    {
        await _authService.Register(registerModel);
        return Ok();
    }
}