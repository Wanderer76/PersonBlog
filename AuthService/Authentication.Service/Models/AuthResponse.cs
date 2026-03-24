using System.Text.Json.Serialization;

namespace Authentication.Service.Models;

public class AuthResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = null!;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = null!;
}

public class AuthCodeResponse
{
    public string AuthCode { get; set; } = null!;
}
