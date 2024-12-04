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
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Authentication.Test")]

namespace Authentication.Service.Service.Implementation;

internal class DefaultAuthService : IAuthService
{
    private readonly IReadWriteRepository<IAuthEntity> _context;
    private readonly IProfileApiAsyncClient _profileApiClient;
    private readonly ITokenService _tokenService;
    public DefaultAuthService(IReadWriteRepository<IAuthEntity> context,
        IProfileApiAsyncClient profileApiClient,
        ITokenService tokenService)
    {
        _context = context;
        _profileApiClient = profileApiClient;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> Authenticate(LoginModel loginModel)
    {
        var user = await _context.Get<AppUser>()
            .Include(x => x.AppUserRoles)
            .FirstOrDefaultAsync(x => x.Login == loginModel.Login);

        user.AssertFound();

        if (!PasswordHasher.Validate(user.Password, loginModel.Password))
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
            }
        };
        _context.Add(user);

        var profileCreateRequest = new ProfileCreateRequest
        {
            FirstName = registerModel.Name,
            SurName = registerModel.Surname,
            LastName = registerModel.Lastname,
            Birthdate = registerModel.Birthdate,
            UserId = userId,
            Email = registerModel.Email
        };

        var response = await _profileApiClient.CreateProfileAsync(profileCreateRequest);

        if (!response.IsSuccessStatusCode)
        {
            throw new ArgumentException($"Не удалось зарегристрировать пользователя",new Exception(await response.Content.ReadAsStringAsync()));
        }

        await _context.SaveChangesAsync();
        return await Authenticate(new LoginModel(user.Login, registerModel.Password));
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
        await _tokenService.ClearUserToken(refreshToken);

        var user = await _context.Get<AppUser>()
            .Include(x => x.AppUserRoles)
            .Where(x => x.Id == userId)
            .FirstOrDefaultAsync();

        user.AssertFound("Пользователь не найден");

        var response = _tokenService.GenerateToken(user);
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