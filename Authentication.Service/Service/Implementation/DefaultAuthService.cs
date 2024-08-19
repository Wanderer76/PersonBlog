using Authentication.Domain.Entities;
using Authentication.Peristence;
using AuthenticationApplication.Models;
using AuthenticationApplication.Models.Requests;
using AuthenticationApplication.Service;
using AuthenticationApplication.Service.ApiClient;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace Authentication.Service.Service.Implementation;

internal class DefaultAuthService : IAuthService
{
    private readonly IReadWriteRepository<AuthenticationDbContext> _context;
    private readonly IProfileApiAsyncClient _profileApiClient;

    public DefaultAuthService(IReadWriteRepository<AuthenticationDbContext> context,
        IProfileApiAsyncClient profileApiClient)
    {
        _context = context;
        _profileApiClient = profileApiClient;
    }

    public async Task<string> Authenticate(LoginModel loginModel)
    {
        return "success";
    }

    public async Task<string> Register(RegisterModel registerModel)
    {
        var isUserExists = await _context.Get<AppUser>()
            .Where(x => x.Login.Equals(registerModel.Login))
            .AnyAsync();

        if (isUserExists)
        {
            throw new ArgumentException("Пользователь с таким логином уже существует");
        }

        var userId = Guid.NewGuid();

        var roles = (await _context.Get<UserRole>()
                .Where(x => registerModel.UserRoleIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync())
            .Select(x => new AppUserRole
            {
                AppUserId = userId,
                UserRoleId = x
            }).ToList();

        var user = new AppUser
        {
            Id = userId,
            Login = registerModel.Login,
            Password = registerModel.Password,
            CreatedAt = DateTimeOffset.Now,
            AppUserRoles = roles
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
}