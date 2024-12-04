using System.ComponentModel.DataAnnotations;

namespace Profile.Service.Models;

public class ProfileCreateModel
{
    [Required]
    public string FirstName { get; set; } = null!;
    [Required]
    public string SurName { get; set; } = null!;
    public string? LastName { get; set; }
    public DateTimeOffset? Birthdate { get; set; }
    [Required]
    public Guid UserId { get; set; }
    public string Email {  get; set; }
}