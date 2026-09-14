using Profile.Domain.Entities;

namespace Profile.Application.Models.Profile;

public class ProfileModel
{
    public long Id { get; set; }
    public string Name { get; set; }
    public Guid UserId { get; set; }
    public string? PhotoUrl { get; set; }
    public ProfileState ProfileState { get; set; }

    public ProfileModel()
    {
        
    }

    public ProfileModel(long id, string name, Guid userId, string? photoUrl, ProfileState profileState)
    {
        Id = id;
        Name = name;
        UserId = userId;
        PhotoUrl = photoUrl;
        ProfileState = profileState;
    }
}
