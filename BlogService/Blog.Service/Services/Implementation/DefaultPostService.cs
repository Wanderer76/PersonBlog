using Blog.Domain.Entities;
using Blog.Domain.Events;
using Blog.Domain.Services.Models;
using Blog.Persistence.Repository.Quries;
using Blog.Service.Models.File;
using Blog.Service.Models.Post;
using FileStorage.Service.Service;
using Infrastructure.Interface;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;
using System.Text.Json;

namespace Blog.Service.Services.Implementation
{
    internal class DefaultPostService : IPostService
    {
        private readonly IReadWriteRepository<IBlogEntity> _context;
        private readonly IFileStorageFactory _fileStorageFactory;
        private readonly ICacheService _cacheService;
        private readonly IUserSession _userSession;
        public DefaultPostService(IReadWriteRepository<IBlogEntity> context, IFileStorageFactory fileStorageFactory, ICacheService cacheService, IUserSession userSession)
        {
            _context = context;
            _fileStorageFactory = fileStorageFactory;
            _cacheService = cacheService;
            _userSession = userSession;
        }

        public async Task<Result<Guid, ErrorList>> CreatePostAsync(PostCreateDto postCreateDto)
        {
            var blog = await _context.Get<PersonBlog>()
            .FirstAsync(x => x.UserId == postCreateDto.UserId);

            var postId = GuidService.GetNewGuid();
            var now = DateTimeService.Now();

            var hasSubscription = postCreateDto.SubscriptionLevelId.HasValue ? await _context.Get<PaymentSubscription>()
                .Where(x => x.BlogId == blog.Id)
                .Where(x => x.Id == postCreateDto.SubscriptionLevelId)
                .Where(x => x.IsDeleted == false)
                .AnyAsync()
                : true;

            if (!hasSubscription)
            {
                return Result<Guid, ErrorList>.Failure(new ErrorList([new Error("", "Не существует текущего уровня подписки")]));
            }

            var post = new Post(postId, blog.Id, postCreateDto.Type, postCreateDto.Text, postCreateDto.Title, postCreateDto.SubscriptionLevelId, postCreateDto.Visibility);
            _context.Add(post);
            await _context.SaveChangesAsync();

            return Result<Guid, ErrorList>.Success(postId);
        }

        public async Task<Result<PostFileMetadataModel, ErrorList>> GetVideoFileMetadataByPostIdAsync(Guid postId)
        {
            var post = await _context.Get<Post>()
                .Where(x => x.Id == postId)
                .Select(x => new { x.Visibility, x.Blog.UserId, x.PreviewId })
                .FirstAsync();

            if (post.Visibility == PostVisibility.Private)
            {
                var session = await _userSession.GetUserSessionAsync();
                if (session.UserId != post.UserId)
                {
                    return Result<PostFileMetadataModel, ErrorList>.Failure(new List<Error> { new Error("403", "Forbiden") });
                }
            }

            var fileMetadata = await _cacheService.GetCachedDataAsync<VideoMetadata>($"VideoMetadata:{postId}");
            if (fileMetadata == null)
            {
                fileMetadata = await _context.Get<VideoMetadata>()
                    .Where(x => x.PostId == postId)
                    .FirstAsync();

                await _cacheService.SetCachedDataAsync($"VideoMetadata:{postId}", fileMetadata, TimeSpan.FromHours(1));
            }

            return Result<PostFileMetadataModel, ErrorList>.Success(new PostFileMetadataModel(
                fileMetadata.ContentType,
                fileMetadata.Length,
                fileMetadata.Name,
                fileMetadata.CreatedAt,
                fileMetadata.Id,
                fileMetadata.ObjectName,
                await _fileStorageFactory.CreateFileStorage().GetFileUrlAsync(postId, post.PreviewId.ToString()),
                postId));
        }

