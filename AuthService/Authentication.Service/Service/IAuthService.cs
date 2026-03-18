using Authentication.Service.Models;
using AuthenticationApplication.Models;
using Shared.Models;
using Shared.Utils;

namespace Authentication.Service.Service;

public interface IAuthService
{
    Task<Result<AuthCodeResponse>> Authenticate(LoginPasswordModel loginModel);
    Task<Result> Register(RegisterModel registerModel);
    Task<Result<AuthResponse>> Refresh(string refreshToken);
    Task<bool> ValidateToken(string token);
    Task<Result<UserModel>> GetCurrentUserAsync(string? token);
    ValueTask Logout();
}