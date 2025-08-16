using System.ComponentModel.DataAnnotations;

namespace Profile.Domain.Models.Profile;

public class ProfileCreateModel
{
    public string? Name { get; set; }
    public DateTimeOffset? Birthdate { get; set; }
    [Required]
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public string? PhotoUrl { get; set; }
}