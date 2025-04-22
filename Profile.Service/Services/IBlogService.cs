using Blog.Service.Models.Blog;

namespace Blog.Service.Services
{
    public interface IBlogService
    {
        Task<BlogModel> CreateBlogAsync(BlogCreateDto model);
        Task<BlogModel> UpdateBlogAsync(BlogEditDto model);
        Task DeleteBlogAsync(Guid id);
        Task<BlogModel> GetBlogByIdAsync(Guid id);
        Task<BlogModel> GetBlogByPostIdAsync(Guid id);
        Task<BlogUserInfoViewModel> GetBlogByPostIdAsync(Guid id,Guid?userId);
        Task<BlogModel> GetBlogByUserIdAsync(Guid userId);
    }
}
