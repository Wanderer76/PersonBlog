using Conference.Domain.Entities;
using Infrastructure.Services;

namespace Conference.Service.Extensions
{
    internal static class CacheExtensions
    {
        public static Task<ConferenceRoom> GetConferenceRoomCacheAsync(this ICacheService cacheService, ConferenceRoomKey key)
        {
            return cacheService.GetCachedDataAsync<ConferenceRoom>(key)!;
        }

        public static Task UpdateConferenceRoomCacheAsync(this ICacheService cacheService, ConferenceRoom room)
        {
            return cacheService.SetCachedDataAsync(new ConferenceRoomKey(room.Id), room, TimeSpan.FromMinutes(10));
        }

        public static Task RemoveConferenceRoomCacheAsync(this ICacheService cacheService, ConferenceRoomKey key)
        {
            return cacheService.RemoveCachedDataAsync(key);
        }
    }
}
