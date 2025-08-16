using Authentication.Contract.Events;
using Profile.Domain.Models.Profile;

namespace Profile.Domain.Services
{
    public interface IProfileService
    {
        Task<ProfileModel> CreateProfileAsync(ProfileRegisterEvent profileCreateModel);
        Task<ProfileModel> GetProfileByUserIdAsync(Guid userId);
        Task<Guid?> GetProfileIdByUserIdIfExistsAsync(Guid userId);
        Task<ProfileModel> UpdateProfileAsync(ProfileUpdateModel profileEditModel);
        Task DeleteProfileByUserIdAsync(Guid userId);
    }
}
