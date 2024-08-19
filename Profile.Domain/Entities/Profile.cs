using System.ComponentModel.DataAnnotations;

namespace Profile.Domain.Entities;

public class Profile : IProfileEntity
{
    [Key] public Guid Id { get; set; }

    [Required] public string FirstName { get; set; } = string.Empty;

    [Required] public string SurName { get; set; } = string.Empty;

    public string? LastName { get; set; }

    public DateTimeOffset Birthdate { get; set; }

    public Guid UserId { get; set; } = Guid.Empty;

    public string? PhotoUrl { get; set; }
    
    public ProfileState ProfileState { get; set; }
}