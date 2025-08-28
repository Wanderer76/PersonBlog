using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Profile.Domain.Entities;
using Profile.Domain.Models.Profile;
using Shared.Utils;
using System.Net.Http.Json;
using System.Text.Json;

namespace Profile.Service.HttpClients
{
    public class ProfileHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProfileHttpClient(HttpClient httpClient, ICacheService cacheService, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _cacheService = cacheService;
            this._httpContextAccessor = httpContextAccessor;
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

        public async Task<Result<ProfileModel>> GetMyProfileAsync()
        {

            var request = new HttpRequestMessage(HttpMethod.Get, "Profile/profile/my");
            AddRequestHeaders(request);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProfileModel>();
            }
            else
            {
                return Result<ProfileModel>.Failure(new Error("Not found"));
            }
        }

        private void AddRequestHeaders(HttpRequestMessage request)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                // Копируем нужные заголовки
                var headersToForward = new[] { "Authorization", "X-Request-Id", "X-Correlation-Id" };

                foreach (var headerName in context.Request.Headers)
                {
                    request.Headers.Add(headerName.Key, headerName.Value.ToList());
                }
            }
        }
    }
}
