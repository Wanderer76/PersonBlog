using Blog.Domain.Entities;
using Blog.Domain.Services;
using Blog.Domain.Services.Models.Category;
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

    public async Task<List<CategoryModel>> GetAllCategoriesAsync()
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
}

class CategoryListCacheKey : ICacheKey
{
    private const string Key = "CategoryListCacheKey";

    public string GetKey() => Key;
}
