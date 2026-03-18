using System.Security.Cryptography;

namespace Shared.Services;
public static class RandomCodeGenerator
{
    public static string GenerateRandomCode()
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_");
        }
    }
}
