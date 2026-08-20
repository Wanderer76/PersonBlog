using Blog.Contracts.Models;
using Blog.Contracts.Models.Blog;
using Shared.Utils;
using System.Net.Http.Json;
using System.Text.Json;

namespace Blog.Contracts;
public sealed class BlogApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
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
            { new StringContent(form.Description ?? string.Empty), "Description" }
        };

        if (form.PhotoUrl != null)
        {
            content.Add(new StreamContent(form.PhotoUrl.OpenReadStream()), "PhotoUrl", form.PhotoUrl.FileName);
        }

        var response = await _httpClient.PostAsync("Blog/create", content);
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
        var response = await _httpClient.GetAsync("Blog/subscription-levels");
        return await HandleResponseAsync<List<SubscriptionLevelModel>>(response);
    }

    public async Task<Result<SubscriptionLevelModel>> CreateSubscriptionAsync(SubscriptionCreateDto form)
    {
        var response = await _httpClient.PostAsJsonAsync("Blog/subscription-levels", form);
        return await HandleResponseAsync<SubscriptionLevelModel>(response);
    }

    public async Task<Result<SubscriptionLevelModel>> GetSubscriptionAsync(Guid id)
    {
        var response = await _httpClient.GetAsync($"Blog/subscription-levels/{id}");
        return await HandleResponseAsync<SubscriptionLevelModel>(response);
    }

    public async Task<Result<List<SubscriptionLevelModel>>> GetSubscriptionsByBlogAsync(Guid blogId)
    {
        var response = await _httpClient.GetAsync($"Blog/subscription-levels/blog/{blogId}");
        return await HandleResponseAsync<List<SubscriptionLevelModel>>(response);
    }

    public async Task<Result<SubscriptionLevelModel>> UpdateSubscriptionAsync(SubscriptionUpdateDto form)
    {
        var response = await _httpClient.PutAsJsonAsync($"Blog/subscription-levels/{form.Id}", form);
        return await HandleResponseAsync<SubscriptionLevelModel>(response);
    }

    public async Task<Result> DeleteSubscriptionAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"Blog/subscription-levels/{id}");
        return await HandleCommandResponseAsync(response);
    }

    public async Task<Result<BlogModel>> GetBlogByPostIdAsync(Guid postId)
    {
        var response = await _httpClient.GetAsync($"Blog/blogByPost/{postId}");
        return await HandleResponseAsync<BlogModel>(response);
    }

    public async Task<Result<BlogUserInfoViewModel>> GetBlogViewerInfoByPostIdAsync(Guid postId)
    {
        var response = await _httpClient.GetAsync($"Blog/blogViewerInfoByPost/{postId}");
        return await HandleResponseAsync<BlogUserInfoViewModel>(response);
    }

    public async Task<Result<BlogModel>> GetBlogByIdAsync(Guid blogId)
    {
        var response = await _httpClient.GetAsync($"Blog/blog/{blogId}");
        return await HandleResponseAsync<BlogModel>(response);
    }

    public async Task<Result<BlogModel>> UpdateBlogAsync(Guid blogId, BlogEditRequest form)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(form.Title), "Title" },
            { new StringContent(form.Description ?? string.Empty), "Description" }
        };

        if (form.PhotoUrl != null)
        {
            content.Add(new StreamContent(form.PhotoUrl.OpenReadStream()), "PhotoUrl", form.PhotoUrl.FileName);
        }

        var response = await _httpClient.PutAsync($"Blog/{blogId}", content);
        return await HandleResponseAsync<BlogModel>(response);
    }

    private static async Task<Result<T>> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                try
                {
                    var errors = JsonSerializer.Deserialize<List<Error>>(
                        responseBody,
                        JsonOptions);

                    if (errors is { Count: > 0 })
                    {
                        return Result<T>.Failure(errors);
                    }
                }
                catch (JsonException)
                {
                    // The downstream service may return ProblemDetails or plain text.
                    // Fall through to a status-based error instead of masking it with
                    // a deserialization exception.
                }
            }

            return Result<T>.Failure(new Error(
                response.StatusCode.ToString(),
                $"Сервис блога вернул ошибку {(int)response.StatusCode} ({response.ReasonPhrase})"));
        }

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return Result<T>.Failure(new Error(
                "EmptyResponse",
                $"Сервис блога вернул пустой ответ со статусом {(int)response.StatusCode}"));
        }

        try
        {
            var content = JsonSerializer.Deserialize<T>(responseBody, JsonOptions);
            return content is null
                ? Result<T>.Failure(new Error("InvalidResponse", "Сервис блога вернул пустой JSON"))
                : Result<T>.Success(content);
        }
        catch (JsonException)
        {
            return Result<T>.Failure(new Error(
                "InvalidResponse",
                "Сервис блога вернул ответ в неподдерживаемом формате"));
        }
    }

    private static async Task<Result> HandleCommandResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return Result.Success();

        var responseBody = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            try
            {
                var errors = JsonSerializer.Deserialize<List<Error>>(responseBody, JsonOptions);
                if (errors is { Count: > 0 })
                    return Result.Failure(errors);
            }
            catch (JsonException)
            {
            }
        }

        return Result.Failure(new Error(
            response.StatusCode.ToString(),
            $"Сервис блога вернул ошибку {(int)response.StatusCode} ({response.ReasonPhrase})"));
    }

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
