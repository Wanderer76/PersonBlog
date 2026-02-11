using Blog.Contracts.Models.Category;
using Blog.Contracts.Services;
using Blog.Domain.Entities;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;

namespace Blog.Service.Services.Implementation;

internal class DefaultCategoryService : ICategoryService
{
    private readonly IReadRepository<IBlogEntity> _repository;
    private readonly ICacheService _cacheService;

    public DefaultCategoryService(IReadRepository<IBlogEntity> repository, ICacheService cacheService)
    {
        _repository = repository;
        _cacheService = cacheService;
    }

    public async Task<IReadOnlyList<CategoryModel>> GetAllCategoriesAsync()
    {
        return await _cacheService.GetOrAddDataAsync(new CategoryListCacheKey(), async () =>
        {
            return await _repository.Get<Category>()
            .OrderBy(x => x.Title)
            .AsAsyncEnumerable()
            .Select(x => new CategoryModel(x.Id, x.Title))
            .ToListAsync();
        });
    }

    public async Task<IReadOnlyList<CategoryModel>> GetCategoriesByIdsAsync(IEnumerable<int> ids) => [.. (await GetAllCategoriesAsync()).Where(x => ids.Contains(x.Id))];
}

class CategoryListCacheKey : ICacheKey
{
    private const string Key = "CategoryListCacheKey";

    public string GetKey() => Key;
}
