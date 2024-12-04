using System.ComponentModel.DataAnnotations;

namespace AuthenticationApplication.Models;

public class RegisterModel
{
    [Required] public string Login { get; set; } = null!;
    [Required] public string Password { get; set; } = null!;

    [Required]
    [Compare(nameof(Password), ErrorMessage = "пароли должны совпадать")]
    public string PasswordConfirm { get; set; } = null!;

    [Required] public string Name { get; set; } = null!;
    [Required] public string Surname { get; set; } = null!;
    public string? Lastname { get; set; }
    public DateTimeOffset Birthdate { get; set; }
    [Required]
    public string Email { get; set; }
 //   [Required] public IEnumerable<Guid> UserRoleIds { get; set; } = new List<Guid>();
}