using Authentication.Service.Models;
using AuthenticationApplication.Models;
using Shared.Utils;

namespace AuthenticationApplication.Service;

public interface IAuthService
{
    Task<Result<AuthResponse,Error>> Authenticate(LoginPasswordModel loginModel);
    Task<Result<AuthResponse,Error>> Register(RegisterModel registerModel);
    Task<Result<AuthResponse,Error>> Refresh(string refreshToken);
    Task<bool> ValidateToken(string token);
    ValueTask Logout();
}