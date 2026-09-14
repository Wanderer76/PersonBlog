using Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Profile.Application.Models.Profile;

public class ProfileUpdateModel
{
    public long Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset? Birthdate { get; set; }
    [Required]
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public FileMetadataModel? ProfilePicture { get; set; }
}
