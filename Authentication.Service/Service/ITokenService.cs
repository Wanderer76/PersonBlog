using Authentication.Domain.Entities;
using Authentication.Service.Models;
using Shared;

namespace Authentication.Service.Service
{
    public interface ITokenService
    {
        Task ClearUserToken(string token);
        bool Validate(string token);
        AuthResponse GenerateToken(AppUser user);
        TokenModel GetTokenRepresentaion(string token);
    }
}
