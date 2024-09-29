using Authentication.Domain.Entities;
using Authentication.Service.Models;
using AuthenticationApplication.Models;
using AuthenticationApplication.Models.Requests;
using AuthenticationApplication.Service;
using AuthenticationApplication.Service.ApiClient;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace Authentication.Service.Service.Implementation;

internal class DefaultAuthService : IAuthService
{
    private readonly IReadWriteRepository<IAuthEntity> _context;
    private readonly IProfileApiAsyncClient _profileApiClient;
    private readonly DefaultTokenService _tokenService;
    public DefaultAuthService(IReadWriteRepository<IAuthEntity> context,
        IProfileApiAsyncClient profileApiClient,
        DefaultTokenService tokenService)
    {
        _context = context;
        _profileApiClient = profileApiClient;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> Authenticate(LoginModel loginModel)
    {
        var user = await _context.Get<AppUser>()
            .FirstOrDefaultAsync(x => x.Login == loginModel.Login);

        if (PasswordHasher.Validate(user.Password, loginModel.Password))
        {
            throw new ArgumentException("Неверный логиг/пароль");
        }

        var response = _tokenService.GenerateToken(user);

        await _context.SaveChangesAsync();

        return response;
    }

    public async Task<AuthResponse> Register(RegisterModel registerModel)
    {
        var isUserExists = await _context.Get<AppUser>()
            .Where(x => x.Login.Equals(registerModel.Login))
            .AnyAsync();

        if (isUserExists)
        {
            throw new ArgumentException("Пользователь с таким логином уже существует");
        }

        var userId = Guid.NewGuid();

        var userRoles = (await _context.Get<UserRole>()
                .Where(x => registerModel.UserRoleIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync())
            .Select(x => new AppUserRole
            {
                AppUserId = userId,
                UserRoleId = x
            }).ToList();

        if (!userRoles.Any())
        {
            throw new ArgumentException("Роли не найдены");
        }

        var user = new AppUser
        {
            Id = userId,
            Login = registerModel.Login,
            Password = PasswordHasher.GetHash(registerModel.Password),
            CreatedAt = DateTimeOffset.UtcNow,
            AppUserRoles = userRoles
        };
        _context.Add(user);

        var profileCreateRequest = new ProfileCreateRequest
        {
            FirstName = registerModel.Name,
            SurName = registerModel.Surname,
            LastName = registerModel.Lastname,
            Birthdate = registerModel.Birthdate,
            UserId = userId
        };

        var response = await _profileApiClient.CreateProfileAsync(profileCreateRequest);

        if (!response.IsSuccessStatusCode)
        {
            throw new ArgumentException("Не удалось зарегристрировать пользователя");
        }

        await _context.SaveChangesAsync();
        return await Authenticate(new LoginModel(user.Login, user.Password));
    }

    public ValueTask Logout()
    {
        throw new NotImplementedException();
    }

    public async Task<AuthResponse> Refresh(string refreshToken)
    {
        var tokenModel = _tokenService.GetTokenRepresentaion(refreshToken);

        if (tokenModel.Type != TokenTypes.Refresh)
        {
            throw new ArgumentException("Не верный тип токена");
        }

        var userId = tokenModel.UserId;
        await ClearUserToken(userId);

        var user = await _context.Get<AppUser>()
            .Where(x => x.Id == userId)
            .FirstOrDefaultAsync();

        user.AssertFound("Пользователь не найден");

        var response = _tokenService.GenerateToken(user);
        await _context.SaveChangesAsync();
        return response;
    }


    private async Task ClearUserToken(Guid userId)
    {
        var userTokens = await _context.Get<Token>()
            .Where(x => x.AppUserId == userId)
            .ToListAsync();

        foreach (var token in userTokens)
        {
            _context.Remove(token);
        }
    }

    public async Task<bool> ValidateToken(string token)
    {
        var tokenModel = _tokenService.GetTokenRepresentaion(token);

        var now = DateTimeOffset.UtcNow;

        if (tokenModel.ExpiredAt > now)
        {
            await ClearUserToken(tokenModel.UserId);
            return false;
        }

        return true;
    }
}