using Authentication.Domain.Interfaces.Models;

namespace Authentication.Domain.Interfaces
{
    public readonly struct SessionKey
    {
        private readonly string Key = "sessionId";

        public SessionKey()
        {

        }

        public static implicit operator string(SessionKey sessionKey) => sessionKey.Key;
    }

    public interface IUserSession
    {
        Task<UserSession> GetUserSessionAsync();
        Task UpdateUserSession(string sessionId, string? token = null);
    }
}
