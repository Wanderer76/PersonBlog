using Shared.Services;

namespace Authentication.Service.Models;
internal sealed class AuthCode
{
    public string Code { get; set; } = null!;
    public Guid UserId { get; set; }
    public string ClientId { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public static ICacheKey GetCacheKey(string Code) => new CacheKey(Code);

    private class CacheKey(string Code) : ICacheKey
    {
        public string GetKey() => $"{nameof(AuthCode)}:{Code}";
    }
}

