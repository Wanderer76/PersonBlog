
using Blog.Service.Models.Blog;
using Infrastructure.Services;
using Shared.Utils;
using System.Net.Http.Json;

namespace Blog.Contracts;
public sealed class BlogApiClient
{
    private readonly HttpClient _httpClient;
    public BlogApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<BlogModel>> GetBlogDetailsAsync(Guid blogId)
    {
        var result =  await _httpClient.GetAsync($"Blog/blog/{blogId}");
        if (result.IsSuccessStatusCode)
        {
            return await result.Content.ReadFromJsonAsync<BlogModel>();
        }
        return new Error(nameof(blogId), "Блог не найден");
    }
}