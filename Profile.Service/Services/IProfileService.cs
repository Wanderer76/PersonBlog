using Blog.Service.Models;

namespace Blog.Service.Services
{
    public interface IProfileService
    {
        Task<ProfileModel> CreateProfileAsync(ProfileCreateModel profileCreateModel);
        Task<ProfileModel> GetProfileByUserIdAsync(Guid userId);
        Task<Guid?> GetProfileIdByUserIdIfExistsAsync(Guid userId);
        Task<ProfileModel> UpdateProfileAsync(ProfileUpdateModel profileEditModel);
        Task DeleteProfileByUserIdAsync(Guid userId);
        Task<bool> CheckForViewAsync(Guid? userId, string? ipAddress);
    }
}
