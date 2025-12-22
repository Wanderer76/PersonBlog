using Infrastructure.Services;
using Shared.Models;
using System.Net.Http.Json;

namespace Authentication.Contract.Services
{
    internal class CurrentUserHttpClientService : ICurrentUserService
    {
        private readonly HttpClient _httpClient;

        public CurrentUserHttpClientService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<UserModel> GetCurrentUserAsync()
        {
            var request = await _httpClient.GetAsync("Auth/me");

            if (request.IsSuccessStatusCode)
            {
                return await request.Content.ReadFromJsonAsync<UserModel>() ?? UserModel.AnonymousUser();
            }
            else
            {
                return UserModel.AnonymousUser();
            }
        }
    }
}
