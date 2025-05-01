using System.ComponentModel.DataAnnotations;

namespace AuthenticationApplication.Models;

public class LoginModel
{
    [Required]
    public string Login { get; set; }
    
    [Required]
    public string Password { get; set; }

    public LoginModel()
    {
        
    }
    public LoginModel(string login, string password)
    {
        Login = login;
        Password = password;
    }
    
}