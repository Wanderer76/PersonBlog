using Authentication.Contract.Constants;
using Blog.Contracts.Events;
using Blog.Contracts.Models;
using Blog.Contracts.Models.Post;
using Blog.Contracts.Models.TextPost;
using Blog.Contracts.Services;
using Blog.Domain.Entities;
using Blog.Service.Events;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;
using System.Net;
using System.Text.RegularExpressions;

namespace Blog.Service.Services.Implementation;

internal class DefaultPostService : IPostService
{
    private const int WordsPerMinute = 200;
    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private readonly IReadWriteRepository<IBlogEntity> _context;
    private readonly IFileStorageFactory _fileStorageFactory;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUserService _userService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DefaultPostService(
        IReadWriteRepository<IBlogEntity> context,
        IFileStorageFactory fileStorageFactory,
        ICacheService cacheService,
        ICurrentUserService userService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _fileStorageFactory = fileStorageFactory;
        _cacheService = cacheService;
        _userService = userService;
        _httpContextAccessor = httpContextAccessor;
    }

    [Obsolete]
    public async Task<Guid> GetVideoChunkStreamByPostIdAsync(Guid postId, Guid fileMetadataId, long offset, long length, Stream output)
    {
        var videoData = await _context.Get<VideoFile>()
            .Where(x => x.Id == fileMetadataId)
            .FirstAsync();

        using var storage = _fileStorageFactory.CreateFileStorage();
        await storage.ReadFileByChunksAsync(postId, videoData.ObjectName, offset, length, output);
        return fileMetadataId;
    }

