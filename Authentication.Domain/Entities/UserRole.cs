using System.ComponentModel.DataAnnotations;

namespace Authentication.Domain.Entities;

public class UserRole  : IAuthEntity
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    [StringLength(20)]
    public string Name { get; set; }
    
    public List<AppUserRole> AppUserRole { get; set; }
}