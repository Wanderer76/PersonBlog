using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Cache.Services
{
    public interface ICacheService
    {
        Task<T?> GetCachedData<T>(string key);
        Task<IEnumerable<T>> GetCachedData<T>(IEnumerable<string> keys);
        Task SetCachedData<T>(string key, T data, TimeSpan ttl) where T : notnull;
        Task RemoveCachedData(string key);
    }

    internal class DefaultCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;
        public DefaultCacheService(IDistributedCache cache, IConnectionMultiplexer redis)
        {
            _cache = cache;
            _redis = redis;
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

        public async Task<IEnumerable<T>> GetCachedData<T>(IEnumerable<string> keys)
        {
            var db = _redis.GetDatabase();
            var redisKeys = keys.Select(x => new RedisKey($"{x}")).ToArray();
            var result = await db.StringGetAsync(redisKeys);
            return result == null 
                ? []
                : result
                .Where(x => x.HasValue)
                .Select(obj => JsonSerializer.Deserialize<T>(obj!)!);
        }

        public async Task RemoveCachedData(string key)
        {
            await _redis.GetDatabase().StringGetDeleteAsync(new RedisKey(key));
        }

        public async Task SetCachedData<T>(string key, T data, TimeSpan ttl) where T : notnull
        {
            var result = JsonSerializer.Serialize(data);
            await _redis.GetDatabase().StringSetAsync(key, Encoding.UTF8.GetBytes(result), ttl);
        }
    }


}
