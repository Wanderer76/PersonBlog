using Profile.Domain.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Profile.Service.Models
{
    public class ProfileModel
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string SurName { get; set; }
        public string Email { get; set; }
        public string? LastName { get; set; }
        public DateTimeOffset? Birthdate { get; set; }
        public Guid UserId { get; set; }
        public string? PhotoUrl { get; set; }
        public ProfileState ProfileState { get; set; }
    }

    internal static class ProfileModelExtensions
    {
        public static ProfileModel ToProfileModel(this AppProfile profile)
        {
            return new ProfileModel
            {
                Id = profile.Id,
                UserId = profile.UserId,
                Birthdate = profile.Birthdate,
                Email = profile.Email,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                PhotoUrl = profile.PhotoUrl,
                ProfileState = profile.ProfileState,
                SurName = profile.SurName,
            };
        }
    }
}
