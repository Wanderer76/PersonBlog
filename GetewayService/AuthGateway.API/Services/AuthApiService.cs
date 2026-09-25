using Authentication.Service.Models;
using Shared.Utils;

namespace AuthGateway.API.Services;

public static class AuthApiService
{
    private const string ClientName = "Auth";

    public static async Task<Result<AuthCodeResponse>> CreateUserAsync(this IHttpClientFactory httpClientFactory, RegisterRequest registerModel)
    {
        var response = await httpClientFactory.CreateClient(ClientName).PostAsJsonAsync("Auth/create", registerModel);
        if (response.IsSuccessStatusCode)
        {
            return (await response.Content.ReadFromJsonAsync<Result<AuthCodeResponse>>())!;
        }
        return new Error(await response.Content.ReadAsStringAsync());
    }

    public static async Task<Result<AuthCodeResponse>> AuthenticateAsync(this IHttpClientFactory httpClientFactory, LoginPasswordModel loginModel)
    {
        var client = httpClientFactory.CreateClient(ClientName);
        var response = await client.PostAsJsonAsync("Auth/login", loginModel);
        return (await response.Content.ReadFromJsonAsync<Result<AuthCodeResponse>>())!;
    }

    public static async Task<Result<AuthResponse>> RefreshAsync(this IHttpClientFactory httpClientFactory, HttpContext context, string refreshToken)
    {
        var client = httpClientFactory.CreateClient(ClientName);
        foreach (var i in context.Request.Headers)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(i.Key, i.Value.ToArray());
        }
        var response = await client.PostAsJsonAsync("Auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        });
        return (await response.Content.ReadFromJsonAsync<Result<AuthResponse>>())!;
    }
}
