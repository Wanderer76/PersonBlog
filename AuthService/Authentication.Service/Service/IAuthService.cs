using Authentication.Service.Models;
using AuthenticationApplication.Models;
using Shared.Models;
using Shared.Utils;

namespace Authentication.Service.Service;

public interface IAuthService
{
    Task<Result<AuthResponse,Error>> Authenticate(LoginPasswordModel loginModel);
    Task<Result<AuthResponse,Error>> Register(RegisterModel registerModel);
    Task<Result<AuthResponse,Error>> Refresh(string refreshToken);
    Task<bool> ValidateToken(string token);
    Task<Result<UserModel>> GetCurrentUserAsync(string? token);
    ValueTask Logout();
}