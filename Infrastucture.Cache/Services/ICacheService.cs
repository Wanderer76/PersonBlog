using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Cache.Services
{
    public interface ICacheService
    {
        Task<T?> GetCachedData<T>(string key);
        Task SetCachedData<T>(string key, T data, TimeSpan ttl) where T : notnull;
    }

    internal class DefaultCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;

        public DefaultCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<T?> GetCachedData<T>(string key)
        {
            var result = await _cache.GetAsync(key);
            if (result == null)
            {
                return default;
            }
            return JsonSerializer.Deserialize<T>(result);
        }

        public async Task SetCachedData<T>(string key, T data, TimeSpan ttl) where T : notnull
        {
            var result = JsonSerializer.Serialize(data);
            await _cache.SetAsync(key, Encoding.UTF8.GetBytes(result), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            });
        }
    }
}
