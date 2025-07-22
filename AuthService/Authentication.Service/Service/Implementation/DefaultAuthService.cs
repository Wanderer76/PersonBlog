using Authentication.Contract.Events;
using Authentication.Domain;
using Authentication.Domain.Entities;
using Authentication.Domain.Interfaces.Models.Profile;
using Authentication.Service.Models;
using AuthenticationApplication.Models;
using AuthenticationApplication.Service;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;
using System.Runtime.CompilerServices;
using System.Text.Json;
[assembly: InternalsVisibleTo("Authentication.Test")]

namespace Authentication.Service.Service.Implementation;

internal class DefaultAuthService : IAuthService
{
    private readonly IReadWriteRepository<IAuthEntity> _context;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _userSession;

    public DefaultAuthService(IReadWriteRepository<IAuthEntity> context, ITokenService tokenService, ICurrentUserService userSession)
    {
        _context = context;
        _tokenService = tokenService;
        _userSession = userSession;
    }

    public async Task<Result<AuthResponse, Error>> Authenticate(LoginPasswordModel loginModel)
    {
        var user = await _context.Get<AppUser>()
            .Include(x => x.AppUserRoles)
            .FirstOrDefaultAsync(x => x.Login == loginModel.Login);

        user.AssertFound();

        if (!PasswordHasher.Validate(user.Password, loginModel.Password))
        {
            return new Error("400", "Неверный логин/пароль");
        }

        var response = await _tokenService.GenerateTokenAsync(user);

        await _context.SaveChangesAsync();

        return Result<AuthResponse, Error>.Success(response);
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

        var user = new AppUser
        {
            Id = userId,
            Login = registerModel.Login,
            Password = PasswordHasher.GetHash(registerModel.Password),
            CreatedAt = DateTimeOffset.UtcNow,
            AppUserRoles = new List<AppUserRole>
            {
                new AppUserRole
                {
                    AppUserId = userId,
                    UserRoleId = Roles.User
                }
            },
        };
        _context.Add(user);

        var profileCreateModel = new ProfileCreateModel
        {
            FirstName = registerModel.Name,
            SurName = registerModel.Surname,
            LastName = registerModel.Lastname,
            Birthdate = registerModel.Birthdate,
            UserId = userId,
            Email = registerModel.Email
        };
        var profile = AppProfile.Create(
                birthdate: profileCreateModel.Birthdate,
                email: profileCreateModel.Email,
                firstName: profileCreateModel.FirstName ?? string.Empty,
                surName: profileCreateModel.SurName ?? string.Empty,
                lastName: profileCreateModel.LastName ?? string.Empty,
                userId: profileCreateModel.UserId
            );

        var userCreateEvent = new UserCreateEvent
        {
            UserId = userId,
            CreatedAt = DateTimeService.Now(),
            PhotoUrl = profile.PhotoUrl,
            UserName = user.Login
        };

        _context.Add(new AuthEvent { EventData = JsonSerializer.Serialize(userCreateEvent), EventType = nameof(UserCreateEvent) });
        _context.Add(profile);
        await _context.SaveChangesAsync();

        return await Authenticate(new LoginPasswordModel(user.Login, registerModel.Password));
    }

    public async ValueTask Logout()
    {
        var user = await _userSession.GetCurrentUserAsync();
        if (user.UserId.HasValue)
        {
            await _context.Get<Token>()
                .Where(x => x.AppUserId == user.UserId.Value)
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
        await _tokenService.ClearUserToken(refreshToken);

        var user = await _context.Get<AppUser>()
            .Include(x => x.AppUserRoles)
            .Where(x => x.Id == userId)
            .FirstOrDefaultAsync();

        user.AssertFound("Пользователь не найден");

        var response = await _tokenService.GenerateTokenAsync(user);
        await _context.SaveChangesAsync();
        return response;
    }

    public async Task<bool> ValidateToken(string token)
    {
        if (!_tokenService.Validate(token))
        {
            await _tokenService.ClearUserToken(token);
            return false;
        }

        return true;
    }
}