using Authentication.Contract.Models;
using Authentication.Domain.Entities;
using Authentication.Service.Models;
using Authentication.Service.Service;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using System.Security.Cryptography;

namespace AuthenticationApplication.Controllers;

public class OAuthController : BaseApiController
{
    private readonly IReadWriteRepository<IAuthEntity> _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly ITokenService _tokenService;

    public OAuthController(ILogger<OAuthController> logger, IReadWriteRepository<IAuthEntity> repository, ICurrentUserService currentUserService, ICacheService cacheService, ITokenService tokenService)
    : base(logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _tokenService = tokenService;
    }

    // 1. Точка входа: Сюда редиректит React приложение
    // Пример: /api/oauth/authorize?client_id=react-app-1&redirect_uri=...&response_type=code
    [HttpGet("authorize")]
    public async Task<RedirectResponse> Authorize(string clientId, string redirectUri, string response_type, string state, string returnUrl)
    {
        //var client = await _repository.Get<Client>()
        //    .FirstOrDefaultAsync(c => c.ClientId == clientId && c.RedirectUri == redirectUri);
        //if (client == null) return BadRequest("Invalid Client");

        // ПРОВЕРКА АВТОРИЗАЦИИ ПОЛЬЗОВАТЕЛЯ
        // Здесь мы проверяем, залогинен ли пользователь в нашу систему авторизации.
        // Если нет - нужно редиректить на страницу логина (см. ниже), а после логина возвращаться сюда.
        // Для примера предположим, что пользователь уже залогинен (есть Claim "sub")
        var userIdClaim = await _currentUserService.GetCurrentUserAsync();

        if (userIdClaim.IsAnonymous)
        {
            // Если не залогинен, редиректим на нашу же страницу входа, запоминая параметры
            return new RedirectResponse($"/auth{(Request.QueryString.Value!)}");
        }
        // Генерируем временный код
        var code = GenerateRandomCode();
        await _cacheService.SetCachedDataAsync(AuthCode.GetCacheKey(code), new AuthCode
        {
            Code = code,
            UserId = userIdClaim.UserId,
            ClientId = clientId,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        },TimeSpan.FromMinutes(10));
        // Редирект обратно в React приложение с кодом
        var redirectUrl = $"{redirectUri}?code={code}&state={state}&returnUrl={returnUrl}";
        return new RedirectResponse(redirectUrl);
    }

    // 2. Обмен кода на токен (делается с бэкенда React приложения или напрямую, если SPA)
    [HttpPost("token")]
    public async Task<IActionResult> Token(string grant_type, string code, string client_id, string client_secret, string redirect_uri)
    {
        if (grant_type != "authorization_code") return BadRequest("Unsupported grant type");
        //var client = await _repository.Get<Client>()
        // .FirstOrDefaultAsync(c => c.ClientId == client_id && c.ClientSecret == client_secret);
        // Валидация клиента
        //if (client == null) return Unauthorized("Invalid client credentials");

        // Валидация кода
        var authCodeEntity = await _cacheService.GetCachedDataAsync<AuthCode>(AuthCode.GetCacheKey(code));
        if (authCodeEntity == null || authCodeEntity.ExpiresAt < DateTime.UtcNow)
            return BadRequest("Invalid or expired code");

        // Получаем пользователя
        var user = await _repository.Get<AppUser>()
            .Include(x => x.AppUserRoles)
            .Include(x => x.UserContexts)
            .FirstOrDefaultAsync(x => x.Id == authCodeEntity.UserId);
        if (user == null) return NotFound("User not found");

        // Удаляем код (он одноразовый)
        var token = await _tokenService.GenerateTokenAsync(user);
        await _repository.SaveChangesAsync();
        await _cacheService.RemoveCachedDataAsync(AuthCode.GetCacheKey(code));

        return Ok(new AuthResponse
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
        });
    }

    // Вспомогательные методы
    private string GenerateRandomCode()
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_");
        }
    }
}

public class RedirectResponse
{
    public string RedirectUrl { get; set; } = null!;
    public RedirectResponse()
    {
        
    }
    public RedirectResponse(string redirectUrl)
    {
        RedirectUrl = redirectUrl;
    }
}