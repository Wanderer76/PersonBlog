using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Cache.Services
{
    public interface ICacheService
    {
        Task<T?> GetCachedDataAsync<T>(string key);
        Task<IEnumerable<T>> GetCachedDataAsync<T>(IEnumerable<string> keys);
        Task SetCachedDataAsync<T>(string key, T data, TimeSpan ttl) where T : notnull;
        Task RemoveCachedDataAsync(string key);
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

        public async Task<T?> GetCachedDataAsync<T>(string key)
        {
            var result = await _redis.GetDatabase().StringGetAsync(key);
            if (!result.HasValue)
            {
                return default;
            }
            return JsonSerializer.Deserialize<T>(result);
        }

        public async Task<IEnumerable<T>> GetCachedDataAsync<T>(IEnumerable<string> keys)
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

        public async Task RemoveCachedDataAsync(string key)
        {
            await _redis.GetDatabase().StringGetDeleteAsync(new RedisKey(key));
        }

        public async Task SetCachedDataAsync<T>(string key, T data, TimeSpan ttl) where T : notnull
        {
            try
            {
                var result = JsonSerializer.Serialize(data);

                await _redis.GetDatabase().StringSetAsync(key, Encoding.UTF8.GetBytes(result), ttl);
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
            }
        }
    }


}
