using System.ComponentModel.DataAnnotations;

namespace AuthenticationApplication.Models;

public class LoginPasswordModel
{
    [Required]
    public string Login { get; set; }
    
    [Required]
    public string Password { get; set; }

    public LoginPasswordModel()
    {
        
    }
    public LoginPasswordModel(string login, string password)
    {
        Login = login;
        Password = password;
    }   
}

public class LoginRequest
{
    [Required]
    public LoginPasswordModel Login { get; set; }

    public Dictionary<string,string> Claims {  get; set; }

    public LoginRequest(LoginPasswordModel login, Dictionary<string, string> claims)
    {
        Login = login;
        Claims = claims;
    }
}