using Blog.Contracts.Models;

namespace Blog.Contracts.Services
{
    public interface IUserPostService
    {
        Task<UserViewInfo> GetUserViewPostInfoAsync(Guid postId, Guid? userId, string? address);
    }
}
