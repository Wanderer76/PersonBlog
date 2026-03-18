using Authentication.Domain.Entities;
using Authentication.Service.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace Authentication.Service.Service.Implementation;

internal class DefaultOAuthService : IOAuthService
{
    private readonly IReadWriteRepository<IAuthEntity> _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly ITokenService _tokenService;

    public DefaultOAuthService(IReadWriteRepository<IAuthEntity> repository, ICurrentUserService currentUserService, ICacheService cacheService, ITokenService tokenService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponse>> ExchangeCodeToTokenAsync(string grant_type, string code, string client_id, string client_secret)
    {
        if (grant_type != "authorization_code") return Result<AuthResponse>.Failure(new Error("", "Unsupported grant type"));
        //var client = await _repository.Get<Client>()
        // .FirstOrDefaultAsync(c => c.ClientId == client_id && c.ClientSecret == client_secret);
        // Валидация клиента
        //if (client == null) return Unauthorized("Invalid client credentials");

        var authCodeEntity = await _cacheService.GetCachedDataAsync<AuthCode>(AuthCode.GetCacheKey(code));
        if (authCodeEntity == null || authCodeEntity.ExpiresAt < DateTime.UtcNow)
            return Result<AuthResponse>.Failure(new Error("", "Invalid or expired code"));

        var user = await _repository.Get<AppUser>()
            .Include(x => x.AppUserRoles)
            .Include(x => x.UserContexts)
            .FirstOrDefaultAsync(x => x.Id == authCodeEntity.UserId);
        if (user == null) return Result<AuthResponse>.Failure(new Error("", "User not found"));

        // Удаляем код (он одноразовый)
        var token = await _tokenService.GenerateTokenAsync(user);
        await _repository.SaveChangesAsync();
        await _cacheService.RemoveCachedDataAsync(AuthCode.GetCacheKey(code));

        return new AuthResponse
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
        };
    }

    public async Task<Result<RedirectResponse>> GenerateAuthCodeAsync(string clientId, string redirectUri, string response_type, string state, string returnUrl)
    {

        //var client = await _repository.Get<Client>()
        //    .FirstOrDefaultAsync(c => c.ClientId == clientId && c.RedirectUri == redirectUri);
        //if (client == null) return BadRequest("Invalid Client");

        var user = await _currentUserService.GetCurrentUserAsync();

        if (user.IsAnonymous)
        {
            var queryString = BuildAuthQueryString(clientId, redirectUri, response_type, state, returnUrl);
            return new RedirectResponse($"/auth{queryString}");
        }
        var code = RandomCodeGenerator.GenerateRandomCode();
        await _cacheService.SetCachedDataAsync(AuthCode.GetCacheKey(code), new AuthCode
        {
            Code = code,
            UserId = user.UserId,
            ClientId = clientId,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        }, TimeSpan.FromMinutes(10));

        var redirectUrl = $"{redirectUri}?code={code}&state={state}&returnUrl={returnUrl}";
        return new RedirectResponse(redirectUrl);
    }

    private static string BuildAuthQueryString(
    string clientId,
    string redirectUri,
    string responseType,
    string state,
    string returnUrl)
    {
        var parameters = new List<(string Key, string Value)>
        {
            ("client_id", clientId),
            ("redirectUri", redirectUri),
            ("response_type", responseType),
            ("state", state),
            ("returnUrl", returnUrl)
        };
        var validParams = parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}");

        var queryString = string.Join("&", validParams);
        return string.IsNullOrEmpty(queryString) ? string.Empty : $"?{queryString}";
    }
}