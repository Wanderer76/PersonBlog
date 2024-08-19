using AuthenticationApplication.Models;

namespace AuthenticationApplication.Service;

public interface IAuthService
{
    Task<string> Authenticate(LoginModel loginModel);
    Task<string> Register(RegisterModel registerModel);
    ValueTask Logout();
}