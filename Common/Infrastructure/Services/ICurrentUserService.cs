using Shared.Models;

namespace Infrastructure.Services
{
    public interface ICurrentUserService
    {
        Task<UserModel> GetCurrentUserAsync();
        //Task<Guid> UpdateCurrentUserAsync(string token);
    }
}
