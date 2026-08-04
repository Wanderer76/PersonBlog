using Authentication.Contract.Constants;
using Blog.Contracts.Events;
using Blog.Contracts.Models;
using Blog.Contracts.Models.File;
using Blog.Contracts.Models.Post;
using Blog.Contracts.Services;
using Blog.Domain.Entities;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace Blog.Service.Services.Implementation;

internal class DefaultPostService : IPostService
{
    private readonly IReadWriteRepository<IBlogEntity> _context;
    private readonly IFileStorageFactory _fileStorageFactory;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUserService _userService;

    public DefaultPostService(IReadWriteRepository<IBlogEntity> context, IFileStorageFactory fileStorageFactory, ICacheService cacheService, ICurrentUserService userService)
    {
        _context = context;
        _fileStorageFactory = fileStorageFactory;
        _cacheService = cacheService;
        _userService = userService;
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
        var isBanned = await _context.Get<Post>()
            .Where(x => x.Id == postId)
            .Select(x => new { x.BanMessageId, x.BlogId })
            .FirstAsync();

        var currentUser = await _userService.GetCurrentUserAsync();

        if ((isBanned.BanMessageId.HasValue && !currentUser.Roles.Intersect([Roles.SuperAdminRoleId, Roles.AdminRoleId]).Any())
            && !(currentUser.HasBlog && isBanned.BlogId == currentUser.BlogId))
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

            if (post.Visibility == PostVisibility.Private)
            {
                var session = await _userService.GetCurrentUserAsync();
                if (session.UserId != post.Blog.UserId)
                {
                    throw new ArgumentException();
                }
            }
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

    public async Task SetReactionToPost(ReactionCreateModel @event)
    {
        var userId = @event.UserId;
        var ipAddress = @event.RemoteIp;

        var post = await _context.Get<Post>()
            .FirstAsync(x => x.Id == @event.PostId);

        var existView = await _context.Get<PostViewer>()
            .Where(x => x.PostId == post.Id)
            .Where(x => x.UserId == userId || x.UserIpAddress == ipAddress)
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
                UserId = userId,
                UserIpAddress = ipAddress!,
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
            existView.UserIpAddress = ipAddress!;
        }

        await _context.SaveChangesAsync();
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
            .Include(x => x.VideoPostInfo)
            .Include(x => x.VideoPostInfo.PreviewFile)
            .ToListAsync();

        var result = posts.Select(async post =>
        {
            return new PostCommonModel
            {
                Id = post.Id,
                Description = post.VideoPostInfo.Description,
                Title = post.Title,
                PreviewObjectName = post.VideoPostInfo.PreviewId.HasValue ? await fileStorage.GetFileUrlAsync(post.BlogId, post.VideoPostInfo.PreviewFile!.ObjectName) : null
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
            .Include(x => x.VideoPostInfo)
            .Include(x => x.VideoPostInfo.PreviewFile)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        using var storage = _fileStorageFactory.CreateFileStorage();

        var result = posts.Select(async post => new PostCommonModel
        {
            Id = post.Id,
            Description = post.VideoPostInfo.Description,
            Title = post.Title,
            PreviewObjectName = post.VideoPostInfo.PreviewId.HasValue ? await storage.GetFileUrlAsync(post.BlogId, post.VideoPostInfo.PreviewFile!.ObjectName) : null
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
            .Include(x => x.VideoPostInfo)
            .Where(x => x.IsDelete == false)
            .Include(x => x.VideoPostInfo.PreviewFile)
            .ToListAsync();

        var result = posts.Select(async post =>
        {
            return new PostCommonModel
            {
                Id = post.Id,
                Description = post.VideoPostInfo.Description,
                Title = post.Title,
                PreviewObjectName = post.VideoPostInfo.PreviewId.HasValue ? await fileStorage.GetFileUrlAsync(post.BlogId, post.VideoPostInfo.PreviewFile!.ObjectName) : null
            };
        });

        return await Task.WhenAll(result);
    }
}
