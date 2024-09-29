using Microsoft.IdentityModel.Tokens;
using System.Text;

public class AuthOptions
{
    public const string ISSUER = "MyAuthServer"; // издатель токена
    public const string AUDIENCE = "MyAuthClient"; // потребитель токена
    const string KEY = "oisefjiosdfsdfjidfjifjiofjiojdfsiiojiodfjiodiojdfsfjiof";   // ключ для шифрации
    public const int LIFETIME = 1; // время жизни токена - 1 минута
    public static SymmetricSecurityKey GetSymmetricSecurityKey()
    {
        return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
    }
}

public class AppClaimTypes
{
    public const string RoleId = "roleId";
    public const string Login = "login";
    public const string UserId = "userId";
    public const string Type = "type";
}