using System.ComponentModel.DataAnnotations;

namespace Profile.API.Dtos;

public class ProfileUpdateDto
{
    public long Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset? Birthdate { get; set; }
    [Required]
    public Guid UserId { get; set; }
    public string? Email { get; set; }
    public IFormFile? ProfilePicture { get; set; }
}
