using Infrastructure.Services;
using Profile.Domain.Entities;
using Profile.Domain.Models.Profile;
using System.Net.Http.Json;
using System.Text.Json;

namespace Profile.Service.HttpClients
{
    public class ProfileHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;

        public ProfileHttpClient(HttpClient httpClient, ICacheService cacheService)
        {
            _httpClient = httpClient;
            _cacheService = cacheService;
        }

        public async Task<ProfileModel> GetProfileByUserIdAsync(Guid userId)
        {
            var key = new AppProfileCacheKey(userId);
            var cachedResult = await _cacheService.GetCachedDataAsync<ProfileModel>(key);

            if (cachedResult != null)
            {
                return cachedResult;
            }

            var response = await _httpClient.GetFromJsonAsync<ProfileModel?>($"Profile/profile/{userId}");
            if (response != null)
            {
                await _cacheService.SetCachedDataAsync(key, response!, TimeSpan.FromMinutes(10));
                return response!;
            }
            else
            {
                return default(ProfileModel);
            }
        }
    }
}
