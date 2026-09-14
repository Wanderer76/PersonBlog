using Authentication.Contract.Events;
using Profile.Application.Models.Profile;

namespace Profile.Application.Services;

public interface IProfileService
{
    Task<ProfileModel> CreateProfileAsync(ProfileRegisterEvent profileCreateModel);
    Task<ProfileModel> GetProfileByUserIdAsync(Guid userId);
    Task<Guid?> GetProfileIdByUserIdIfExistsAsync(Guid userId);
    Task<ProfileModel> UpdateProfileAsync(ProfileUpdateModel profileEditModel);
    Task DeleteProfileByUserIdAsync(Guid userId);
}
