using Shared;
using Shared.Utils;
using System.ComponentModel.DataAnnotations;

namespace Authentication.Domain.Entities;

public class AppProfile : BaseEntity, IAuthEntity
{
    [Key]
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    public Guid UserId { get; set; }
    public string? PhotoUrl { get; set; }

    public AppProfile() { }

    internal AppProfile(string email, string name, Guid userId)
    {
        Email = email;
        UserId = userId;
        Name = name;
        IsDeleted = false;
    }

    public static AppProfile Create(string email, string name, Guid userId)
    {
        return new AppProfile(email, name, userId);
    }
}
