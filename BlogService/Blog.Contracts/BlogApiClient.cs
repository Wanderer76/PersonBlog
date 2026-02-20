using Blog.Contracts.Models;
using Blog.Contracts.Models.Blog;
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
        var response = await _httpClient.GetAsync($"Blog/blog/{blogId}");
        return await HandleResponseAsync<BlogModel>(response);
    }

    public async Task<Result<BlogModel>> CreateBlogAsync(BlogCreateRequest form)
    {

        using var content = new MultipartFormDataContent
        {
            { new StringContent(form.Title), "Title" },
            { new StringContent(form.Description), "Description" }
        };

        if (form.PhotoUrl != null)
        {
            content.Add(new StreamContent(form.PhotoUrl.OpenReadStream()), "PhotoUrl", form.PhotoUrl.FileName);
        }

        var response = await _httpClient.PostAsync("create", content);
        return await HandleResponseAsync<BlogModel>(response);
    }

    public async Task<Result<bool>> HasUserBlogAsync(Guid userId)
    {
        var response = await _httpClient.GetAsync($"Blog/hasBlog/{userId}");
        return await HandleResponseAsync<bool>(response);
    }

    public async Task<Result<BlogModel>> GetBlogByUserIdAsync(Guid userId)
    {
        var response = await _httpClient.GetAsync($"Blog/detail?userId={userId}");
        return await HandleResponseAsync<BlogModel>(response);
    }

    public async Task<Result<List<SubscriptionLevelModel>>> GetAllSubscriptionsAsync()
    {
        var response = await _httpClient.GetAsync("Blog/subscriptionLevelCreate");
        return await HandleResponseAsync<List<SubscriptionLevelModel>>(response);
    }

    public async Task<Result<SubscriptionLevelModel>> CreateSubscriptionAsync(SubscriptionCreateDto form)
    {
        var response = await _httpClient.PostAsJsonAsync("Blog/subscriptionLevelCreate", form);
        return await HandleResponseAsync<SubscriptionLevelModel>(response);
    }

    public async Task<Result<BlogModel>> GetBlogByPostIdAsync(Guid postId, Guid? viewerId = null)
    {
        var url = viewerId.HasValue
            ? $"Blog/blogViewerInfoByPost/{postId}?viewerId={viewerId}"
            : $"Blog/blogByPost/{postId}";

        var response = await _httpClient.GetAsync(url);
        return await HandleResponseAsync<BlogModel>(response);
    }

    public async Task<Result<BlogModel>> GetBlogByIdAsync(Guid blogId)
    {
        var response = await _httpClient.GetAsync($"Blog/blog/{blogId}");
        return await HandleResponseAsync<BlogModel>(response);
    }

    private static async Task<Result<T>> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadFromJsonAsync<T>();
            return Result<T>.Success(content);
        }

        var errors = await response.Content.ReadFromJsonAsync<List<Error>>();
        return Result<T>.Failure(errors ?? new List<Error> { new Error("ServerError", "Ошибка сервиса блога") });
    }
    //public async Task<Result<BlogModel>> UpdateBlogAsync(Guid blogId, BlogUpdateRequest form)
    //{
    //    var response = await _httpClient.PutAsJsonAsync($"blog/{blogId}", form);
    //    return await HandleResponseAsync<BlogModel>(response);
    //}

    //public async Task<Result<Unit>> DeleteBlogAsync(Guid blogId)
    //{
    //    var response = await _httpClient.DeleteAsync($"blog/{blogId}");
    //    if (response.IsSuccessStatusCode)
    //    {
    //        return Result<Unit>.Success(Unit.Value);
    //    }

    //    var errors = await response.Content.ReadFromJsonAsync<List<Error>>();
    //    return Result<Unit>.Failure(errors ?? new List<Error> { new Error("ServerError", "Ошибка микросервиса") });
    //}
}