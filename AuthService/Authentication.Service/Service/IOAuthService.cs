using Authentication.Service.Models;

namespace Authentication.Service.Service;
public interface IOAuthService
{
    Task<Result<RedirectResponse>> GenerateAuthCodeAsync(string clientId, string redirectUri, string response_type, string state, string returnUrl);
    Task<Result<AuthResponse>> ExchangeCodeToTokenAsync(string grant_type, string code, string client_id, string client_secret);
}
