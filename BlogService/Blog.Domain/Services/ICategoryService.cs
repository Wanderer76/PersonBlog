using Blog.Domain.Services.Models.Category;

namespace Blog.Domain.Services
{
    public interface ICategoryService
    {
        Task<IReadOnlyList<CategoryModel>> GetAllCategoriesAsync();
        Task<IReadOnlyList<CategoryModel>> GetCategoriesByIdsAsync(IEnumerable<int> ids);
    }
}
