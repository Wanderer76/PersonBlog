using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Authentication.Domain.Entities;

public class AppUserRole : IAuthEntity
{
    [Key] public Guid AppUserId { get; set; }
    public Guid UserRoleId { get; set; }
    [ForeignKey(nameof(AppUserId))] public AppUser AppUser { get; set; } = null!;
    [ForeignKey(nameof(UserRoleId))] public UserRole UserRole { get; set; } = null!;
}