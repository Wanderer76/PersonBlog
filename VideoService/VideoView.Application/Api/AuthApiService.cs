using Authentication.Service.Models;
using AuthenticationApplication.Models;
using Shared.Models;
using Shared.Utils;
using System.Text.Json;

namespace VideoView.Application.Api
{
    public static class AuthApiService
    {
        private const string ClientName = "Auth";

        public static async Task<Result<AuthResponse>> CreateUserAsync(this IHttpClientFactory httpClientFactory, RegisterModel registerModel)
        {
            var response = await httpClientFactory.CreateClient(ClientName).PostAsJsonAsync("Auth/create", registerModel);
            if (response.IsSuccessStatusCode)
            {
                return await JsonSerializer.DeserializeAsync<AuthResponse>(response.Content.ReadAsStream());
            }
            return new Error(await response.Content.ReadAsStringAsync());
        }

        public static async Task<Result<AuthResponse>> AuthenticateAsync(this IHttpClientFactory httpClientFactory, LoginPasswordModel loginModel)
        {
            var client = httpClientFactory.CreateClient(ClientName);
            //foreach (var i in HttpContext.Request.Headers)
            //{
            //    client.DefaultRequestHeaders.TryAddWithoutValidation(i.Key, i.Value.ToArray());
            //}
            var response = await client.PostAsJsonAsync("Auth/login", loginModel);
            if (response.IsSuccessStatusCode)
            {
                return await JsonSerializer.DeserializeAsync<AuthResponse>(response.Content.ReadAsStream());
            }
            return new Error(await response.Content.ReadAsStringAsync());
        }
        public static async Task UpdateSessionAsync(this IHttpClientFactory httpClientFactory, HttpContext context)
        {
            var client = httpClientFactory.CreateClient(ClientName);
            foreach (var i in context.Request.Headers)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(i.Key, i.Value.ToArray());
            }
            var a = await client.GetFromJsonAsync<Guid>("Auth/session");
            context.Response.Cookies.Append(SessionKey.Key, a.ToString());
        }

        public static async Task<Result<AuthResponse>> RefreshAsync(this IHttpClientFactory httpClientFactory, HttpContext context, string refreshToken)
        {
            var client = httpClientFactory.CreateClient(ClientName);
            foreach (var i in context.Request.Headers)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(i.Key, i.Value.ToArray());
            }
            var response = await client.PostAsync($"Auth/refresh?refreshToken={refreshToken}", null);
            if (response.IsSuccessStatusCode)
            {
                return await JsonSerializer.DeserializeAsync<AuthResponse>(response.Content.ReadAsStream());
            }
            return new Error(await response.Content.ReadAsStringAsync());
        }
    }
}
