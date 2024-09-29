using Microsoft.IdentityModel.Tokens;
using System.Text;

public class AuthOptions
{
    public const string ISSUER = "MyAuthServer"; // �������� ������
    public const string AUDIENCE = "MyAuthClient"; // ����������� ������
    const string KEY = "oisefjiosdfsdfjidfjifjiofjiojdfsiiojiodfjiodiojdfsfjiof";   // ���� ��� ��������
    public const int LIFETIME = 1; // ����� ����� ������ - 1 ������
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