using Profile.Service.Models;

namespace Profile.Service.Services
{
    public interface IProfileService
    {
        Task<ProfileModel> CreateProfileAsync(ProfileCreateModel profileCreateModel);
        Task<ProfileModel> GetProfileByUserIdAsync(Guid userId);
        Task<Guid?> GetProfileIdByUserIdIfExistsAsync(Guid userId);
        Task<ProfileModel> UpdateProfileAsync(ProfileUpdateModel profileEditModel);
        Task DeleteProfileByUserIdAsync(Guid userId);
        Task SubscribeToBlog(Guid blogId);
        Task UnSubscribeFromBlog(Guid blogId);
        Task<bool> CheckForViewAsync(Guid? userId, string? ipAddress);
    }
}
