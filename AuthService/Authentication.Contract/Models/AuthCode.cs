using Shared.Services;

namespace Authentication.Contract.Models;
public class AuthCode
{
    public string Code { get; set; } = null!;
    public Guid UserId { get; set; }
    public string ClientId { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }

    private class CacheKey(string Code) : ICacheKey
    {
        public string GetKey() => $"{nameof(AuthCode)}:{Code}";
    }
    public static ICacheKey GetCacheKey(string Code) => new CacheKey(Code);
}

