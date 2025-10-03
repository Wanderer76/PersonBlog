using Microsoft.AspNetCore.Http;
using Shared.Utils;
using System.Net.Http.Json;

namespace MusicRecommendation.Services
{
    public class MusicRecommendationHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor httpContextAccessor;

        public MusicRecommendationHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result<List<Guid>>> GetRecommendationFotUser(int page, int size)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"Recommendation/user?page={page}&size={size}");
            AddRequestHeaders(request);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<Guid>>();
            }
            else
            {
                return Result<List<Guid>>.Failure(new Error("Not found"));
            }
        }

        private void AddRequestHeaders(HttpRequestMessage request)
        {
            var context = httpContextAccessor.HttpContext;
            foreach (var headerName in context.Request.Headers)
            {
                request.Headers.Add(headerName.Key, headerName.Value.ToList());
            }
        }
    }
}
