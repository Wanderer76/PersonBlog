using Blog.Domain.Entities;
using Blog.Domain.Services;
using Blog.Domain.Services.Models;
using Blog.Domain.Services.Models.Playlist;
using FileStorage.Service.Service;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Utils;

namespace Blog.Service.Services.Implementation
{
    internal class DefaultPlayListService : IPlayListService
    {
        private readonly IReadWriteRepository<IBlogEntity> _repository;
        private readonly ICacheService _cacheService;
        private readonly IFileStorageFactory _fileStorageFactory;
        private readonly ICurrentUserService _userSession;
        private readonly IPostService _postService;

        public DefaultPlayListService(IReadWriteRepository<IBlogEntity> repository, ICacheService cacheService, IFileStorageFactory fileStorageFactory, ICurrentUserService userSession, IPostService postService)
        {
            _repository = repository;
            _cacheService = cacheService;
            _fileStorageFactory = fileStorageFactory;
            _userSession = userSession;
            _postService = postService;
        }

        public async Task<Result<PlayListViewModel>> AddVideoToPlayListAsync(PlayListItemAddRequest playListItems)
        {
            var user = await _userSession.GetCurrentUserAsync();
            user.AssertFound();
            var key = new PlayListCacheKey(playListItems.PlayListId);

            var playlist = await _cacheService.GetOrAddDataAsync(key, async () =>
            {
                return await _repository.Get<PlayList>()
                .Where(x => x.Id == playListItems.PlayListId && x.IsDeleted == false)
                .Include(x => x.PlayListItems)
                .FirstOrDefaultAsync();
            });

            if (playlist == null)
            {
                return new Error("403", "Not found");
            }

            var userBlogId = await _repository.Get<PersonBlog>()
               .Where(x => x.UserId == user.UserId.Value)
               .Select(x => x.Id)
               .FirstAsync();

            if (userBlogId != playlist.BlogId)
            {
                return new Error("403");
            }


            using var fileStorage = _fileStorageFactory.CreateFileStorage();

            _repository.Attach(playlist);
            foreach (var playListItem in playListItems.Items)
            {
                var isAdded = playlist.AddVideo(playListItem.PostId);
                if (isAdded.IsFailure)
                {
                    return isAdded.Error!;
                }
            }

            await _repository.SaveChangesAsync();
            await _cacheService.SetCachedDataAsync(key, playlist, TimeSpan.FromMinutes(10));
            return new PlayListViewModel
            {
                Id = playlist.Id,
                ThumbnailUrl = await fileStorage.GetFileUrlAsync(user.UserId.Value, playlist.ThumbnailId),
                Title = playlist.Title,
                Posts = await playlist.PlayListItems.ToAsyncEnumerable().SelectAwait(async x => await _postService.GetDetailPostByIdAsync(x.PostId)).ToListAsync(),
            };
        }

        public async Task<Result<PostCommonModel>> ChangePostPositionAsync(ChangePostPositionRequest changePostPositionRequest)
        {
            var key = new PlayListCacheKey(changePostPositionRequest.PlaylistId);
            var playlist = await _cacheService.GetOrAddDataAsync(key, async () =>
            {
                return await _repository.Get<PlayList>()
                .Where(x => x.Id == changePostPositionRequest.PlaylistId)
                .Include(x => x.PlayListItems.OrderBy(x => x.Position))
                .FirstOrDefaultAsync();
            });
            if (playlist == null)
            {
                return Result<PostCommonModel>.Failure(new("404", "Не найден плейлист"));
            }
            var user = await _userSession.GetCurrentUserAsync();
            var userBlogId = await _repository.Get<PersonBlog>()
               .Where(x => x.UserId == user.UserId.Value)
               .Select(x => x.Id)
               .FirstAsync();

            if (userBlogId != playlist.BlogId)
            {
                return new Error("403");
            }


            _repository.Attach(playlist);

            var result = playlist.ChangeVideoPosition(changePostPositionRequest.PostId, changePostPositionRequest.Destination);
            if (result.IsFailure)
            {
                return result.Error;
            }
            await _repository.SaveChangesAsync();
            await _cacheService.SetCachedDataAsync(key, playlist, TimeSpan.FromMinutes(10));
            return Result<PostCommonModel>.Success(null);
        }

