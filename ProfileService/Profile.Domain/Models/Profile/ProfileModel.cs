using Profile.Domain.Entities;

namespace Profile.Domain.Models.Profile
{
    public class ProfileModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTimeOffset? Birthdate { get; set; }
        public Guid UserId { get; set; }
        public string? PhotoUrl { get; set; }
        public ProfileState ProfileState { get; set; }

        public ProfileModel()
        {
            
        }

        public ProfileModel(long id, string name, string email, DateTimeOffset? birthdate, Guid userId, string? photoUrl, ProfileState profileState)
        {
            Id = id;
            Name = name;
            Email = email;
            Birthdate = birthdate;
            UserId = userId;
            PhotoUrl = photoUrl;
            ProfileState = profileState;
        }
    }

    public static class ProfileModelExtensions
    {
        public static ProfileModel ToProfileModel(this AppProfile profile)
        {
            return new ProfileModel
            {
                Id = profile.Id,
                UserId = profile.UserId,
                Birthdate = profile.Birthdate,
                Email = profile.Email,
                Name = profile.Name,
                PhotoUrl = profile.PhotoUrl,
                ProfileState = profile.ProfileState,
            };
        }
    }
}
