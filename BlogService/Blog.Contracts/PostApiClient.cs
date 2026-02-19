using Blog.Contracts.Models;
using Infrastructure.Services;
using Shared.Services;
using System.Net.Http.Json;

namespace Blog.Contracts;
public class PostApiClient
{
    private readonly HttpClient httpClient;
    private readonly ICacheService _cacheService;

    public PostApiClient(HttpClient httpClient, ICacheService cacheService)
    {
        this.httpClient = httpClient;
        _cacheService = cacheService;
    }

    public async Task<IReadOnlyList<PostCommonModel>> GetPostCommonModelAsync(IEnumerable<Guid> ids)
    {
        return await _cacheService.GetOrAddDataAsync(new PostCommonCacheKey(ids.GetHashCode()), async () =>
        {
            var result = await httpClient.PostAsJsonAsync($"Post/commonByIds", ids);
            if (result.IsSuccessStatusCode)
                return (await result.Content.ReadFromJsonAsync<List<PostCommonModel>>())!;
            return [];
        }, 1);
    }
    public async Task<IReadOnlyList<PostCommonModel>> GetCurrentUserPostCommonModelWithExcludeIdsAsync(IEnumerable<Guid> excludeIds)
    {
        var result = await httpClient.PostAsJsonAsync($"Post/commonWithExcludeIds", excludeIds);
        if (result.IsSuccessStatusCode)
            return (await result.Content.ReadFromJsonAsync<List<PostCommonModel>>())!;
        return [];
    }
}

file record PostCommonCacheKey(int Id) : ICacheKey
{
    public string GetKey() => $"{nameof(PostCommonCacheKey)}:{Id}";
}
