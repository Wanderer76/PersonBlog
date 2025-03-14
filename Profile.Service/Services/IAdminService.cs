using Blog.Service.Models;

namespace Blog.Service.Services
{
    public interface IAdminService
    {
        Task<IEnumerable<ProfileModel>> GetAllProfilesAsync(int offset, int limit);
        Task<IEnumerable<ProfileModel>> GetProfilesByLoginAsync(string login);
        Task<ProfileModel> GetProfileByUserId(Guid userId);
    }
}
