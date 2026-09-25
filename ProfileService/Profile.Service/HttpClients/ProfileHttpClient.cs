using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Profile.Application.Models.Profile;
using Profile.Domain.Entities;
using Shared.Utils;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;

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

        public async Task<Result<ProfileModel>> GetMyProfileAsync(CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "Profile/profile/my");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return await ReadProfileResponseAsync(response, cancellationToken);
        }

        public async Task<Result<ProfileModel>> UpdateProfileAsync(
            long profileId,
            Guid userId,
            string name,
            IFormFile? profilePicture,
            CancellationToken cancellationToken = default)
        {
            using var content = new MultipartFormDataContent
            {
                { new StringContent(profileId.ToString(CultureInfo.InvariantCulture)), "Id" },
                { new StringContent(name), "Name" },
                { new StringContent(userId.ToString()), "UserId" }
            };

            if (profilePicture is not null)
            {
                var fileContent = new StreamContent(profilePicture.OpenReadStream());
                if (MediaTypeHeaderValue.TryParse(profilePicture.ContentType, out var contentType))
                {
                    fileContent.Headers.ContentType = contentType;
                }

                content.Add(fileContent, "ProfilePicture", profilePicture.FileName);
            }

            using var response = await _httpClient.PostAsync("Profile/edit", content, cancellationToken);
            return await ReadProfileResponseAsync(response, cancellationToken);
        }

        private static async Task<Result<ProfileModel>> ReadProfileResponseAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            if (!response.IsSuccessStatusCode)
            {
                return Result<ProfileModel>.Failure(new Error(
                    response.StatusCode.ToString(),
                    $"Сервис профилей вернул ошибку {(int)response.StatusCode} ({response.ReasonPhrase})"));
            }

            var profile = await response.Content.ReadFromJsonAsync<ProfileModel>(cancellationToken);
            return profile is null
                ? Result<ProfileModel>.Failure(new Error(
                    "InvalidResponse",
                    "Сервис профилей вернул пустой ответ"))
                : Result<ProfileModel>.Success(profile);
        }
    }
}
