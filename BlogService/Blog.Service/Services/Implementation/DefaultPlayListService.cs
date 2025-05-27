using Blog.Domain.Entities;
using Blog.Domain.Services;
using Blog.Domain.Services.Models;
using Blog.Domain.Services.Models.Playlist;
using FileStorage.Service.Service;
using Infrastructure.Interface;
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
        private readonly IUserSession _userSession;
        private readonly IPostService _postService;

        public DefaultPlayListService(IReadWriteRepository<IBlogEntity> repository, ICacheService cacheService, IFileStorageFactory fileStorageFactory, IUserSession userSession, IPostService postService)
        {
            _repository = repository;
            _cacheService = cacheService;
            _fileStorageFactory = fileStorageFactory;
            _userSession = userSession;
            _postService = postService;
        }

        public async Task<Result<PlayListViewModel>> AddVideoToPlayListAsync(PlayListItemAddRequest playListItems)
        {
            var user = await _userSession.GetUserSessionAsync();
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
            using var fileStorage = _fileStorageFactory.CreateFileStorage();

            foreach (var playListItem in playListItems.Items)
            {
                var position = 0;
                if (playListItem.Position.HasValue)
                {
                    if (playlist.PlayListItems.Any(x => x.Position == playListItem.Position.Value))
                    {
                        return new Error("400", $"Нельзя добавить на позицию {playListItem.Position}");
                    }
                    position = playListItem.Position.Value;
                }
                else
                {
                    position = playlist.PlayListItems.Count != 0 ? playlist.PlayListItems.Max(x => x.Position) + 1 : 1;
                }
                _repository.Attach(playlist);
                var isAdded = playlist.AddVideo(new PlayListItem(playListItem.PostId, playlist.Id, position));


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

            _repository.Attach(playlist);

            var result = playlist.ChangeVideoPosition(changePostPositionRequest.PostId, changePostPositionRequest.Destination);
            if (result.IsFailure)
            {
                return result.Error;
            }
            await _repository.SaveChangesAsync();
            await _cacheService.SetCachedDataAsync(key, playlist, TimeSpan.FromMinutes(10));
            await _cacheService.RemoveCachedDataAsync(new PlayListDetailCacheKey(playlist.Id));
            return Result<PostCommonModel>.Success(null);
        }

        public async Task<Result<PlayListDetailViewModel>> CreatePlayListAsync(PlayListCreateRequest playList)
        {
            var user = await _userSession.GetUserSessionAsync();
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

            var result = new PlayListDetailViewModel
            {
                Id = playlist.Id,
                Title = playlist.Title,
                ThumbnailUrl = playlist.ThumbnailId != null ? await fileStorage.GetFileUrlAsync(user.UserId.Value, playlist.ThumbnailId) : null,
                Posts = await playlist.PlayListItems.ToAsyncEnumerable().SelectAwait(async x => await _postService.GetDetailPostByIdAsync(x.PostId)).ToListAsync(),
            };

            await _cacheService.SetCachedDataAsync(new PlayListDetailCacheKey(result.Id), result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<Result<IEnumerable<PostCommonModel>>> GetAvailablePostsToPlayListByIdAsync(Guid? playlistId)
        {
            using var storage = _fileStorageFactory.CreateFileStorage();

            var user = await _userSession.GetUserSessionAsync();
            var userBlog = await _repository.Get<PersonBlog>()
                .Where(x => x.UserId == user.UserId)
                .Select(x => x.Id)
                .FirstAsync();

            var availablePostQuery = _repository.Get<Post>()
                .Where(x => x.BlogId == userBlog)
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

            var userId = await _repository.Get<PersonBlog>()
                .Where(x => x.Id == playlist.BlogId)
                .Select(x => x.UserId)
                .FirstAsync();

            using var fileStorage = _fileStorageFactory.CreateFileStorage();
            var key = new PlayListDetailCacheKey(id);

            var playlistViewModel = await _cacheService.GetOrAddDataAsync(key, async () =>
            {
                return new PlayListDetailViewModel
                {
                    Id = playlist.Id,
                    Title = playlist.Title,
                    ThumbnailUrl = playlist.ThumbnailId != null ? await fileStorage.GetFileUrlAsync(userId, playlist.ThumbnailId) : null,
                    Posts = await playlist.PlayListItems.OrderBy(x => x.Position).ToAsyncEnumerable().SelectAwait(async x => await _postService.GetDetailPostByIdAsync(x.PostId)).ToListAsync(),
                };
            });

            return playlistViewModel;
        }

        public async Task<Result<PlayListViewModel>> RemoveVideoFromPlayListAsync(PlayListItemRemoveRequest playListRequest)
        {
            var key = new PlayListCacheKey(playListRequest.PlayListId);
            var playlist = await _repository.Get<PlayList>()
                .Where(x => x.Id == playListRequest.PlayListId && x.IsDeleted == false)
                .Include(x => x.PlayListItems)
                .FirstOrDefaultAsync();

            if (playlist == null) { return Result<PlayListViewModel>.Failure(new("404", "Плейлист не найден")); }

            _repository.Attach(playlist);
            playlist.RemoveVideo(playListRequest.PostId);
            await _repository.SaveChangesAsync();
            await _cacheService.SetCachedDataAsync(key, playlist, TimeSpan.FromMinutes(10));
            using var fileStorage = _fileStorageFactory.CreateFileStorage();
            var userId = (await _userSession.GetUserSessionAsync()).UserId!.Value;
            await _cacheService.RemoveCachedDataAsync(new PlayListDetailCacheKey(playlist.Id));

            return new PlayListViewModel
            {
                Id = playlist.Id,
                ThumbnailUrl = playlist.ThumbnailId == null ? null : await fileStorage.GetFileUrlAsync(userId!, playlist.ThumbnailId!),
                Title = playlist.Title,
                Posts = await playlist.PlayListItems.ToAsyncEnumerable().SelectAwait(async x => await _postService.GetDetailPostByIdAsync(x.PostId)).ToListAsync()
            };
        }
    }
}
