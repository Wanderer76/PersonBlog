using Blog.Contracts.Models.Blog;
using Shared.Utils;

namespace Blog.Contracts.Services;

public interface IBlogService
{
    Task<Result<BlogModel>> CreateBlogAsync(BlogCreateRequest model);
    Task<BlogModel> UpdateBlogAsync(BlogEditRequest model);
    Task<Result> DeleteBlogAsync(Guid id);
    Task<BlogModel> GetBlogByIdAsync(Guid id);
    Task<BlogModel> GetBlogByPostIdAsync(Guid id);
    Task<BlogUserInfoViewModel> GetBlogByPostIdAsync(Guid id, Guid? userId);
    Task<BlogModel> GetBlogByUserIdAsync(Guid userId);
    Task<bool> HasUserBlogAsync(Guid userId);
}
