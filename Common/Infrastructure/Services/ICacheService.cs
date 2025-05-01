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

    public static class CacheServiceExtensions
    {
        public static async Task<T> GetOrAddDataAsync<T>(this ICacheService cache, ICacheKey key, Func<Task<T>> store, long ttlInMinutes = 10)
        {
            var data = await cache.GetCachedDataAsync<T>(key);
            if (data == null)
            {
                var result = await store.Invoke();
                await cache.SetCachedDataAsync(key, result, TimeSpan.FromMinutes(ttlInMinutes));
                data = result;
            }
            return data;
        }
    }
}