        public async Task<Result<PlayListDetailViewModel>> CreatePlayListAsync(PlayListCreateRequest playList)
        {
            var user = await _userSession.GetCurrentUserAsync();
            user.AssertFound();
            using var fileStorage = _fileStorageFactory.CreateFileStorage();

            var blogId = await _repository.Get<PersonBlog>()
            .Where(x => x.UserId == user.UserId.Value)
            .Select(x => x.Id)
            .FirstAsync();

            var thumbnail = string.IsNullOrEmpty(playList.ThumbnailId)
                ? await _repository.Get<Post>()
                .Where(x => x.Id == playList.PostIds.First())
                .Select(x => x.PreviewId)
                .FirstOrDefaultAsync()
                : playList.ThumbnailId;

            var playlist = new PlayList(playList.Title, blogId, thumbnail, playList.PostIds);
            _repository.Add(playlist);
            await _repository.SaveChangesAsync();
            await _cacheService.RemoveCachedDataAsync(new PlayListCacheKey(blogId));

            var result = new PlayListDetailViewModel
            {
                Id = playlist.Id,
                CanEdit = true,
                Title = playlist.Title,
                ThumbnailUrl = playlist.ThumbnailId != null ? await fileStorage.GetFileUrlAsync(user.UserId.Value, playlist.ThumbnailId) : null,
                Posts = await playlist.PlayListItems.ToAsyncEnumerable().SelectAwait(async x => await _postService.GetDetailPostByIdAsync(x.PostId)).ToListAsync(),
            };
            return result;
        }

        public async Task<Result<IEnumerable<PostCommonModel>>> GetAvailablePostsToPlayListByIdAsync(Guid? playlistId)
        {
            using var storage = _fileStorageFactory.CreateFileStorage();

            var user = await _userSession.GetCurrentUserAsync();
            var userBlogId = await _repository.Get<PersonBlog>()
                .Where(x => x.UserId == user.UserId)
                .Select(x => x.Id)
                .FirstAsync();

            var availablePostQuery = _repository.Get<Post>()
                .Where(x => x.BlogId == userBlogId)
                .Where(x => x.VideoFile != null)
                .Where(x => x.IsDeleted == false);

            availablePostQuery = playlistId.HasValue
                ?
                availablePostQuery
                .Where(x => !_repository.Get<PlayList>()
                .Where(x => x.Id == playlistId)
                .SelectMany(x => x.PlayListItems)
                .Select(x => x.PostId).Contains(x.Id))
                : availablePostQuery;

            var post = await availablePostQuery
                .Select(x => new
                {
                    Id = x.Id,
                    Description = x.Description,
                    Title = x.Title,
                    PreviewUrl = x.PreviewId
                })
                .AsAsyncEnumerable()
                .SelectAwait(async x =>
                {
                    var fileUrl = await storage.GetFileUrlAsync(x.Id, x.PreviewUrl);
                    return new PostCommonModel
                    {
                        Id = x.Id,
                        Description = x.Description,
                        Title = x.Title,
                        PreviewUrl = fileUrl
                    };
                })
                .ToListAsync();

            return post;
        }

        public async Task<Result<IReadOnlyList<PlayListViewModel>>> GetBlogPlayListsAsync(Guid blogId)
        {
            var key = new PlayListCacheKey(blogId);
            var playlists = await _cacheService.GetOrAddDataAsync(key, async () =>
            {
                var data = await _repository.Get<PlayList>()
                .Where(x => x.BlogId == blogId && x.IsDeleted == false)
                .Include(x => x.PlayListItems)
                .ToListAsync();
                return data;
            });

            var userId = await _repository.Get<PersonBlog>()
                .Where(x => x.Id == blogId)
                .Select(x => x.UserId)
                .FirstAsync();

            using var fileStorage = _fileStorageFactory.CreateFileStorage();
            var result = await playlists.ToAsyncEnumerable().SelectAwait(async x => new PlayListViewModel
            {
                Id = x.Id,
                ThumbnailUrl = x.ThumbnailId == null ? null : await fileStorage.GetFileUrlAsync(userId, x.ThumbnailId),
                Title = x.Title,
                Posts = await x.PlayListItems.ToAsyncEnumerable().SelectAwait(async x => await _postService.GetDetailPostByIdAsync(x.PostId)).ToListAsync()
            }).ToListAsync();

            return result;
        }

        public async Task<Result<PlayListDetailViewModel>> GetPlayListDetailAsync(Guid id)
        {
            var playListKey = new PlayListCacheKey(id);
            var playlist = await _cacheService.GetOrAddDataAsync(playListKey, async () =>
            {
                var data = await _repository.Get<PlayList>()
                .Where(x => x.Id == id)
                .Include(x => x.PlayListItems.OrderBy(x => x.Position))
                .FirstOrDefaultAsync();
                return data;
            });

            playlist.AssertFound();

            var userId = (await _userSession.GetCurrentUserAsync()).UserId;


            var userBlogId = await _repository.Get<PersonBlog>()
                .Where(x => x.Id == playlist.BlogId)
                .Select(x => new { x.UserId, x.Id })
                .FirstAsync();


            var canEdit = userBlogId.UserId == userId;

            using var fileStorage = _fileStorageFactory.CreateFileStorage();

            return new PlayListDetailViewModel
            {
                Id = playlist.Id,
                Title = playlist.Title,
                CanEdit = canEdit,
                ThumbnailUrl = playlist.ThumbnailId != null ? await fileStorage.GetFileUrlAsync(userBlogId.UserId, playlist.ThumbnailId) : null,
                Posts = await playlist.PlayListItems.OrderBy(x => x.Position).ToAsyncEnumerable().SelectAwait(async x => await _postService.GetDetailPostByIdAsync(x.PostId)).ToListAsync(),
            };

        }

