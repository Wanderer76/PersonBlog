using Profile.Service.Models;

namespace Profile.Service.Services
{
    public interface IUserPostService
    {
        Task<UserViewInfo> GetUserViewPostInfoAsync(Guid postId, Guid? userId, string? address);
    }
}
