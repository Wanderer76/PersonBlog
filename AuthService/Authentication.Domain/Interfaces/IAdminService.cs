using Authentication.Domain.Interfaces.Models.Profile;

namespace Authentication.Domain.Interfaces
{
    public interface IAdminService
    {
        Task<IEnumerable<ProfileModel>> GetAllProfilesAsync(int offset, int limit);
        Task<IEnumerable<ProfileModel>> GetProfilesByLoginAsync(string login);
        Task<ProfileModel> GetProfileByUserId(Guid userId);
    }
}
