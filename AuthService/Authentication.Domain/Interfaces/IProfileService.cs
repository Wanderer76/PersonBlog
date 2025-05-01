using Authentication.Domain.Interfaces.Models.Profile;

namespace Authentication.Domain.Interfaces
{
    public interface IProfileService
    {
        Task<ProfileModel> CreateProfileAsync(ProfileCreateModel profileCreateModel);
        Task<ProfileModel> GetProfileByUserIdAsync(Guid userId);
        Task<Guid?> GetProfileIdByUserIdIfExistsAsync(Guid userId);
        Task<ProfileModel> UpdateProfileAsync(ProfileUpdateModel profileEditModel);
        Task DeleteProfileByUserIdAsync(Guid userId);
    }
}