    public async Task<Result> RemovePostByIdAsync(Guid id)
    {
        var currentUser = await _userService.GetCurrentUserAsync();
        var post = await _context.Get<Post>()
            .Where(x => x.Id == id)
            .Include(x => x.VideoPostInfo)
            .FirstOrDefaultAsync();

        if (post == null)
        {
            return Result.Failure(new Error("NotFound", "Пост не найден"));
        }

        if (post.BlogId != currentUser.BlogId)
        {
            return Result.Failure(new Error("Forbidden", "Пост не принадлежит текущему пользователю"));
        }

        _context.Attach(post);
        post.Delete();
        _context.Add(new PostRemoveEvent(post.Id, DateTimeService.Now()));
        _context.Add(VideoProcessEvent.Create(new PostUpdateEvent
        {
            BlogId = post.BlogId,
            CreatedAt = DateTimeService.Now(),
            UpdateType = UpdateType.Delete,
            Description = post.VideoPostInfo.Description,
            PostId = post.Id,
            Title = post.Title,
            ViewCount = post.ViewCount
        }));
        _context.Add(VideoProcessEvent.Create(PostCatalogChangedV2Factory.Create(post)));

        await _cacheService.RemoveCachedDataAsync(new PostModelCacheKey(id));
        await _context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<bool> CanAccessVideoAsync(Guid blogId, Guid postId)
    {
        var post = await _context.Get<Post>()
            .Where(x => x.Id == postId && x.BlogId == blogId)
            .Select(x => new
            {
                x.IsDelete,
                x.Type,
                x.ProcessState,
                x.Visibility,
                x.BanMessageId,
                OwnerUserId = x.Blog.UserId
            })
            .FirstOrDefaultAsync();

        if (post == null || post.IsDelete || post.Type != PostType.Video || post.ProcessState != ProcessState.Complete)
        {
            return false;
        }

        if (post.Visibility != PostVisibility.Private && !post.BanMessageId.HasValue)
        {
            return true;
        }

        var currentUser = await _userService.GetCurrentUserAsync();
        var isOwner = !currentUser.IsAnonymous && currentUser.UserId == post.OwnerUserId;
        var isModerator = currentUser.Roles.Intersect([Roles.SuperAdminRoleId, Roles.AdminRoleId]).Any();

        return (post.Visibility != PostVisibility.Private || isOwner)
            && (!post.BanMessageId.HasValue || isOwner || isModerator);
    }

    public async Task<PostDetailViewModel?> GetDetailPostByIdAsync(Guid postId)
    {
        var accessInfo = await _context.Get<Post>()
            .Where(x => x.Id == postId)
            .Select(x => new
            {
                x.BanMessageId,
                x.IsDelete,
                x.Visibility,
                OwnerUserId = x.Blog.UserId
            })
            .FirstOrDefaultAsync();

        if (accessInfo == null || accessInfo.IsDelete)
        {
            return null;
        }

        var currentUser = await _userService.GetCurrentUserAsync();
        var isOwner = !currentUser.IsAnonymous && currentUser.UserId == accessInfo.OwnerUserId;
        var isModerator = currentUser.Roles.Intersect([Roles.SuperAdminRoleId, Roles.AdminRoleId]).Any();

        if (accessInfo.Visibility == PostVisibility.Private && !isOwner)
        {
            return null;
        }

        if (accessInfo.BanMessageId.HasValue && !isOwner && !isModerator)
        {
            return null;
        }

        var cacheData = await _cacheService.GetOrAddDataAsync(new PostDetailViewModelCacheKey(postId), async () =>
        {
            var post = await _context.Get<Post>()
            .Include(x => x.VideoPostInfo)
            .ThenInclude(x => x.VideoFile)
            .Include(x => x.VideoPostInfo)
            .ThenInclude(x => x.PreviewFile)
            .Include(x => x.Blog)
            .FirstAsync(x => x.Id == postId);

            using var fileStorage = _fileStorageFactory.CreateFileStorage();

            var previewUrl = !post.VideoPostInfo.PreviewId.HasValue
                ? null
                : await fileStorage.GetFileUrlAsync(post.BlogId, post.VideoPostInfo.PreviewFile!.ObjectName);

            var videoMetadata = post.VideoPostInfo.VideoFile;
            var processState = post.VideoPostInfo.VideoFile != null ? post.ProcessState : ProcessState.Load;

            return new PostDetailViewModel(
                post.Id,
                previewUrl,
                post.CreatedAt,
                post.ViewCount,
                post.VideoPostInfo.Description,
                post.Title,
                post.Type,
                post.LikeCount,
                post.DislikeCount,
                videoMetadata != null && processState == ProcessState.Complete ?
                            new VideoMetadataModel(
                                videoMetadata.Id,
                                videoMetadata.Length,
                                videoMetadata.Duration,
                                videoMetadata.ContentType,
                                videoMetadata.ObjectName
                            ) : null,
                processState
            );
        });
        return cacheData;
    }

    public async Task<Result<TextPostDetailResponse>> GetTextPostDetailAsync(Guid postId)
    {
        var currentUser = await _userService.GetCurrentUserAsync();
        var post = await _context.Get<Post>()
            .Include(x => x.TextPostInfo)
            .ThenInclude(x => x.Files)
            .Include(x => x.Blog)
            .FirstOrDefaultAsync(x => x.Id == postId);

        if (post == null ||
            post.IsDelete ||
            post.Type != PostType.Text ||
            post.ProcessState != ProcessState.Complete ||
            post.TextPostInfo == null)
        {
            return new Error("NotFound", "Текстовый пост не найден");
        }

        var accessInfo = new PostAccessInfo(
            post.IsDelete,
            post.Visibility,
            post.BanMessageId,
            post.Blog.UserId);
        if (!CanAccessPost(accessInfo, currentUser))
        {
            return new Error("Forbidden", "Нет доступа к текстовому посту");
        }

        Guid? viewerId = currentUser.IsAnonymous ? null : currentUser.UserId;
        var anonymousSessionId = GetAnonymousSessionId(currentUser);

        var viewer = await FindViewerAsync(postId, viewerId, anonymousSessionId);
        var isSubscribed = viewerId.HasValue && await _context.Get<Subscriber>()
            .Active()
            .AnyAsync(x => x.BlogId == post.BlogId && x.UserId == viewerId.Value);

        var authorStatistics = await _context.Get<Post>()
            .Where(x => x.BlogId == post.BlogId && !x.IsDelete)
            .GroupBy(_ => 1)
            .Select(x => new
            {
                PostsCount = x.Count(),
                TotalViewsCount = x.Sum(postItem => (long)postItem.ViewCount)
            })
            .FirstOrDefaultAsync();

        using var storage = _fileStorageFactory.CreateFileStorage();
        var mediaTasks = post.TextPostInfo.Files.Select(async file =>
            new TextPostMedia(
                file.Id,
                file.Name,
                await storage.GetFileUrlAsync(post.BlogId, file.ObjectName),
                file.ContentType,
                file.Length));
        var photoUrlTask = string.IsNullOrWhiteSpace(post.Blog.PhotoUrl)
            ? Task.FromResult<string?>(null)
            : GetPhotoUrlAsync(storage, post.BlogId, post.Blog.PhotoUrl);

        var media = await Task.WhenAll(mediaTasks);
        var photoUrl = await photoUrlTask;
        var text = post.TextPostInfo.Text ?? string.Empty;
        var isOwner = viewerId.HasValue && viewerId.Value == accessInfo.OwnerUserId;

        return new TextPostDetailResponse(
            post.Id,
            post.Title,
            Lead: null,
            text,
            post.CreatedAt,
            CalculateEstimatedReadingTime(text),
            post.ViewCount,
            post.LikeCount,
            post.DislikeCount,
            Categories: [],
            media,
            new TextPostAuthor(
                post.BlogId,
                post.Blog.Title,
                post.Blog.Description,
                photoUrl,
                post.Blog.SubscriptionsCount,
                authorStatistics?.PostsCount ?? 0,
                authorStatistics?.TotalViewsCount ?? 0),
            new TextPostViewerState(
                viewer?.IsViewed == true,
                viewer?.IsLike,
                isSubscribed,
                CanEdit: isOwner,
                CanDelete: isOwner));
    }

    private static async Task<string?> GetPhotoUrlAsync(
        IFileStorage storage,
        Guid blogId,
        string objectName) =>
        await storage.GetFileUrlAsync(blogId, objectName);

    public async Task<bool> RegisterPostViewAsync(Guid postId)
    {
        var currentUser = await _userService.GetCurrentUserAsync();
        var accessInfo = await GetPostAccessInfoAsync(postId, PostType.Text);
        if (accessInfo == null || !CanAccessPost(accessInfo, currentUser))
        {
            return false;
        }

        Guid? viewerId = currentUser.IsAnonymous ? null : currentUser.UserId;
        var anonymousSessionId = GetAnonymousSessionId(currentUser);
        if (!viewerId.HasValue && anonymousSessionId == null)
        {
            return false;
        }

        var existingViewer = await FindViewerAsync(postId, viewerId, anonymousSessionId);
        if (existingViewer != null)
        {
            if (!existingViewer.IsViewed)
            {
                _context.Attach(existingViewer);
                existingViewer.IsViewed = true;

                var existingPost = await _context.Get<Post>().FirstAsync(x => x.Id == postId);
                _context.Attach(existingPost);
                existingPost.ViewCount++;
                await _context.SaveChangesAsync();
                await _cacheService.RemoveCachedDataAsync(new PostDetailViewModelCacheKey(postId));
            }

            return true;
        }

        var post = await _context.Get<Post>().FirstAsync(x => x.Id == postId);
        _context.Attach(post);
        post.ViewCount++;
        _context.Add(new PostViewer
        {
            Id = GuidService.GetNewGuid(),
            PostId = postId,
            UserId = viewerId,
            UserIpAddress = anonymousSessionId,
            IsViewed = true,
            CreatedAt = DateTimeService.Now()
        });

        await _context.SaveChangesAsync();
        await _cacheService.RemoveCachedDataAsync(new PostDetailViewModelCacheKey(postId));
        return true;
    }

    private async Task<PostAccessInfo?> GetPostAccessInfoAsync(Guid postId, PostType type) =>
        await _context.Get<Post>()
            .Where(x => x.Id == postId && x.Type == type && x.ProcessState == ProcessState.Complete)
            .Select(x => new PostAccessInfo(
                x.IsDelete,
                x.Visibility,
                x.BanMessageId,
                x.Blog.UserId))
            .FirstOrDefaultAsync();

    private static bool CanAccessPost(PostAccessInfo post, UserModel currentUser)
    {
        if (post.IsDelete)
        {
            return false;
        }

        if (post.Visibility != PostVisibility.Private && !post.BanMessageId.HasValue)
        {
            return true;
        }

        var isOwner = !currentUser.IsAnonymous && currentUser.UserId == post.OwnerUserId;
        var isModerator = currentUser.Roles.Intersect([Roles.SuperAdminRoleId, Roles.AdminRoleId]).Any();

        return (post.Visibility != PostVisibility.Private || isOwner)
            && (!post.BanMessageId.HasValue || isOwner || isModerator);
    }

    private string? GetAnonymousSessionId(UserModel currentUser)
    {
        if (!currentUser.IsAnonymous)
        {
            return null;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext == null ? null : AnonymousSession.GetOrCreate(httpContext);
    }

    private async Task<PostViewer?> FindViewerAsync(
        Guid postId,
        Guid? viewerId,
        string? anonymousSessionId)
    {
        if (!viewerId.HasValue && string.IsNullOrWhiteSpace(anonymousSessionId))
        {
            return null;
        }

        return await _context.Get<PostViewer>()
            .Where(x => x.PostId == postId)
            .Where(x => viewerId.HasValue
                ? x.UserId == viewerId.Value
                : x.UserId == null && x.UserIpAddress == anonymousSessionId)
            .FirstOrDefaultAsync();
    }

    private static int CalculateEstimatedReadingTime(string html)
    {
        var plainText = WebUtility.HtmlDecode(HtmlTagRegex.Replace(html, " "));
        var wordCount = plainText.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;

        return Math.Max(1, (int)Math.Ceiling(wordCount / (double)WordsPerMinute));
    }

    private sealed record PostAccessInfo(
        bool IsDelete,
        PostVisibility Visibility,
        Guid? BanMessageId,
        Guid OwnerUserId);

    public async Task SetReactionToPost(ReactionCreateModel @event)
    {
        var userId = @event.UserId;
        var ipAddress = @event.RemoteIp;

        var post = await _context.Get<Post>()
            .FirstAsync(x => x.Id == @event.PostId);

        var existView = await _context.Get<PostViewer>()
            .Where(x => x.PostId == post.Id)
            .Where(x => userId.HasValue
                ? x.UserId == userId
                : x.UserId == null && x.UserIpAddress == ipAddress)
            .FirstOrDefaultAsync();

        _context.Attach(post);

        if (existView == null)
        {
            if (@event.IsLike == true)
            {
                post.LikeCount++;
            }
            if (@event.IsLike == false)
            {
                post.DislikeCount++;
            }
            post.ViewCount++;
            existView = new PostViewer
            {
                Id = GuidService.GetNewGuid(),
                PostId = @event.PostId,
                IsLike = @event.IsLike,
                IsViewed = true,
                UserId = userId,
                UserIpAddress = ipAddress,
                CreatedAt = DateTimeService.Now(),
            };
            _context.Add(existView);
        }
        else
        {
            if (@event.IsLike.HasValue)
            {
                if (@event.IsLike == true)
                {
                    post.LikeCount = Math.Max(post.LikeCount + (existView!.IsLike == true ? -1 : 1), 0);
                    post.DislikeCount = Math.Max(post.DislikeCount + (existView!.IsLike == false ? -1 : 0), 0);
                }
                else
                {
                    post.LikeCount = Math.Max(post.LikeCount + (existView!.IsLike == true ? -1 : 0), 0);
                    post.DislikeCount = Math.Max(post.DislikeCount + (existView!.IsLike == false ? -1 : 1), 0);
                }
            }
            else
            {
                if (existView!.IsLike == true)
                {
                    post.LikeCount = Math.Max(post.LikeCount - 1, 0);
                }
                else if (existView!.IsLike == false)
                {
                    post.DislikeCount = Math.Max(post.DislikeCount - 1, 0);
                }
            }

            _context.Attach(existView);
            existView.IsLike = @event.IsLike == existView.IsLike ? null : @event.IsLike;
            existView.UserId = userId;
            existView.UserIpAddress = ipAddress;
        }

        await _context.SaveChangesAsync();
        await _cacheService.RemoveCachedDataAsync(new PostDetailViewModelCacheKey(post.Id));
    }

    public IEnumerable<SelectItem<PostVisibility>> GetPostVisibilityList()
    {
        return Enum.GetValues<PostVisibility>().Select(x => new SelectItem<PostVisibility>(x, x.FormatName()));
    }

    public async Task<Result<PostEditViewModel>> GetPostUpdateModelAsync(Guid postId)
    {
        var currentUser = await _userService.GetCurrentUserAsync();

        var post = await _context.Get<Post>()
            .FirstOrDefaultAsync(x => x.Id == postId);

        if (post == null)
        {
            return new Error("Пост не найден");
        }

        if (currentUser.BlogId != post.BlogId)
        {
            return new Error("Пост не относится к вашему блогу");
        }
        return null!;
        //return new PostEditViewModel(
        //    post.Id,
        //    post.Title,
        //    post.Description,
        //    post.PreviewId,
        //    post.Visibility,
        //    post.PaymentSubscriptionId,
        //    post.PostCategories.Select(x => x.CategoryId).ToList()
        //);

    }

    public async Task<IReadOnlyList<PostCommonModel>> GetPostCommonModelAsync(IEnumerable<Guid> postIds)
    {
        using var fileStorage = _fileStorageFactory.CreateFileStorage();

        var posts = await _context.Get<Post>()
            .Where(x => postIds.Contains(x.Id))
            .Include(x => x.Blog)
            .Include(x => x.VideoPostInfo)
            .Include(x => x.VideoPostInfo.PreviewFile)
            .Include(x => x.TextPostInfo)
            .ToListAsync();

        var result = posts.Select(async post =>
        {
            var description = post.Type == PostType.Video
                ? post.VideoPostInfo?.Description
                : post.TextPostInfo?.Text;
            var previewObjectName = post.Type == PostType.Video
                && post.VideoPostInfo?.PreviewFile is not null
                    ? await fileStorage.GetFileUrlAsync(
                        post.BlogId,
                        post.VideoPostInfo.PreviewFile.ObjectName)
                    : null;
            var creatorAvatarUrl = post.Blog.PhotoUrl is not null
                ? await fileStorage.GetFileUrlAsync(post.BlogId, post.Blog.PhotoUrl)
                : null;

            return new PostCommonModel
            {
                Id = post.Id,
                Description = description,
                Title = post.Title,
                PreviewObjectName = previewObjectName,
                Creator = new PostCreatorModel
                {
                    UserId = post.Blog.UserId,
                    BlogId = post.Blog.Id,
                    Name = post.Blog.Title,
                    AvatarUrl = creatorAvatarUrl
                }
            };
        });

        return await Task.WhenAll(result);
    }

    public async Task<IReadOnlyList<PostCommonModel>> GetCurrentUserPostListAsync()
    {
        var user = await _userService.GetCurrentUserAsync();

        var posts = await _context.Get<Post>()
            .Where(x => x.IsDelete == false)
            .Where(x => x.BlogId == user.BlogId)
            .Include(x => x.Blog)
            .Include(x => x.VideoPostInfo)
            .Include(x => x.VideoPostInfo.PreviewFile)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        using var storage = _fileStorageFactory.CreateFileStorage();

        var result = posts.Select(async post =>
        {
            var creatorAvatarUrl = post.Blog.PhotoUrl is not null
                ? await storage.GetFileUrlAsync(post.BlogId, post.Blog.PhotoUrl)
                : null;

            return new PostCommonModel
            {
                Id = post.Id,
                Description = post.VideoPostInfo.Description,
                Title = post.Title,
                PreviewObjectName = post.VideoPostInfo.PreviewId.HasValue
                    ? await storage.GetFileUrlAsync(post.BlogId, post.VideoPostInfo.PreviewFile!.ObjectName)
                    : null,
                Creator = new PostCreatorModel
                {
                    UserId = post.Blog.UserId,
                    BlogId = post.Blog.Id,
                    Name = post.Blog.Title,
                    AvatarUrl = creatorAvatarUrl
                }
            };
        });

        return await Task.WhenAll(result);

    }

    public async Task<IReadOnlyList<PostCommonModel>> GetPostCommonModelWithExcludeIdsAsync(IEnumerable<Guid> excludePostIds)
    {
        var user = await _userService.GetCurrentUserAsync();
        using var fileStorage = _fileStorageFactory.CreateFileStorage();

        var posts = await _context.Get<Post>()
            .Where(x => x.BlogId == user.BlogId)
            .Where(x => !excludePostIds.Contains(x.Id))
            .Include(x => x.Blog)
            .Include(x => x.VideoPostInfo)
            .Where(x => x.IsDelete == false)
            .Include(x => x.VideoPostInfo.PreviewFile)
            .ToListAsync();

        var result = posts.Select(async post =>
        {
            var creatorAvatarUrl = post.Blog.PhotoUrl is not null
                ? await fileStorage.GetFileUrlAsync(post.BlogId, post.Blog.PhotoUrl)
                : null;

            return new PostCommonModel
            {
                Id = post.Id,
                Description = post.VideoPostInfo.Description,
                Title = post.Title,
                PreviewObjectName = post.VideoPostInfo.PreviewId.HasValue
                    ? await fileStorage.GetFileUrlAsync(post.BlogId, post.VideoPostInfo.PreviewFile!.ObjectName)
                    : null,
                Creator = new PostCreatorModel
                {
                    UserId = post.Blog.UserId,
                    BlogId = post.Blog.Id,
                    Name = post.Blog.Title,
                    AvatarUrl = creatorAvatarUrl
                }
            };
        });

        return await Task.WhenAll(result);
    }
}