        public async Task<Guid> GetVideoChunkStreamByPostIdAsync(Guid postId, Guid fileMetadataId, long offset, long length, Stream output)
        {
            var videoData = await _context.Get<VideoMetadata>()
                .Where(x => x.Id == fileMetadataId)
                .FirstAsync();

            var storage = _fileStorageFactory.CreateFileStorage();
            await storage.ReadFileByChunksAsync(postId, videoData.ObjectName, offset, length, output);
            return fileMetadataId;
        }

        public Task GetVideoStream(Guid postId, Stream output)
        {
            throw new NotImplementedException();
        }

        public async ValueTask<bool> HasVideoExistByPostIdAsync(Guid postId)
        {
            var hasVideo = await _context.Get<VideoMetadata>()
                .Where(x => x.PostId == postId)
                .AnyAsync();
            return hasVideo;
        }

        public async Task<PostPagedListViewModel> GetPostsByBlogIdPagedAsync(Guid blogId, int page, int limit)
        {
            var pagedPosts = await _context.GetPostByBlogIdPagedAsync(blogId, page, limit);

            var profileId = await _context.Get<PersonBlog>()
                .Where(x => x.Id == blogId)
                .Select(x => x.UserId)
                .FirstAsync();

            var fileStorage = _fileStorageFactory.CreateFileStorage();
            var posts = new List<PostModel>(pagedPosts.Posts.Count());
            var cachedPosts = (await _cacheService.GetCachedDataAsync<PostModel>(pagedPosts.Posts.Select(x => $"{nameof(PostModel)}:{x.Id}"))).ToList();
            if (cachedPosts.Count != pagedPosts.Posts.Count())
            {
                foreach (var post in pagedPosts.Posts.ExceptBy(cachedPosts.Select(x => x.Id), x => x.Id))
                {
                    var previewUrl = string.IsNullOrWhiteSpace(post.PreviewId) ? null : await fileStorage.GetFileUrlAsync(post.Id, post.PreviewId);
                    var isProcessed = post.VideoFile != null ? post.VideoFile.ProcessState : ProcessState.Running;
                    var videoFile = post.VideoFile;
                    var postModel = new PostModel(
                                    post.Id,
                                    post.Type,
                                    post.Title,
                                    post.Description,
                                    post.CreatedAt,
                                    previewUrl,
                                    post.VideoFile != null && isProcessed == ProcessState.Complete ?
                                    new VideoMetadataModel(
                                        videoFile.Id,
                                        videoFile.Length,
                                        videoFile.ContentType,
                                        videoFile.ObjectName
                                    ) : null,
                                    isProcessed,
                                    isProcessed == ProcessState.Error ? videoFile?.ErrorMessage : null
                                );
                    posts.Add(postModel);
                    cachedPosts.Add(postModel);
                    await _cacheService.SetCachedDataAsync($"{nameof(PostModel)}:{postModel.Id}", postModel, TimeSpan.FromHours(10));

                }
            }
            return new PostPagedListViewModel
            {
                TotalPageCount = pagedPosts.TotalPagesCount,
                TotalPostsCount = pagedPosts.TotalPagesCount * limit,
                Posts = cachedPosts.OrderByDescending(x => x.CreatedAt),
            };
        }

        public async Task RemovePostByIdAsync(Guid id)
        {
            var post = await _context.Get<Post>()
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();
            if (post != null)
            {
                _context.Attach(post);
                post.IsDeleted = true;
            }

            await _cacheService.RemoveCachedDataAsync($"{nameof(PostModel)}:{id}");
            await _context.SaveChangesAsync();
        }

