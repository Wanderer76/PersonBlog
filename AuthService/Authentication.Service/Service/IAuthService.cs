using Authentication.Service.Models;
using Shared.Models;

namespace Authentication.Service.Service;

public interface IAuthService
{
    Task<Result<AuthCodeResponse>> Authenticate(LoginPasswordModel loginModel);
    Task<Result> Register(RegisterRequest registerModel);
    Task<Result<AuthResponse>> Refresh(string refreshToken);
    Task<Result<UserModel>> GetCurrentUserAsync(string? token);
    ValueTask Logout();
}