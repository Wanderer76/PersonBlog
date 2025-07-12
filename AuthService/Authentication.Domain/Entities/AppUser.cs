using System.ComponentModel.DataAnnotations;

namespace Authentication.Domain.Entities;

public class AppUser : IAuthEntity
{
    [Key] public Guid Id { get; set; }
    [Required] public string Login { get; set; }

    [Required] 
    public string Password { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastAuthenticate { get; set; }
    
    public List<AppUserRole> AppUserRoles { get; set; }
}