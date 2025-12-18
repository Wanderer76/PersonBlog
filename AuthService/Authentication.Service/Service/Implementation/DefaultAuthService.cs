using Authentication.Contract.Constants;
using Authentication.Contract.Events;
using Authentication.Domain.Entities;
using Authentication.Service.Models;
using AuthenticationApplication.Models;
using AuthenticationApplication.Service;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AuthTests")]

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

    public async Task<Result<AuthResponse, Error>> Authenticate(LoginPasswordModel loginModel)
    {
        var user = await _context.Get<AppUser>()
            .Include(x => x.AppUserRoles)
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

            var blogId = await _context.Get<AppProfile>()
                .Where(x => x.UserId == user.Id)
                .Select(x => x.BlogId)
                .FirstOrDefaultAsync();

            var response = await _tokenService.GenerateTokenAsync(user);
            await _cacheService.SetCachedDataAsync(new SessionKey(user.Id), new UserModel(user.Id, user.Login, null, blogId ?? Guid.Empty, user.AppUserRoles.Select(x => x.UserRoleId).ToList()), TimeSpan.FromDays(10));
            await _context.SaveChangesAsync();

            if (loginModel.RedirectUrl != null)
            {
                response.AuthCode = user.Id.ToString();
            }

            return Result<AuthResponse, Error>.Success(response);
        }
        catch (Exception ex)
        {
            return new Error(ex.Message);
        }
    }

    public async Task<Result<AuthResponse, Error>> Register(RegisterModel registerModel)
    {
        var isUserExists = await _context.Get<AppUser>()
            .Where(x => x.Login.Equals(registerModel.Login))
            .AnyAsync();

        if (isUserExists)
        {
            return new Error("400", "Пользователь с таким логином уже существует");
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

        _context.Add(AppProfile.Create(registerModel.Email, registerModel.Name, userId));

        var profileCreateModel = new ProfileRegisterEvent
        (
            registerModel.Name,
            registerModel.Birthdate,
            userId,
            registerModel.Email,
            createdAt
        );

        var userCreateEvent = new UserCreateEvent
        {
            UserId = userId,
            CreatedAt = createdAt,
            UserName = user.Login
        };

        _context.Add(AuthEvent.Create(userCreateEvent));
        _context.Add(AuthEvent.Create(profileCreateModel));
        await _context.SaveChangesAsync();

        return await Authenticate(new LoginPasswordModel(user.Login, registerModel.Password) { RedirectUrl = registerModel.RedirectUrl });
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

    public async Task<Result<AuthResponse, Error>> Refresh(string refreshToken)
    {
        var tokenModel = _tokenService.GetTokenRepresentation(refreshToken);

        if (tokenModel.Type != TokenTypes.Refresh)
        {
            return new Error("Не верный тип токена");
        }

        var userId = tokenModel.UserId;

        var currentRefreshToken = await _context.Get<Token>()
            .Where(x=>x.AppUserId == userId)
            .Where(x=>x.TokenType == TokenTypes.Refresh)
            .FirstOrDefaultAsync();

        if (currentRefreshToken == null)
        {
            return new Error("Время сессии закончено");
        }
        await _tokenService.ClearUserToken(refreshToken);

        var user = await _context.Get<AppUser>()
            .Include(x => x.AppUserRoles)
            .Where(x => x.Id == userId)
            .FirstOrDefaultAsync();

        user.AssertFound("Пользователь не найден");
        var blogId = await _context.Get<AppProfile>()
           .Where(x => x.UserId == user.Id)
           .Select(x => x.BlogId)
           .FirstOrDefaultAsync();
        var response = await _tokenService.GenerateTokenAsync(user);
        await _cacheService.SetCachedDataAsync(new SessionKey(user.Id), new UserModel(user.Id, user.Login, null, blogId??Guid.Empty, user.AppUserRoles.Select(x => x.UserRoleId).ToList()), TimeSpan.FromDays(10));
        await _context.SaveChangesAsync();
        return response;
    }

    public async Task<bool> ValidateToken(string token)
    {
        if (!_tokenService.Validate(token))
        {
            var repr = JwtUtils.GetTokenRepresentaion(token);
            if (repr.IsSuccess)
            {
                await _cacheService.RemoveCachedDataAsync(new SessionKey(JwtUtils.GetTokenRepresentaion(token).Value.UserId));
            }
            await _tokenService.ClearUserToken(token);
            return false;
        }

        return true;
    }
}