using Blog.Domain.Services.Models.Category;

namespace Blog.Domain.Services
{
    public interface ICategoryService
    {
        Task<List<CategoryModel>> GetAllCategoriesAsync();
    }
}