        public async Task UploadVideoChunkAsync(UploadVideoChunkDto uploadVideoChunkDto)
        {
            var fileStorage = _fileStorageFactory.CreateFileStorage();
            var cacheMetadata = await _cacheService.GetCachedDataAsync<VideoMetadata>($"{nameof(VideoMetadata)}:{uploadVideoChunkDto.PostId}");
            var metadata = cacheMetadata ?? await _context.Get<VideoMetadata>()
                    .Where(x => x.IsProcessed == true)
                    .FirstAsync(x => x.PostId == uploadVideoChunkDto.PostId);

            await fileStorage.PutFileChunkAsync(uploadVideoChunkDto.PostId, GuidService.GetNewGuid(), uploadVideoChunkDto.ChunkData, new VideoChunkUploadingInfo
            {
                FileId = metadata.Id,
                ChunkNumber = uploadVideoChunkDto.ChunkNumber,
            });

            if (uploadVideoChunkDto.TotalChunkCount == uploadVideoChunkDto.ChunkNumber)
            {
                var videoCreateEvent = new CombineFileChunksCommand
                {
                    VideoMetadataId = metadata.Id,
                    PostId = uploadVideoChunkDto.PostId,
                };

                var videoEvent = new VideoProcessEvent
                {
                    Id = GuidService.GetNewGuid(),
                    EventData = JsonSerializer.Serialize(videoCreateEvent),
                    EventType = nameof(CombineFileChunksCommand),
                };
                _context.Add(videoEvent);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<PostModel> UpdatePostAsync(PostEditDto postEditDto)
        {
            var post = await _context.Get<Post>()
                .Include(x => x.VideoFile)
                .FirstOrDefaultAsync(x => x.Id == postEditDto.Id) ?? throw new ArgumentException("Пост не найден");

            var blogUserId = await _context.Get<PersonBlog>()
                .Where(x => x.Id == post.BlogId)
                .Select(x => x.UserId)
                .FirstAsync();

            if (blogUserId != postEditDto.UserId)
            {
                throw new ArgumentException("Пост вам не принадлежит");
            }
            var storage = _fileStorageFactory.CreateFileStorage();

            _context.Attach(post);
            if (postEditDto.PreviewId != null)
            {
                var snapshotFileId = GuidService.GetNewGuid();
                using var copyStream = postEditDto.PreviewId.OpenReadStream();
                copyStream.Position = 0;
                var objectName = await storage.PutFileAsync(post.Id, snapshotFileId, copyStream);
                post.PreviewId = objectName;
            }
            _context.Attach(post);
            post.Title = postEditDto.Title;
            post.Description = postEditDto.Description;
            await _context.SaveChangesAsync();

            var previewUrl = string.IsNullOrWhiteSpace(post.PreviewId) ? null : await storage.GetFileUrlAsync(post.Id, post.PreviewId);
            var isProcessed = post.VideoFile != null ? post.VideoFile.ProcessState : ProcessState.Running;
            var videoMetadata = post.VideoFile;
            var result = new PostModel(
                            post.Id,
                            post.Type,
                            post.Title,
                            post.Description,
                            post.CreatedAt,
                            previewUrl,
                            post.VideoFile != null && isProcessed == ProcessState.Complete ?
                            new VideoMetadataModel(
                                videoMetadata.Id,
                                videoMetadata.Length,
                                videoMetadata.ContentType,
                                videoMetadata.ObjectName
                            ) : null,
                            isProcessed,
                            isProcessed == ProcessState.Error ? videoMetadata?.ErrorMessage : null
                        );

            await _cacheService.SetCachedDataAsync($"PostModel:{result.Id}", result, TimeSpan.FromHours(10));

            return result;
        }

        public async Task<PostDetailViewModel> GetDetailPostByIdAsync(Guid postId)
        {
            var cacheData = await _cacheService.GetOrAddDataAsync(new PostDetailViewModelCacheKey(postId), async () =>
            {
                var post = await _context.Get<Post>()
                .Include(x => x.VideoFile)
                .Include(x => x.Blog)
                .FirstAsync(x => x.Id == postId);

                if (post.Visibility == PostVisibility.Private)
                {
                    var session = await _userSession.GetUserSessionAsync();
                    if (session.UserId != post.Blog.UserId)
                    {
                        throw new ArgumentException();
                    }
                }
                var fileStorage = _fileStorageFactory.CreateFileStorage();

                var previewUrl = string.IsNullOrWhiteSpace(post.PreviewId)
                    ? null
                    : await fileStorage.GetFileUrlAsync(post.Id, post.PreviewId);

                var videoMetadata = post.VideoFile;
                var isProcessed = post.VideoFile != null ? post.VideoFile.IsProcessed : false;

                return new PostDetailViewModel(
                    post.Id,
                    previewUrl,
                    post.CreatedAt,
                    post.ViewCount,
                    post.Description,
                    post.Title,
                    post.Type,
                    post.LikeCount,
                    post.DislikeCount,
                    post.VideoFile != null && !isProcessed ?
                                new VideoMetadataModel(
                                    videoMetadata.Id,
                                    videoMetadata.Length,
                                    videoMetadata.ContentType,
                                    videoMetadata.ObjectName
                                ) : null,
                    isProcessed
                );
            });
            return cacheData;
        }

        public Task SetVideoViewed(ViewedVideoModel value)
        {
            throw new NotImplementedException();
            //var dateFromNow = DateTimeOffset.UtcNow.AddMonths(-2);
            //var hasView = await _context.Get<PostViewers>()
            //    .Where(x => x.PostId == value.PostId)
            //    .Where(x => x.UserId == value.UserId)
            //    .Where(x => x.UserIpAddress == value.RemoteIp)
            //    .Where(x => x.CreatedAt < dateFromNow)
            //    .AnyAsync();

            //if (hasView)
            //{
            //    return;
            //}

            //var videoViewEvent = new VideoViewEvent
            //{
            //    EventId = GuidService.GetNewGuid(),
            //    PostId = value.PostId,
            //    CreatedAt = DateTimeOffset.UtcNow,
            //    RemoteIp = value.RemoteIp,
            //    UserId = value.UserId,
            //};

            //var videoEvent = new ProfileEventMessages
            //{
            //    Id = videoViewEvent.EventId,
            //    EventData = JsonSerializer.Serialize(videoViewEvent),
            //    EventType = nameof(VideoViewEvent),
            //    State = EventState.Pending,
            //};
            //_context.Add(videoEvent);
            //await _context.SaveChangesAsync();
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
                    UserIpAddress = ipAddress,
                };
                _context.Add(existView);
            }
            else
            {
                if (@event.IsLike.HasValue)
                {
                    if (@event.IsLike == true)
                    {
                        post.LikeCount = Math.Max(post.LikeCount + (existView?.IsLike == true ? -1 : 1), 0);
                        post.DislikeCount = Math.Max(post.DislikeCount + (existView?.IsLike == false ? -1 : 0), 0);
                    }
                    else
                    {
                        post.LikeCount = Math.Max(post.LikeCount + (existView?.IsLike == true ? -1 : 0), 0);
                        post.DislikeCount = Math.Max(post.DislikeCount + (existView?.IsLike == false ? -1 : 1), 0);
                    }
                }
                else
                {
                    if (existView?.IsLike == true)
                    {
                        post.LikeCount = Math.Max(post.LikeCount - 1, 0);
                    }
                    else if (existView?.IsLike == false)
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
        }
        public async Task<bool> CheckForViewAsync(Guid? userId, string? ipAddress)
        {
            if (userId == null && ipAddress == null)
            {
                return true;
            }
            return await _context.Get<PostViewer>()
                .Where(x => x.UserId == userId && x.UserIpAddress == ipAddress)
                .AnyAsync();
        }

        public Task<IEnumerable<SelectItem<PostVisibility>>> GetPostVisibilityListAsync()
        {
            return Task.FromResult(Enum.GetValues<PostVisibility>().Select(x => new SelectItem<PostVisibility>(x, x.FormatName())));
        }
    }
}
