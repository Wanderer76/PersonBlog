using Blog.Service.Models.Blog;
using Shared.Utils;

namespace Blog.Service.Services;

public interface IBlogService
{
    Task<Result<BlogModel>> CreateBlogAsync(BlogCreateDto model);
    Task<BlogModel> UpdateBlogAsync(BlogEditDto model);
    Task<Result> DeleteBlogAsync(Guid id);
    Task<BlogModel> GetBlogByIdAsync(Guid id);
    Task<BlogModel> GetBlogByPostIdAsync(Guid id);
    Task<BlogUserInfoViewModel> GetBlogByPostIdAsync(Guid id,Guid?userId);
    Task<BlogModel> GetBlogByUserIdAsync(Guid userId);
    Task<Guid?> HasUserBlogAsync(Guid userId);
}
