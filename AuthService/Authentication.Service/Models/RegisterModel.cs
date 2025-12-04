using System.ComponentModel.DataAnnotations;

namespace AuthenticationApplication.Models;

public class RegisterModel
{
    [Required] public string Login { get; set; } = null!;
    [Required] public string Password { get; set; } = null!;

    [Required]
    [Compare(nameof(Password), ErrorMessage = "пароли должны совпадать")]
    public string PasswordConfirm { get; set; } = null!;
    [Required]
    public string Name { get; set; } 

    public DateTimeOffset? Birthdate { get; set; }

    [Required]
    public string Email { get; set; }

    public string? RedirectUrl {  get; set; }
}