using Authentication.Service.Models;
using AuthenticationApplication.Models;

namespace AuthenticationApplication.Service;

public interface IAuthService
{
    Task<AuthResponse> Authenticate(LoginModel loginModel);
    Task<AuthResponse> Register(RegisterModel registerModel);
    Task<AuthResponse> Refresh(string refreshToken);
    Task<bool> ValidateToken(string token);
    ValueTask Logout();
}