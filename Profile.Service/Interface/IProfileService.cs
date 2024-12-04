using Profile.Service.Models;

namespace Profile.Service.Interface
{
    public interface IProfileService
    {
        Task<ProfileModel> CreateProfileAsync(ProfileCreateModel profileCreateModel);
        Task<ProfileModel> GetProfileByUserIdAsync(Guid userId);
        Task<ProfileModel> UpdateProfileAsync(ProfileEditModel profileEditModel);
        Task DeleteProfileByUserIdAsync(Guid userId);
        Task SubscribeToBlog(Guid blogId);
        Task UnSubscribeFromBlog(Guid blogId);
    }
}
