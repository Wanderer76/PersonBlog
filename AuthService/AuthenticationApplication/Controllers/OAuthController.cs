using Authentication.Service.Models;
using Authentication.Service.Service;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationApplication.Controllers;

public class OAuthController : BaseApiController
{
    private readonly IOAuthService _oAuthService;

    public OAuthController(ILogger<OAuthController> logger, IOAuthService oAuthService)
    : base(logger)
    {
        _oAuthService = oAuthService;
    }

    // 1. Точка входа: Сюда редиректит React приложение
    // Пример: /api/oauth/authorize?client_id=react-app-1&redirect_uri=...&response_type=code
    [HttpGet("authorize")]
    public async Task<Result<RedirectResponse>> Authorize(string clientId, string redirectUri, string response_type, string state, string returnUrl)
    {
        return await _oAuthService.GenerateAuthCodeAsync(clientId, redirectUri, response_type, state, returnUrl);

    }

    // 2. Обмен кода на токен (делается с бэкенда React приложения или напрямую, если SPA)
    [HttpPost("token")]
    public async Task<Result<AuthResponse>> Token(string grant_type, string code, string client_id, string client_secret)
    {
        return await _oAuthService.ExchangeCodeToTokenAsync(grant_type, code, client_id, client_secret);
    }
}