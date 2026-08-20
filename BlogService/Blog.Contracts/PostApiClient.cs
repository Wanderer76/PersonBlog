using Amazon.Runtime.Internal;
using Authentication.Contract.Constants;
using Blog.Contracts.Models;
using Blog.Contracts.Models.Post;
using Blog.Contracts.Services;
using Blog.Domain.Entities;
using Infrastructure.Middleware;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using Shared.Services;
using Shared.Utils;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;

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

    public async Task<Result<PostDetailViewModel>> GetPostDetailAsync(Guid postId)
    {
        var response = await httpClient.GetAsync($"Post/detail/{postId}");
        return await ToResultAsync<PostDetailViewModel>(response);
    }

    public async Task<HttpStatusCode> CheckVideoAccessAsync(Guid blogId, Guid postId)
    {
        var response = await httpClient.GetAsync($"Post/video-access/{blogId}/{postId}");
        return response.StatusCode;
    }

    public async Task<Result<UserViewInfo>> GetUserViewInfoAsync(Guid postId, Guid? userId, string? address)
    {
        var query = new List<string>();
        if (userId.HasValue)
            query.Add($"userId={userId.Value}");
        if (!string.IsNullOrWhiteSpace(address))
            query.Add($"address={Uri.EscapeDataString(address)}");

        var suffix = query.Count == 0 ? string.Empty : $"?{string.Join('&', query)}";
        var response = await httpClient.GetAsync($"Post/userInfo/{postId}{suffix}");
        return await ToResultAsync<UserViewInfo>(response);
    }

    public async Task<Result> SetReactionAsync(Guid postId, bool? isLike)
    {
        var response = await httpClient.PostAsync($"Post/setReaction/{postId}?isLike={isLike}", null);
        return await ToCommandResultAsync(response);
    }

    public async Task<Result> DeletePostAsync(Guid postId)
    {
        var response = await httpClient.DeleteAsync($"Post/delete/{postId}");
        return await ToCommandResultAsync(response);
    }

    [HttpGet("create")]
    public async Task<CreatePostModelViewModel> GetPostCreateModelAsync()
    {
        var model = await httpClient.GetFromJsonAsync<CreatePostModelViewModel>("ProfilePostV2/create");
        return model!;
    }

    public async Task<PagedListViewModel<UserPostInfoModel>> GetCurrentUserPostsAsync(int page, int pageSize, PostType postType)
    {
        return (await httpClient.GetFromJsonAsync<PagedListViewModel<UserPostInfoModel>>(
            $"ProfilePostV2/my?page={page}&pageSize={pageSize}&postType={(int)postType}"))!;
    }

    public async Task<PagedListViewModel<PostCommonModelV2>> GetAvailablePostsByBlogIdAsync(
        Guid blogId,
        int page,
        int pageSize,
        PostType postType)
    {
        return (await httpClient.GetFromJsonAsync<PagedListViewModel<PostCommonModelV2>>(
            $"ProfilePostV2/availablePostByBlogId/{blogId}?page={page}&pageSize={pageSize}&postType={(int)postType}"))!;
    }

    public async Task<Result<UserPostInfoModel>> CreatePostAsync(VideoPostCreateRequest request)
    {
        using var formData = new MultipartFormDataContent
        {
            { new StringContent(request.Title), "Title" },
            { new StringContent(((int)request.Visibility).ToString()), "Visibility" },
            { new StringContent(request.VideoPostData.Description ?? string.Empty), "VideoPostData.Description" }
        };

        foreach (var category in request.VideoPostData.Categories)
            formData.Add(new StringContent(category.ToString()), "VideoPostData.Categories");

        if (request.VideoPostData.Thumbnail is not null)
            AddFile(formData, request.VideoPostData.Thumbnail, "VideoPostData.Thumbnail");

        var response = await httpClient.PostAsync("ProfilePostV2/create", formData);
        return await ToResultAsync<UserPostInfoModel>(response);
    }

    public async Task<Result> RemovePostAsync(Guid postId)
    {
        var response = await httpClient.PostAsync($"ProfilePostV2/remove/{postId}", null);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(new Error("ProfilePost", await response.Content.ReadAsStringAsync()));
    }

    public async Task<Result<PostEditViewModel>> GetPostEditModelAsync(Guid postId)
    {
        var response = await httpClient.GetAsync($"ProfilePostV2/edit/{postId}");
        return await ToResultAsync<PostEditViewModel>(response);
    }

    public async Task<Result> UpdatePostAsync(PostEditDto request)
    {
        using var formData = new MultipartFormDataContent
        {
            { new StringContent(request.Id.ToString()), "Id" },
            { new StringContent(request.Title), "Title" },
            { new StringContent(request.Description ?? string.Empty), "Description" },
            { new StringContent(((int)request.Visibility).ToString()), "Visibility" }
        };

        foreach (var category in request.Categories ?? [])
            formData.Add(new StringContent(category.ToString()), "Categories");

        if (request.Preview is not null)
            AddFile(formData, request.Preview, "Preview");

        var response = await httpClient.PostAsync("ProfilePostV2/edit", formData);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(new Error("ProfilePost", await response.Content.ReadAsStringAsync()));
    }

    [HttpPost("createTextPost")]
    public async Task<Result<UserPostInfoModel>> CreateTextPostAsync([FromForm] TextPostCreateForm request)
    {
        if (string.IsNullOrWhiteSpace(request.Text) && request.Media == null)
            return Result<UserPostInfoModel>.Failure(new Shared.Utils.Error("no content"));

        using var formData = new MultipartFormDataContent();
        formData.Add(new StringContent(request.Title), "Title");
        formData.Add(new StringContent(request.Text ?? string.Empty), "Text");
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
        return await ToResultAsync<UserPostInfoModel>(response);
    }

    public async Task<Result<TextPostEditViewModel>> GetTextPostEditModelAsync(Guid postId)
    {
        var response = await httpClient.GetAsync($"ProfilePostV2/textEdit/{postId}");
        return await ToResultAsync<TextPostEditViewModel>(response);
    }

    public async Task<Result> UpdateTextPostAsync(TextPostEditDto request)
    {
        using var formData = new MultipartFormDataContent
        {
            { new StringContent(request.Id.ToString()), "Id" },
            { new StringContent(request.Title), "Title" },
            { new StringContent(request.Text ?? string.Empty), "Text" },
            { new StringContent(((int)request.Visibility).ToString()), "Visibility" }
        };

        foreach (var removedMediaId in request.RemovedMediaIds)
            formData.Add(new StringContent(removedMediaId.ToString()), "RemovedMediaIds");

        if (request.Media != null)
        {
            foreach (var file in request.Media)
                AddFile(formData, file, "Media");
        }

        var response = await httpClient.PostAsync("ProfilePostV2/textEdit", formData);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(new Error("TextPost", await response.Content.ReadAsStringAsync()));
    }

    private static void AddFile(MultipartFormDataContent formData, IFormFile file, string name)
    {
        var content = new StreamContent(file.OpenReadStream());
        content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        formData.Add(content, name, file.FileName);
    }

    private static async Task<Result<T>> ToResultAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<T>())!;

        return Result<T>.Failure(new Error(GetErrorKey(response), await response.Content.ReadAsStringAsync()));
    }

    private static async Task<Result> ToCommandResultAsync(HttpResponseMessage response)
    {
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(new Error(GetErrorKey(response), await response.Content.ReadAsStringAsync()));
    }

    private static string GetErrorKey(HttpResponseMessage response) => response.StatusCode switch
    {
        HttpStatusCode.NotFound => "NotFound",
        HttpStatusCode.Forbidden => "Forbidden",
        HttpStatusCode.Unauthorized => "Unauthorized",
        _ => "Post"
    };
}

file record PostCommonCacheKey(int Id) : ICacheKey
{
    public string GetKey() => $"{nameof(PostCommonCacheKey)}:{Id}";
}
