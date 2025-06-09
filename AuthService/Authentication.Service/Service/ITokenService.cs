using Authentication.Domain.Entities;
using Authentication.Service.Models;
using Shared;

namespace Authentication.Service.Service
{
    public interface ITokenService
    {
        Task ClearUserToken(string token);
        bool Validate(string token);
        Task<AuthResponse> GenerateTokenAsync(AppUser user);
        AuthResponse GenerateToken(AppUser user,Dictionary<string, string> claims);
        TokenModel GetTokenRepresentation(string token);
    }
}
