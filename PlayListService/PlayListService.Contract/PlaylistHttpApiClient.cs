using Blog.Contracts.Models;
using Infrastructure.Extensions;
using Infrastructure.Services;
using PlayListService.Services.Models;
using PlayListService.Services.Services;
using Shared.Models;
using Shared.Utils;
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
        var result = await _client.PostAsJsonAsync("addVideo", playListItems);
        if (result.IsSuccessStatusCode)
            return (await result.Content.ReadFromJsonAsync<PlayListListItem>())!;

        var errors = (await result.Content.ReadFromJsonAsync<ApiException>())!;

        return Result<PlayListListItem>.Failure(errors.FromApiException());
    }

    public async Task<Result<PlayListListItem>> ChangePostPositionAsync(ChangePostPositionRequest changePostPositionRequest)
    {
        var result = await _client.PostAsJsonAsync("updatePositions", changePostPositionRequest);
        if (result.IsSuccessStatusCode)
            return (await result.Content.ReadFromJsonAsync<PlayListListItem>())!;

        var errors = (await result.Content.ReadFromJsonAsync<ApiException>())!;

        return Result<PlayListListItem>.Failure(errors.FromApiException());
    }

    public async Task<Result<PlayListListItem>> CreatePlayListAsync(CreatePlayListRequest request)
    {
        var result = await _client.PostAsJsonAsync("create", request);
        if (result.IsSuccessStatusCode)
            return (await result.Content.ReadFromJsonAsync<PlayListListItem>())!;

        var errors = (await result.Content.ReadFromJsonAsync<ApiException>())!;

        return Result<PlayListListItem>.Failure(errors.FromApiException());
    }

    public async Task<Result<PlayListListItem>> GetPlayListAsync(Guid id)
    {
        var result = await _client.GetAsync($"item/{id}");
        if (result.IsSuccessStatusCode)
            return (await result.Content.ReadFromJsonAsync<PlayListListItem>())!;

        var errors = (await result.Content.ReadFromJsonAsync<ApiException>())!;
        return Result<PlayListListItem>.Failure(errors.FromApiException());
    }

    public async Task<PagedListViewModel<PostCommonModel>> GetPlayListPostPagedAsync(Guid playListId, int page, int pageSize)
    {
        var result = await _client.GetAsync($"item/{playListId}/postList?{nameof(page)}={page}&{nameof(pageSize)}={pageSize}");
        return (await result.Content.ReadFromJsonAsync<PagedListViewModel<PostCommonModel>>())!;
    }

    public async Task<Result<IReadOnlyList<PlayListListItem>>> GetPlayListsByBlogId(Guid blogId)
    {
        var result = await _client.GetAsync("list");
        if (result.IsSuccessStatusCode)
            return (await result.Content.ReadFromJsonAsync<List<PlayListListItem>>())!;

        var errors = (await result.Content.ReadFromJsonAsync<ApiException>())!;
        return Result<IReadOnlyList<PlayListListItem>>.Failure(errors.FromApiException());
    }

    public Task<IReadOnlyList<PlayListListItem>> GetUserPlayLists()
    {
        throw new NotImplementedException();
    }

    public async Task<Result> RemovePlayListAsync(Guid id)
    {
        var result = await _client.PostAsync($"removePlaylist/{id}",null);
        if (result.IsSuccessStatusCode)
            return Result.Success();

        var errors = (await result.Content.ReadFromJsonAsync<ApiException>())!;
        return Result.Failure(errors.FromApiException());
    }

    public async Task<Result<PlayListListItem>> RemoveVideoAsync(PlayListItemRemoveRequest request)
    {
        var result = await _client.PostAsJsonAsync($"removeVideo", request);
        if (result.IsSuccessStatusCode)
            return (await result.Content.ReadFromJsonAsync<PlayListListItem>())!;

        var errors = (await result.Content.ReadFromJsonAsync<ApiException>())!;
        return Result<PlayListListItem>.Failure(errors.FromApiException());
    }
}
