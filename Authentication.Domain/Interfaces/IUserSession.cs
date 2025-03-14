using Authentication.Domain.Interfaces.Models;
using Infrastructure.Cache.Services;
using Microsoft.AspNetCore.Http;
using Shared.Services;

namespace Authentication.Domain.Interfaces
{
    public struct SessionKey
    {
        public const string Key = "sessionId";
    }

    public interface IUserSession
    {
        Task<UserSession> GetUserSessionAsync();
        Task UpdateUserSession(string sessionId, string? token = null);
    }
}
