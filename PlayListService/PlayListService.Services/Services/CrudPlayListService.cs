using Blog.Contracts;
using Blog.Contracts.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using PlayListService.Domain.Entities;
using PlayListService.Services.Models;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace PlayListService.Services.Services;

internal sealed class CrudPlayListService : IPlayListService
{
    private readonly IReadWriteRepository<IPlayListEntity> _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly PlayListFileService _playListFileService;
    private readonly PostApiClient postApiClient;
    private readonly BlogApiClient blogApiClient;

    public CrudPlayListService(IReadWriteRepository<IPlayListEntity> repository, ICurrentUserService currentUserService, PlayListFileService playListFileService, PostApiClient postApiClient, BlogApiClient blogApiClient)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _playListFileService = playListFileService;
        this.postApiClient = postApiClient;
        this.blogApiClient = blogApiClient;
    }

    public async Task<Result<PlayListListItem>> CreatePlayListAsync(CreatePlayListRequest request)
    {
        var user = await _currentUserService.GetCurrentUserAsync();

        var playListId = GuidService.GetNewGuid();
        var thumbnailUrl = request.Thumbnail == null ? null : await _playListFileService.UploadThumbnailAsync(playListId, user.UserId, request.Thumbnail.OpenReadStream());

        var newPlayList = PlayList.Create(
            id: playListId,
            createdAt: DateTimeService.Now(),
            title: request.Title,
            userId: user.UserId,
            thumbnailId: thumbnailUrl,
            playListItems: request.PostIds
            );

        if (newPlayList.IsFailure)
        {
            return Result<PlayListListItem>.Failure(newPlayList.Errors!);
        }

        _repository.Add(newPlayList.Value);
        await _repository.SaveChangesAsync();

        return new PlayListListItem
        {
            Id = newPlayList.Value.Id,
            PostCount = newPlayList.Value.PlayListItems.Count,
            Title = request.Title,
            ThumbnailUrl = await _playListFileService.GetThumbnailAsync(newPlayList.Value)
        };
    }

    public async Task<Result<PlayListListItem>> GetPlayListAsync(Guid id)
    {
        var playlist = await _repository.Get<PlayList>()
         .Where(x => x.Id == id && x.IsDelete == false)
         .FirstOrDefaultAsync();

        if (playlist == null)
        {
            return new Error(nameof(id), "Not found");
        }

        return Result<PlayListListItem>.Success(new PlayListListItem
        {
            Id = playlist.Id,
            PostCount = playlist.PlayListItems.Count,
            ThumbnailUrl = await _playListFileService.GetThumbnailAsync(playlist),
            Title = playlist.Title,
        });
    }

    public async Task<Result<PlayListListItem>> AddVideoAsync(PlayListItemAddRequest playListItems)
    {
        var user = await _currentUserService.GetCurrentUserAsync();

        var playlist = await _repository.Get<PlayList>()
            .Where(x => x.Id == playListItems.PlayListId && x.IsDelete == false)
            .Include(x => x.PlayListItems)
            .FirstAsync();


        if (user.UserId != playlist.UserId)
        {
            return new Error("");
        }

        var now = DateTimeService.Now();
        _repository.Attach(playlist);
        foreach (var playListItem in playListItems.Items)
        {
            var isAdded = playlist.AddVideo(playListItem.PostId, now, playListItem.Position);
            if (isAdded.IsFailure)
            {
                return Result<PlayListListItem>.Failure(isAdded.Errors!);
            }
        }

        await _repository.SaveChangesAsync();
        return Result<PlayListListItem>.Success(new PlayListListItem
        {
            Id = playlist.Id,
            PostCount = playlist.PlayListItems.Count,
            ThumbnailUrl = await _playListFileService.GetThumbnailAsync(playlist),
            Title = playlist.Title,
        });
    }

    public async Task<Result<PlayListListItem>> RemoveVideoAsync(PlayListItemRemoveRequest request)
    {
        var playlist = await _repository.Get<PlayList>()
            .Where(x => x.IsDelete == false)
            .Where(x => x.Id == request.PlayListId)
            .Include(x => x.PlayListItems)
            .FirstAsync();

        if (playlist == null)
        {
            return Result<PlayListListItem>.Failure(new Error(nameof(request.PlayListId), "Не найден плейлист"));
        }

        var user = await _currentUserService.GetCurrentUserAsync();

        if (user.UserId != playlist.UserId)
        {
            return Result<PlayListListItem>.Failure(new Error(""));
        }
        _repository.Attach(playlist);
        playlist.RemoveVideo(request.PostId);
        await _repository.SaveChangesAsync();
        return Result<PlayListListItem>.Success(new PlayListListItem
        {
            Id = playlist.Id,
            PostCount = playlist.PlayListItems.Count,
            ThumbnailUrl = await _playListFileService.GetThumbnailAsync(playlist),
            Title = playlist.Title,
        });
    }

    public async Task<Result<PlayListListItem>> ChangePostPositionAsync(ChangePostPositionRequest changePostPositionRequest)
    {
        var playlist = await _repository.Get<PlayList>()
            .Where(x => x.Id == changePostPositionRequest.PlaylistId)
            .Include(x => x.PlayListItems.OrderBy(x => x.Position))
            .FirstAsync();
        if (playlist == null)
        {
            return Result<PlayListListItem>.Failure(new Error(nameof(changePostPositionRequest.PlaylistId), "Не найден плейлист"));
        }

        var user = await _currentUserService.GetCurrentUserAsync();

        if (user.UserId != playlist.UserId)
        {
            return Result<PlayListListItem>.Failure(new Error(""));
        }

        _repository.Attach(playlist);

        var result = playlist.ChangeVideoPosition(changePostPositionRequest.PostId, changePostPositionRequest.Destination);
        if (result.IsFailure)
        {
            return Result<PlayListListItem>.Failure(result.Errors!);
        }
        await _repository.SaveChangesAsync();


        return Result<PlayListListItem>.Success(new PlayListListItem
        {
            Id = playlist.Id,
            PostCount = playlist.PlayListItems.Count,
            ThumbnailUrl = await _playListFileService.GetThumbnailAsync(playlist),
            Title = playlist.Title,
        });
    }

    public async Task<Result> RemovePlayListAsync(Guid id)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var playList = await _repository.Get<PlayList>()
            .FirstAsync(x => x.Id == id);
        if (playList.UserId != user.UserId)
        {
            return Result.Failure(new Error("", "Нельзя удалить чужой плейлист"));
        }
        _repository.Attach(playList);
        playList.RemovePlayList();
        await _repository.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<PagedListViewModel<PostCommonModel>> GetPlayListPostPagedAsync(Guid playListId, int page, int pageSize)
    {
        var query = _repository.Get<PlayList>()
            .Where(x => x.Id == playListId)
            .SelectMany(x => x.PlayListItems)
            .Where(x => x.IsDelete == false);

        var totalPostCount = await query.CountAsync();

        var posts = await query
            .OrderBy(x => x.Position)
            .Select(x => x.PostId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = posts.Count == 0 ? [] : await postApiClient.GetPostCommonModelAsync(posts);
        return new PagedListViewModel<PostCommonModel>(totalPostCount, pageSize, items);
    }

    public async Task<IReadOnlyList<PlayListListItem>> GetUserPlayLists()
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var playLists = await _repository.Get<PlayList>()
            .Where(x => x.UserId == user.UserId)
            .Where(x => x.IsDelete == false)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Title,
                PlayListItemsCount = x.PlayListItems.Count(),
                x.ThumbnailId,
            })
            .ToListAsync();

        var result = playLists.Select(async x => new PlayListListItem
        {
            Id = x.Id,
            PostCount = x.PlayListItemsCount,
            Title = x.Title,
            ThumbnailUrl = x.ThumbnailId == null ? null : await _playListFileService.GetThumbnailAsync(user.UserId, x.ThumbnailId)
        });

        return await Task.WhenAll(result);
    }

    public async Task<Result<IReadOnlyList<PlayListListItem>>> GetPlayListsByBlogId(Guid blogId)
    {
        var blog = await blogApiClient.GetBlogDetailsAsync(blogId);
        if (blog.IsFailure)
        {
            return Result<IReadOnlyList<PlayListListItem>>.Failure(blog.Errors);
        }
        var playLists = await _repository.Get<PlayList>()
            .Where(x => x.UserId == blog.Value.UserId)
            .Where(x => x.IsDelete == false)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Title,
                PlayListItemsCount = x.PlayListItems.Count,
                x.ThumbnailId,
            })
            .ToListAsync();

        var result = playLists.Select(async x => new PlayListListItem
        {
            Id = x.Id,
            PostCount = x.PlayListItemsCount,
            Title = x.Title,
            ThumbnailUrl = x.ThumbnailId == null ? null : await _playListFileService.GetThumbnailAsync(blog.Value.UserId, x.ThumbnailId)
        });

        return await Task.WhenAll(result);
    }
}

public class PlayListItemAddRequest
{
    public required Guid PlayListId { get; set; }

    public required List<PlayListAddItem> Items { get; set; }
}

public class PlayListAddItem
{
    public required Guid PostId { get; set; }
    public int? Position { get; set; }
}

public class ChangePostPositionRequest
{
    public required Guid PlaylistId { get; set; }
    public required Guid PostId { get; set; }
    public required int Destination { get; set; }
}

public class PlayListItemRemoveRequest
{
    public required Guid PlayListId { get; set; }
    public required Guid PostId { get; set; }
}

