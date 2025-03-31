using Shared.Services;

namespace Infrastructure.Services
{
    public interface ICacheService
    {
        Task<T?> GetCachedDataAsync<T>(string key);
        Task<T?> GetCachedDataAsync<T>(ICacheKey key);
        Task<IEnumerable<T>> GetCachedDataAsync<T>(IEnumerable<string> keys);
        Task<IEnumerable<T>> GetCachedDataAsync<T>(IEnumerable<ICacheKey> keys);
        Task SetCachedDataAsync<T>(string key, T data, TimeSpan ttl) where T : notnull;
        Task SetCachedDataAsync<T>(ICacheKey key, T data, TimeSpan ttl) where T : notnull;
        Task RemoveCachedDataAsync(string key);
        Task RemoveCachedDataAsync(ICacheKey key);
    }
}
