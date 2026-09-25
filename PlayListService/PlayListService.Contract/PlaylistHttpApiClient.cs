using Blog.Contracts.Models;
using Infrastructure.Services;
using PlayListService.Services.Models;
using PlayListService.Services.Services;
using Shared.Models;
using Shared.Utils;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PlayListService.Contract;

public class PlaylistHttpApiClient : IPlayListService
{
    private readonly HttpClient _client;
    private readonly ICacheService _cacheService;

    public PlaylistHttpApiClient(HttpClient client, ICacheService cacheService)
    {
        _client = client;
        _cacheService = cacheService;
    }

    public async Task<Result<PlayListListItem>> AddVideoAsync(PlayListItemAddRequest playListItems)
    {
        var result = await _client.PostAsJsonAsync("PlayList/addVideo", playListItems);
        if (result.IsSuccessStatusCode)
            return (await result.Content.ReadFromJsonAsync<DataEnvelope<PlayListListItem>>())!.Data;

        var errors = (await result.Content.ReadFromJsonAsync<List<Error>>())!;

        return Result<PlayListListItem>.Failure(errors);
    }

    public async Task<Result<PlayListListItem>> ChangePostPositionAsync(ChangePostPositionRequest changePostPositionRequest)
    {
        var result = await _client.PostAsJsonAsync("PlayList/updatePositions", changePostPositionRequest);
        if (result.IsSuccessStatusCode)
            return (await result.Content.ReadFromJsonAsync<DataEnvelope<PlayListListItem>>())!.Data;

        var errors = (await result.Content.ReadFromJsonAsync<List<Error>>())!;

        return Result<PlayListListItem>.Failure(errors);
    }

    public async Task<Result<PlayListListItem>> CreatePlayListAsync(CreatePlayListRequest request)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(request.Title), nameof(request.Title) },
            { new StringContent(((int)request.ContentType).ToString()), nameof(request.ContentType) },
            { new StringContent(((int)request.Kind).ToString()), nameof(request.Kind) }
        };

        if (request.ThumbnailId.HasValue)
        {
            content.Add(
                new StringContent(request.ThumbnailId.Value.ToString()),
                nameof(request.ThumbnailId));
        }

        foreach (var postId in request.PostIds)
        {
            content.Add(new StringContent(postId.ToString()), nameof(request.PostIds));
        }

        if (request.Thumbnail is not null)
        {
            var thumbnailContent = new StreamContent(request.Thumbnail.OpenReadStream());
            thumbnailContent.Headers.ContentType = new MediaTypeHeaderValue(request.Thumbnail.ContentType);
            content.Add(thumbnailContent, nameof(request.Thumbnail), request.Thumbnail.FileName);
        }

        var result = await _client.PostAsync("PlayList/create", content);
        if (result.IsSuccessStatusCode)
            return (await result.Content.ReadFromJsonAsync<PlayListListItem>())!;

        var errors = (await result.Content.ReadFromJsonAsync<List<Error>>())!;

        return Result<PlayListListItem>.Failure(errors);
    }

    public async Task<Result<PlayListListItem>> GetPlayListAsync(Guid id)
    {
        var result = await _client.GetAsync($"PlayList/item/{id}");
        if (result.IsSuccessStatusCode)
            return (await result.Content.ReadFromJsonAsync<PlayListListItem>())!;

        var errors = (await result.Content.ReadFromJsonAsync<List<Error>>())!;
        return Result<PlayListListItem>.Failure(errors);
    }

    public async Task<PagedListViewModel<PostCommonModel>> GetPlayListPostPagedAsync(Guid playListId, int page, int pageSize)
    {
        var result = await _client.GetAsync($"PlayList/item/{playListId}/postList?{nameof(page)}={page}&{nameof(pageSize)}={pageSize}");
        return (await result.Content.ReadFromJsonAsync<PagedListViewModel<PostCommonModel>>())!;
    }

    public async Task<Result<IReadOnlyList<PlayListListItem>>> GetPlayListsByBlogIdAsync(Guid blogId)
    {
        var result = await _client.GetAsync($"PlayList/list?blogId={blogId}");
        if (result.IsSuccessStatusCode)
            return (await result.Content.ReadFromJsonAsync<List<PlayListListItem>>())!;
        var errors = (await result.Content.ReadFromJsonAsync<List<Error>>())!;
        return Result<IReadOnlyList<PlayListListItem>>.Failure(errors);
    }

    public async Task<IReadOnlyList<PlayListListItem>> GetUserPlayLists()
    {
        var result = await _client.GetAsync($"PlayList/my/list");
        if (result.IsSuccessStatusCode)
            return (await result.Content.ReadFromJsonAsync<List<PlayListListItem>>())!;
        return [];
    }

    public async Task<Result> RemovePlayListAsync(Guid id)
    {
        var result = await _client.PostAsync($"PlayList/removePlaylist/{id}",null);
        if (result.IsSuccessStatusCode)
            return Result.Success();

        var errors = (await result.Content.ReadFromJsonAsync<List<Error>>())!;
        return Result.Failure(errors);
    }

    public async Task<Result<PlayListListItem>> RemoveVideoAsync(PlayListItemRemoveRequest request)
    {
        var result = await _client.PostAsJsonAsync($"PlayList/removeVideo", request);
        if (result.IsSuccessStatusCode)
            return (await result.Content.ReadFromJsonAsync<PlayListListItem>())!;

        var errors = (await result.Content.ReadFromJsonAsync<List<Error>>())!;
        return Result<PlayListListItem>.Failure(errors);
    }

    private sealed record DataEnvelope<T>(T Data);
}
