using Profile.Service.Models;

namespace Profile.Service.Interface
{
    public interface IAdminService
    {
        Task<IEnumerable<ProfileModel>> GetAllProfilesAsync(int offset, int limit);
        Task<IEnumerable<ProfileModel>> GetProfilesByLoginAsync(string login);
        Task<ProfileModel> GetProfileByUserId(Guid userId);
    }
}
