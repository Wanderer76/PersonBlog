using Shared.Services;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Services
{
    internal class RedisCacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            IgnoreReadOnlyProperties = false,
            WriteIndented = true,
        };
        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task<T?> GetCachedDataAsync<T>(string key)
        {
            var result = await _redis.GetDatabase().StringGetAsync(key);
            if (!result.HasValue)
            {
                return default;
            }
            return JsonSerializer.Deserialize<T>(result!, _serializerOptions);
        }

        public async Task<IEnumerable<T>> GetCachedDataAsync<T>(IEnumerable<string> keys)
        {
            var db = _redis.GetDatabase();
            var redisKeys = keys.Select(x => new RedisKey(x)).ToArray();
            var result = await db.StringGetAsync(redisKeys);
            return result == null
                ? []
                : result
                .Where(x => x.HasValue)
                .Select(obj => JsonSerializer.Deserialize<T>(obj!, _serializerOptions)!);
        }

        public Task<T?> GetCachedDataAsync<T>(ICacheKey key)
        {
            return GetCachedDataAsync<T>(key.GetKey());
        }

        public Task<IEnumerable<T>> GetCachedDataAsync<T>(IEnumerable<ICacheKey> keys)
        {
            return GetCachedDataAsync<T>(keys.Select(x => x.GetKey()));
        }

        public async Task RemoveCachedDataAsync(string key)
        {
            await _redis.GetDatabase().StringGetDeleteAsync(new RedisKey(key));
        }

        public Task RemoveCachedDataAsync(ICacheKey key)
        {
            return RemoveCachedDataAsync(key.GetKey());
        }

        public async Task SetCachedDataAsync<T>(string key, T data, TimeSpan ttl) where T : notnull
        {
            var result = JsonSerializer.Serialize(data, _serializerOptions);
            await _redis.GetDatabase().StringSetAsync(key, Encoding.UTF8.GetBytes(result), ttl);
        }

        public Task SetCachedDataAsync<T>(ICacheKey key, T data, TimeSpan ttl) where T : notnull
        {
            return SetCachedDataAsync(key.GetKey(), data, ttl);
        }
    }
}
