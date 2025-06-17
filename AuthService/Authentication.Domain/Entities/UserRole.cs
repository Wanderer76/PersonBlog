using Authentication.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Authentication.Domain.Entities;

public class UserRole : IAuthEntity
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    [StringLength(20)]
    public string Name { get; set; }

    public List<AppUserRole> AppUserRole { get; set; }
}

public static class Roles
{
    public static readonly Guid AdminRole = Guid.Parse("57a2b99b-b6ee-4c98-a1f0-b18fe96dae60");
    public static readonly Guid SuperAdminRole = Guid.Parse("accbc12f-6ff1-4343-a26f-13b99e64abb6");
    public static readonly Guid User = Guid.Parse("d95ca3d6-0f63-4b48-a54f-1202f3d6bf2c");
    public static readonly Guid Blogger = Guid.Parse("68816c1b-fdfb-45d0-bfea-dceb04277a57");
}