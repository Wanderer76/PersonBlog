using Blog.Contracts.Models.Category;

namespace Blog.Contracts.Services
{
    public interface ICategoryService
    {
        Task<IReadOnlyList<CategoryModel>> GetAllCategoriesAsync();
        Task<IReadOnlyList<CategoryModel>> GetCategoriesByIdsAsync(IEnumerable<int> ids);
    }
}