        public async Task<Result<PlayListViewModel>> RemoveVideoFromPlayListAsync(PlayListItemRemoveRequest playListRequest)
        {
            var key = new PlayListCacheKey(playListRequest.PlayListId);
            var playlist = await _repository.Get<PlayList>()
                .Where(x => x.Id == playListRequest.PlayListId && x.IsDeleted == false)
                .Include(x => x.PlayListItems)
                .FirstOrDefaultAsync();

            var user = await _userSession.GetCurrentUserAsync();

            if (playlist == null) { return Result<PlayListViewModel>.Failure(new("404", "Плейлист не найден")); }

            var userBlogId = await _repository.Get<PersonBlog>()
               .Where(x => x.UserId == user.UserId.Value)
               .Select(x => x.Id)
               .FirstAsync();

            if (userBlogId != playlist.BlogId)
            {
                return new Error("403");
            }


            _repository.Attach(playlist);
            playlist.RemoveVideo(playListRequest.PostId);
            await _repository.SaveChangesAsync();
            await _cacheService.SetCachedDataAsync(key, playlist, TimeSpan.FromMinutes(10));
            using var fileStorage = _fileStorageFactory.CreateFileStorage();
            var userId = (await _userSession.GetCurrentUserAsync()).UserId!.Value;

            return new PlayListViewModel
            {
                Id = playlist.Id,
                ThumbnailUrl = playlist.ThumbnailId == null ? null : await fileStorage.GetFileUrlAsync(userId!, playlist.ThumbnailId!),
                Title = playlist.Title,
                Posts = await playlist.PlayListItems.ToAsyncEnumerable().SelectAwait(async x => await _postService.GetDetailPostByIdAsync(x.PostId)).ToListAsync()
            };
        }

        public async Task<Result<PlayListDetailViewModel>> UpdatePlayListCommonDataAsync(PlayListUpdateRequest updateRequest)
        {
            using var fileStorage = _fileStorageFactory.CreateFileStorage();
            var key = new PlayListCacheKey(updateRequest.PlayListId);
            var playlist = await _cacheService.GetOrAddDataAsync(key, async () =>
            {
                var data = await _repository.Get<PlayList>()
                .Where(x => x.Id == updateRequest.PlayListId && x.IsDeleted == false)
                .Include(x => x.PlayListItems)
                .FirstOrDefaultAsync();
                data.AssertFound();
                return data;
            });
            var user = await _userSession.GetCurrentUserAsync();

            var userBlogId = await _repository.Get<PersonBlog>()
                .Where(x => x.UserId == user.UserId.Value)
                .Select(x => x.Id)
                .FirstAsync();

            if (userBlogId != playlist.BlogId)
            {
                return new Error("403");
            }


            var playlistDetailViewModel = new PlayListDetailViewModel
            {
                Id = playlist.Id,
                Title = playlist.Title,
                CanEdit = true,
                ThumbnailUrl = playlist.ThumbnailId != null ? await fileStorage.GetFileUrlAsync(user.UserId.Value, playlist.ThumbnailId) : null,
                Posts = await playlist.PlayListItems.OrderBy(x => x.Position).ToAsyncEnumerable().SelectAwait(async x => await _postService.GetDetailPostByIdAsync(x.PostId)).ToListAsync(),
            };

            if (string.IsNullOrWhiteSpace(updateRequest.ThumbnailId) && string.IsNullOrWhiteSpace(updateRequest.Title))
            {
                return playlistDetailViewModel;
            }

            _repository.Attach(playlist);
            if (!string.IsNullOrWhiteSpace(updateRequest.ThumbnailId))
            {
                if (!string.IsNullOrWhiteSpace(playlist.ThumbnailId))
                {

                    await fileStorage.RemoveFileAsync(user.UserId.Value, playlist.ThumbnailId);
                }

                playlist.ThumbnailId = updateRequest.ThumbnailId;
            }
            if (!string.IsNullOrWhiteSpace(updateRequest.Title))
            {
                playlist.Title = updateRequest.Title;
            }
            await _repository.SaveChangesAsync();


            playlistDetailViewModel.Title = playlist.Title;
            playlistDetailViewModel.ThumbnailUrl = await fileStorage.GetFileUrlAsync(user.UserId.Value, playlist.ThumbnailId);
            await _cacheService.SetCachedDataAsync(key, playlist, TimeSpan.FromMinutes(10));
            return playlistDetailViewModel;
        }
    }
}
