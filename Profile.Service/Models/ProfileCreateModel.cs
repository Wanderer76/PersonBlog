using System.ComponentModel.DataAnnotations;

namespace Profile.Service.Models;

public class ProfileCreateModel
{
    public string? FirstName { get; set; } 
    public string? SurName { get; set; } 
    public string? LastName { get; set; }
    public DateTimeOffset? Birthdate { get; set; }
    [Required]
    public Guid UserId { get; set; }
    public string Email {  get; set; }
    public string? PhotoUrl { get;set; }
}