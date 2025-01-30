using Profile.Service.Models.Blog;

namespace Profile.Service.Interface
{
    public interface IBlogService
    {
        Task<BlogModel> CreateBlogAsync(BlogCreateDto model);
        Task<BlogModel> UpdateBlogAsync(BlogEditDto model);
        Task DeleteBlogAsync(Guid id);
        Task<BlogModel> GetBlogAsync(Guid id);
        Task<BlogModel> GetBlogByPostIdAsync(Guid id);
        Task<BlogModel> GetBlogByUserIdAsync(Guid userId);
    }
}
