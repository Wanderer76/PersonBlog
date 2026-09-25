using Authentication.Domain.Entities;
using Authentication.Service.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.WebUtilities;
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
        var client = await _repository.Get<Client>()
         .FirstOrDefaultAsync(c => c.ClientId == client_id);

        if (client == null) return Result<AuthResponse>.Failure(new Error("Invalid client credentials"));

        var authCodeEntity = await _cacheService.GetCachedDataAsync<AuthCode>(AuthCode.GetCacheKey(code));
        if (authCodeEntity == null ||
            authCodeEntity.ExpiresAt < DateTime.UtcNow ||
            !string.Equals(authCodeEntity.ClientId, client_id, StringComparison.Ordinal))
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
        if (!string.Equals(response_type, "code", StringComparison.Ordinal))
            return Result<RedirectResponse>.Failure(new Error("unsupported_response_type", "Unsupported response type"));

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirectUri))
            return Result<RedirectResponse>.Failure(new Error("invalid_request", "Client ID and redirect URI are required"));

        var client = await _repository.Get<Client>()
            .FirstOrDefaultAsync(c => c.ClientId == clientId);
        if (client == null || !string.Equals(client.RedirectUri, redirectUri, StringComparison.Ordinal))
            return Result<RedirectResponse>.Failure(new Error("invalid_request", "Invalid client or redirect URI"));

        var user = await _currentUserService.GetCurrentUserAsync();

        if (user.IsAnonymous)
        {
            var authClient = await _repository.Get<Client>()
                .FirstOrDefaultAsync(c => c.ClientId == "auth");
            if (authClient == null)
                return Result<RedirectResponse>.Failure(new Error("server_error", "Authentication client was not found"));

            return new RedirectResponse(QueryHelpers.AddQueryString(authClient.RedirectUri, new Dictionary<string, string?>
            {
                ["client_id"] = clientId,
                ["redirectUri"] = redirectUri,
                ["response_type"] = response_type,
                ["state"] = state,
                ["returnUrl"] = returnUrl
            }));
        }

        var code = RandomCodeGenerator.GenerateRandomCode();
        await _cacheService.SetCachedDataAsync(AuthCode.GetCacheKey(code), new AuthCode
        {
            Code = code,
            UserId = user.UserId,
            ClientId = clientId,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        }, TimeSpan.FromMinutes(10));

        var redirectUrl = QueryHelpers.AddQueryString(redirectUri, new Dictionary<string, string?>
        {
            ["code"] = code,
            ["state"] = state,
            ["returnUrl"] = returnUrl
        });
        return new RedirectResponse(redirectUrl);
    }
}
