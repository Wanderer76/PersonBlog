using System.Text.Json.Serialization;

namespace Authentication.Service.Models.Options;

public sealed class TokenOptions
{
    public double AccessTokenExpiredInMinutes { get; }
    public double RefreshTokenExpiredInMinutes { get; }

    public TokenOptions(double accessTokenExpiredInMinutes, double refreshTokenExpiredInMinutes)
    {
        AccessTokenExpiredInMinutes = accessTokenExpiredInMinutes;
        RefreshTokenExpiredInMinutes = refreshTokenExpiredInMinutes;
    }
}
