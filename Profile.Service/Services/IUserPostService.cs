using Blog.Service.Models;

namespace Blog.Service.Services
{
    public interface IUserPostService
    {
        Task<UserViewInfo> GetUserViewPostInfoAsync(Guid postId, Guid? userId, string? address);
    }
}
