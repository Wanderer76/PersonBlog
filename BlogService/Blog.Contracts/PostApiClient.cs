using Amazon.Runtime.Internal;
using Authentication.Contract.Constants;
using Blog.Contracts.Models;
using Blog.Contracts.Services;
using Blog.Domain.Entities;
using Infrastructure.Middleware;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Services;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;

namespace Blog.Contracts;
public sealed class PostApiClient
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
    public async Task<Result<IReadOnlyList<PostCommonModel>>> GetCurrentUserPostListAsync()
    {
        var result = await httpClient.GetAsync($"Post/my/list");
        if (result.IsSuccessStatusCode)
        {
            return await result.Content.ReadFromJsonAsync<List<PostCommonModel>>();
        }
        return Result<IReadOnlyList<PostCommonModel>>.Success([]);
    }

    [HttpGet("create")]
    public async Task<CreatePostModelViewModel> GetPostCreateModelAsync()
    {
        var model = await httpClient.GetFromJsonAsync<CreatePostModelViewModel>("ProfilePostV2/create");
        return model!;
    }

    [HttpPost("createTextPost")]
    public async Task<Result<UserPostInfoModel>> CreateTextPostAsync([FromForm] TextPostCreateForm request)
    {
        if (string.IsNullOrWhiteSpace(request.Text) && request.Media == null)
            return Result<UserPostInfoModel>.Failure(new Shared.Utils.Error("no content"));

        using var formData = new MultipartFormDataContent();
        formData.Add(new StringContent(request.Title), "Title");
        formData.Add(new StringContent(request.Text), "Text");
        formData.Add(new StringContent(request.Visibility.ToString()), "Visibility");

        if (request.Media != null)
        {
            foreach (var file in request.Media)
            {
                var streamContent = new StreamContent(file.OpenReadStream());
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                formData.Add(streamContent, "Media", file.FileName);
            }
        }
        var response = await httpClient.PostAsync($"ProfilePostV2/createTextPost", formData);
        return await response.Content.ReadFromJsonAsync<UserPostInfoModel>();
    }
}

file record PostCommonCacheKey(int Id) : ICacheKey
{
    public string GetKey() => $"{nameof(PostCommonCacheKey)}:{Id}";
}
