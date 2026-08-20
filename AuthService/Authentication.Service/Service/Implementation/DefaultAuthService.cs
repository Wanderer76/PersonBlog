using Authentication.Contract.Constants;
using Authentication.Contract.Events;
using Authentication.Domain.Entities;
using Authentication.Service.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
[assembly: InternalsVisibleTo("AuthTests", AllInternalsVisible = true)]

namespace Authentication.Service.Service.Implementation;

internal class DefaultAuthService : IAuthService
{
    private readonly IReadWriteRepository<IAuthEntity> _context;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _userSession;
    private readonly ICacheService _cacheService;

    public DefaultAuthService(IReadWriteRepository<IAuthEntity> context, ITokenService tokenService, ICurrentUserService userSession, ICacheService cacheService)
    {
        _context = context;
        _tokenService = tokenService;
        _userSession = userSession;
        _cacheService = cacheService;
    }

    public async Task<Result<AuthCodeResponse>> Authenticate(LoginPasswordModel loginModel)
    {
        var user = await _context.Get<AppUser>()
            .Include(x => x.AppUserRoles)
            .Include(x => x.UserContexts)
            .FirstOrDefaultAsync(x => x.Login == loginModel.Login);

        if (user == null)
        {
            return new Error("400", "Пользователь не найден");
        }
        try
        {
            if (!PasswordHasher.Validate(user.Password, loginModel.Password))
            {
                return new Error("400", "Неверный логин/пароль");
            }

            var client = await _context.Get<Client>()
                .FirstOrDefaultAsync(x => x.ClientId == loginModel.ClientId);
            if (client == null ||
                !string.Equals(client.RedirectUri, loginModel.RedirectUrl, StringComparison.Ordinal))
            {
                return new Error("invalid_request", "Invalid client or redirect URI");
            }

            var context = GetDefaultContext(user);

            await _cacheService.SetCachedDataAsync(
                new SessionKey(user.Id),
                new UserModel(
                    user.Id,
                    user.Login,
                    null,
                    context?.ContextType,
                    context?.ContextId ?? Guid.Empty,
                    user.AppUserRoles.Select(x => x.UserRoleId).ToList()),
                TimeSpan.FromMinutes(10));
            await _context.SaveChangesAsync();

            var authCode = RandomCodeGenerator.GenerateRandomCode();

            await _cacheService.SetCachedDataAsync(AuthCode.GetCacheKey(authCode), new AuthCode
            {
                Code = authCode,
                UserId = user.Id,
                ClientId = client.ClientId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            }, TimeSpan.FromMinutes(10));

            return Result<AuthCodeResponse>.Success(new AuthCodeResponse { AuthCode = authCode });
        }
        catch (Exception ex)
        {
            return new Error(ex.Message);
        }
    }

    public async Task<Result> Register(RegisterRequest registerModel)
    {
        var isUserExists = await _context.Get<AppUser>()
            .Where(x => x.Login.Equals(registerModel.Login))
            .AnyAsync();

        if (isUserExists)
        {
            return Result.Failure(new Error("400", "Пользователь с таким логином уже существует"));
        }

        var userId = Guid.NewGuid();
        var createdAt = DateTimeService.Now();

        var user = new AppUser
        {
            Id = userId,
            Login = registerModel.Login,
            Password = PasswordHasher.GetHash(registerModel.Password),
            CreatedAt = createdAt,
            AppUserRoles = new List<AppUserRole>
            {
                new AppUserRole
                {
                    AppUserId = userId,
                    UserRoleId = Roles.UserRoleId
                }
            },
        };
        _context.Add(user);

        var userRegisteredEvent = new ProfileRegisterEvent
        (
            registerModel.UserName,
            user.Login,
            userId,
            createdAt
        );

        _context.Add(AuthEvent.Create(userRegisteredEvent));
        await _context.SaveChangesAsync();
        return Result.Success();
    }

    public async ValueTask Logout()
    {
        var user = await _userSession.GetCurrentUserAsync();
        if (!user.IsAnonymous)
        {
            await _cacheService.RemoveCachedDataAsync(new SessionKey(user.UserId));

            await _context.Get<Token>()
                .Where(x => x.AppUserId == user.UserId)
                .ExecuteDeleteAsync();
        }
    }

    public async Task<Result<AuthResponse>> Refresh(string refreshToken)
    {
        var tokenModel = _tokenService.GetTokenRepresentation(refreshToken);

        if (tokenModel.IsFailure)
            return Result<AuthResponse>.Failure(tokenModel.Errors![0]);

        if (tokenModel.Value.Type != TokenTypes.Refresh)
        {
            return new Error("Не верный тип токена");
        }

        var userId = tokenModel.Value.UserId;

        var currentRefreshToken = await _context.Get<Token>()
            .Where(x => x.AppUserId == userId)
            .Where(x => x.TokenType == TokenTypes.Refresh)
            .FirstOrDefaultAsync();

        if (currentRefreshToken == null)
        {
            return new Error("Время сессии закончено");
        }
        await _tokenService.ClearUserToken(refreshToken);

        var user = await _context.Get<AppUser>()
            .Include(x => x.AppUserRoles)
            .Include(x => x.UserContexts)
            .Where(x => x.Id == userId)
            .FirstAsync();

        if (user == null)
        {
            return new Error("Пользователь не найден");
        }

        var context = GetDefaultContext(user);
        var response = await _tokenService.GenerateTokenAsync(user);
        await _cacheService.SetCachedDataAsync(
            new SessionKey(user.Id),
            new UserModel(
                user.Id,
                user.Login,
                null,
                context?.ContextType,
                context?.ContextId ?? Guid.Empty,
                user.AppUserRoles.Select(x => x.UserRoleId).ToList()),
            TimeSpan.FromDays(10));
        await _context.SaveChangesAsync();
        return response;
    }

    public async Task<Result<UserModel>> GetCurrentUserAsync(string? token)
    {
        var now = DateTimeService.Now();
        var tokenRepr = token == null ? null : _tokenService.GetTokenRepresentation(token);
        if (tokenRepr == null || tokenRepr != null && (tokenRepr.IsFailure || tokenRepr?.Value?.ExpiredAt <= now))
            return UserModel.AnonymousUser();

        var tokenData = tokenRepr!.Value;

        var userRoles = await _context.Get<AppUserRole>()
            .Where(x => x.AppUserId == tokenData.UserId)
            .Select(x => x.UserRoleId)
            .ToListAsync();

        var model = new UserModel(
            tokenData.UserId,
            tokenData.Login,
            null,
            tokenData.ContextType,
            tokenData.ContextId,
            userRoles);
        return model;
    }

    private static UserContext? GetDefaultContext(AppUser user) =>
        user.UserContexts
            .OrderByDescending(x => x.ContextType == UserContextTypes.Blog)
            .FirstOrDefault();
}
