using Blog.Contracts;
using Blog.Contracts.Models;
using Authentication.Contract.Constants;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
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
    private const long MaxThumbnailLength = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedThumbnailContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

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
        if (!Enum.IsDefined(request.ContentType) || !Enum.IsDefined(request.Kind))
        {
            return Result<PlayListListItem>.Failure(new Error(nameof(request.Kind), "Неизвестная классификация плейлиста"));
        }
        var contentType = ToDomain(request.ContentType);
        var kind = ToDomain(request.Kind);
        var permissionResult = ValidateCreatePermission(kind, user);
        if (permissionResult.IsFailure)
        {
            return Result<PlayListListItem>.Failure(permissionResult.Errors!);
        }

        var thumbnailValidationResult = ValidateThumbnail(request.Thumbnail);
        if (thumbnailValidationResult.IsFailure)
        {
            return Result<PlayListListItem>.Failure(thumbnailValidationResult.Errors!);
        }

        var validationResult = await ValidatePostsAsync(
            request.PostIds,
            contentType,
            kind,
            user.UserId,
            nameof(request.PostIds));
        if (validationResult.IsFailure)
        {
            return Result<PlayListListItem>.Failure(validationResult.Errors!);
        }

        using var transaction = await _repository.BeginTransactionAsync();
        var playListId = GuidService.GetNewGuid();
        var thumbnailId = request.ThumbnailId ?? (request.Thumbnail is null ? null : GuidService.GetNewGuid());

        var newPlayList = PlayList.Create(
            id: playListId,
            createdAt: DateTimeService.Now(),
            title: request.Title,
            userId: user.UserId,
            thumbnailId: thumbnailId,
            contentType: contentType,
            kind: kind,
            playListItems: request.PostIds
            );

        if (newPlayList.IsFailure)
        {
            return Result<PlayListListItem>.Failure(newPlayList.Errors!);
        }

        _repository.Add(newPlayList.Value);
        if (request.Thumbnail is not null)
        {
            await _playListFileService.UploadThumbnailAsync(
                playListId,
                user.UserId,
                new BaseFileMetadataEntity
                {
                    Id = thumbnailId!.Value,
                    ContentType = request.Thumbnail.ContentType,
                    CreatedAt = DateTimeService.Now(),
                    FileExtension = Path.GetExtension(request.Thumbnail.FileName),
                    Length = request.Thumbnail.Length,
                    Name = request.Thumbnail.FileName,
                },
                request.Thumbnail.OpenReadStream());
        }
        else if (thumbnailId.HasValue)
        {
            var thumbnailFile = await _repository.Get<PlayListFile>()
                .FirstAsync(x => x.Id == thumbnailId.Value && x.PlaylistId == null);
            _repository.Attach(thumbnailFile);
            thumbnailFile.PlaylistId = playListId;
        }
        await _repository.SaveChangesAsync();
        await transaction.CommitAsync();
        return new PlayListListItem
        {
            Id = newPlayList.Value.Id,
            PostCount = newPlayList.Value.PlayListItems.Count,
            Title = request.Title,
            ContentType = ToModel(newPlayList.Value.ContentType),
            Kind = ToModel(newPlayList.Value.Kind),
            ThumbnailUrl = await _playListFileService.GetThumbnailAsync(newPlayList.Value)
        };
    }

    public async Task<Result<PlayListListItem>> GetPlayListAsync(Guid id)
    {
        var playlist = await _repository.Get<PlayList>()
            .Include(x => x.PlayListItems)
            .Where(x => x.Id == id && x.IsDelete == false)
            .FirstOrDefaultAsync();

        if (playlist == null)
        {
            return new Error(nameof(id), "Not found");
        }
        var user = await _currentUserService.GetCurrentUserAsync();

        return Result<PlayListListItem>.Success(new PlayListListItem
        {
            Id = playlist.Id,
            PostCount = playlist.PlayListItems.Count,
            ThumbnailUrl = await _playListFileService.GetThumbnailAsync(playlist),
            Title = playlist.Title,
            CanEdit = playlist.UserId == user.UserId,
            ContentType = ToModel(playlist.ContentType),
            Kind = ToModel(playlist.Kind),
        });
    }

    public async Task<Result<PlayListListItem>> AddVideoAsync(PlayListItemAddRequest playListItems)
    {
        var user = await _currentUserService.GetCurrentUserAsync();

        var playlist = await _repository.Get<PlayList>()
            .Where(x => x.Id == playListItems.PlayListId && x.IsDelete == false)
            .Include(x => x.PlayListItems)
            .FirstOrDefaultAsync();

        if (playlist == null)
        {
            return Result<PlayListListItem>.Failure(
                new Error(nameof(playListItems.PlayListId), "Не найден плейлист"));
        }

        if (user.UserId != playlist.UserId)
        {
            return new Error("Нельзя добавить пост не в свой плейлист");
        }
        var permissionResult = ValidateCreatePermission(playlist.Kind, user);
        if (permissionResult.IsFailure)
        {
            return Result<PlayListListItem>.Failure(permissionResult.Errors!);
        }

        var validationResult = await ValidatePostsAsync(
            playListItems.PostsToAdd,
            playlist.ContentType,
            playlist.Kind,
            user.UserId,
            nameof(playListItems.PostsToAdd));
        if (validationResult.IsFailure)
        {
            return Result<PlayListListItem>.Failure(validationResult.Errors!);
        }

        var now = DateTimeService.Now();
        _repository.Attach(playlist);
        foreach (var playListItem in playListItems.PostsToAdd)
        {
            var isAdded = playlist.AddPost(playListItem, playlist.ContentType, now);
            if (isAdded.IsFailure)
            {
                return Result<PlayListListItem>.Failure(isAdded.Errors!);
            }
        }

        await _repository.SaveChangesAsync();
        return new PlayListListItem
        {
            Id = playlist.Id,
            PostCount = playlist.PlayListItems.Count,
            ThumbnailUrl = await _playListFileService.GetThumbnailAsync(playlist),
            Title = playlist.Title,
            ContentType = ToModel(playlist.ContentType),
            Kind = ToModel(playlist.Kind),
        };
    }

    public async Task<Result<PlayListListItem>> RemoveVideoAsync(PlayListItemRemoveRequest request)
    {
        var playlist = await _repository.Get<PlayList>()
            .Where(x => x.IsDelete == false)
            .Where(x => x.Id == request.PlayListId)
            .Include(x => x.PlayListItems)
            .FirstOrDefaultAsync();

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
        var removeResult = playlist.RemoveVideo(request.PostId);
        if (removeResult.IsFailure)
        {
            return Result<PlayListListItem>.Failure(removeResult.Errors!);
        }
        await _repository.SaveChangesAsync();
        return Result<PlayListListItem>.Success(new PlayListListItem
        {
            Id = playlist.Id,
            PostCount = playlist.PlayListItems.Count,
            ThumbnailUrl = await _playListFileService.GetThumbnailAsync(playlist),
            Title = playlist.Title,
            ContentType = ToModel(playlist.ContentType),
            Kind = ToModel(playlist.Kind),
        });
    }

    public async Task<Result<PlayListListItem>> ChangePostPositionAsync(ChangePostPositionRequest changePostPositionRequest)
    {
        var playlist = await _repository.Get<PlayList>()
            .Where(x => x.Id == changePostPositionRequest.PlaylistId && x.IsDelete == false)
            .Include(x => x.PlayListItems.OrderBy(x => x.Position))
            .FirstOrDefaultAsync();
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
            ContentType = ToModel(playlist.ContentType),
            Kind = ToModel(playlist.Kind),
        });
    }

    private async Task<Result> ValidatePostsAsync(
        IEnumerable<Guid> requestedPostIds,
        PlayListContentType contentType,
        PlayListKind kind,
        Guid ownerUserId,
        string errorKey)
    {
        var requestedPostIdArray = requestedPostIds.ToArray();
        var requestedIds = requestedPostIdArray.Distinct().ToArray();
        if (requestedIds.Length == 0)
        {
            return Result.Success();
        }
        if (requestedIds.Length != requestedPostIdArray.Length)
        {
            return Result.Failure(new Error(errorKey, "Плейлист не может содержать повторяющиеся посты"));
        }

        var posts = await postApiClient.GetPostCommonModelAsync(requestedIds);
        if (posts.Count != requestedIds.Length)
        {
            return Result.Failure(new Error(
                errorKey,
                "Один или несколько постов не найдены, удалены или не опубликованы"));
        }

        var expectedPostType = contentType switch
        {
            PlayListContentType.Video => PostTypeModel.Video,
            PlayListContentType.Text => PostTypeModel.Text,
            _ => throw new ArgumentOutOfRangeException(nameof(contentType), contentType, null)
        };
        if (posts.Any(x => x.PostType != expectedPostType))
        {
            return Result.Failure(new Error(errorKey, "Все посты плейлиста должны иметь один выбранный тип"));
        }
        if (kind == PlayListKind.Authored && posts.Any(x => x.Creator.UserId != ownerUserId))
        {
            return Result.Failure(new Error(errorKey, "Авторский плейлист может содержать только собственные посты"));
        }

        return Result.Success();
    }

    private static Result ValidateThumbnail(IFormFile? thumbnail)
    {
        if (thumbnail is null)
        {
            return Result.Success();
        }
        if (thumbnail.Length <= 0)
        {
            return Result.Failure(new Error(nameof(CreatePlayListRequest.Thumbnail), "Файл обложки пуст"));
        }
        if (thumbnail.Length > MaxThumbnailLength)
        {
            return Result.Failure(new Error(nameof(CreatePlayListRequest.Thumbnail), "Размер обложки не должен превышать 10 МБ"));
        }
        if (!AllowedThumbnailContentTypes.Contains(thumbnail.ContentType))
        {
            return Result.Failure(new Error(nameof(CreatePlayListRequest.Thumbnail), "Поддерживаются изображения JPG, PNG и WebP"));
        }
        return Result.Success();
    }

    private static Result ValidateCreatePermission(PlayListKind kind, UserModel user)
    {
        var roles = user.Roles.ToHashSet();
        return kind switch
        {
            PlayListKind.Authored when roles.Contains(Roles.BloggerRoleId) => Result.Success(),
            PlayListKind.Collection when roles.Contains(Roles.UserRoleId) || roles.Contains(Roles.BloggerRoleId) => Result.Success(),
            PlayListKind.Authored => Result.Failure(new Error(nameof(kind), "Авторские плейлисты доступны только блогерам")),
            PlayListKind.Collection => Result.Failure(new Error(nameof(kind), "Недостаточно прав для создания коллекции")),
            _ => Result.Failure(new Error(nameof(kind), "Неизвестный тип плейлиста"))
        };
    }

    private static PlayListContentType ToDomain(PlayListContentTypeModel contentType) => contentType switch
    {
        PlayListContentTypeModel.Video => PlayListContentType.Video,
        PlayListContentTypeModel.Text => PlayListContentType.Text,
        _ => throw new ArgumentOutOfRangeException(nameof(contentType), contentType, null)
    };

    private static PlayListKind ToDomain(PlayListKindModel kind) => kind switch
    {
        PlayListKindModel.Authored => PlayListKind.Authored,
        PlayListKindModel.Collection => PlayListKind.Collection,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private static PlayListContentTypeModel ToModel(PlayListContentType contentType) => contentType switch
    {
        PlayListContentType.Video => PlayListContentTypeModel.Video,
        PlayListContentType.Text => PlayListContentTypeModel.Text,
        _ => throw new ArgumentOutOfRangeException(nameof(contentType), contentType, null)
    };

    private static PlayListKindModel ToModel(PlayListKind kind) => kind switch
    {
        PlayListKind.Authored => PlayListKindModel.Authored,
        PlayListKind.Collection => PlayListKindModel.Collection,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public async Task<Result> RemovePlayListAsync(Guid id)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var playList = await _repository.Get<PlayList>()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsDelete == false);
        if (playList == null)
        {
            return Result.Failure(new Error(nameof(id), "Не найден плейлист"));
        }
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

        var unorderedItems = posts.Count == 0
            ? []
            : await postApiClient.GetPostCommonModelAsync(posts);
        var itemsById = unorderedItems.ToDictionary(x => x.Id);
        var items = posts
            .Where(itemsById.ContainsKey)
            .Select(postId => itemsById[postId])
            .ToArray();
        return PagedListViewModel.Create(items, pageSize, totalPostCount);
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
                x.ContentType,
                x.Kind,
            })
            .ToListAsync();

        var result = playLists.Select(async x => new PlayListListItem
        {
            Id = x.Id,
            PostCount = x.PlayListItemsCount,
            Title = x.Title,
            ContentType = ToModel(x.ContentType),
            Kind = ToModel(x.Kind),
            ThumbnailUrl = x.ThumbnailId == null ? null : await _playListFileService.GetThumbnailAsync(user.UserId, x.ThumbnailId.Value)
        });

        return await Task.WhenAll(result);
    }

    public async Task<Result<IReadOnlyList<PlayListListItem>>> GetPlayListsByBlogIdAsync(Guid blogId)
    {
        var blog = await blogApiClient.GetBlogDetailsAsync(blogId);
        if (blog.IsFailure)
        {
            return Result<IReadOnlyList<PlayListListItem>>.Failure(blog.Errors);
        }

        var playLists = await _repository.Get<PlayList>()
            .Where(x => x.UserId == blog.Value.UserId)
            .Where(x => x.IsDelete == false)
            .Where(x => x.Kind == PlayListKind.Authored)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Title,
                PlayListItemsCount = x.PlayListItems.Count,
                x.ThumbnailId,
                x.ContentType,
                x.Kind,
            })
            .ToListAsync();

        var result = playLists.Select(async x => new PlayListListItem
        {
            Id = x.Id,
            PostCount = x.PlayListItemsCount,
            Title = x.Title,
            ContentType = ToModel(x.ContentType),
            Kind = ToModel(x.Kind),
            ThumbnailUrl = x.ThumbnailId == null ? null : await _playListFileService.GetThumbnailAsync(blog.Value.UserId, x.ThumbnailId.Value)
        });
        return await Task.WhenAll(result);
    }
}

public class PlayListItemAddRequest
{
    public required Guid PlayListId { get; set; }

    public required List<Guid> PostsToAdd { get; set; }
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

