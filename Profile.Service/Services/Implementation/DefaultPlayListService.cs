using Blog.Domain.Entities;
using Blog.Domain.Services;
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

        public async Task<Result<PlayListViewModel>> AddVideoToPlayListAsync(PlayListItemAddRequest playListItem)
        {
            var user = await _userSession.GetUserSessionAsync();
            user.AssertFound();
            var key = new PlayListCacheKey(playListItem.PlayListId);
            var playlist = await _cacheService.GetOrAddDataAsync(key, async () =>
            {
                var data = await _repository.Get<PlayList>()
                .Where(x => x.Id == playListItem.PlayListId && x.IsDeleted == false)
                .Include(x => x.PlayListItems)
                .FirstOrDefaultAsync();
                return data;
            });

            if (playlist == null)
            {
                return new Error("403", "Not found");
            }

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
            using var fileStorage = _fileStorageFactory.CreateFileStorage();
            _repository.Attach(playlist);
            var isAdded = playlist.AddVideo(new PlayListItem(playListItem.PostId, playlist.Id, position));

            if (isAdded.IsFailure)
            {
                return isAdded.Error!;
            }

            await _repository.SaveChangesAsync();
            await _cacheService.SetCachedDataAsync(key, playlist, TimeSpan.FromMinutes(10));
            return new PlayListViewModel
            {
                Id = playlist.Id,
                ThumbnailUrl = await fileStorage.GetFileUrlAsync(user.UserId.Value, playlist.ThumbnailId),
                Title = playlist.Title,
                Posts = playlist.PlayListItems.Select(x => x.PostId).ToList(),
            };
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

            var playlist = new PlayList(playList.Title, blogId, playList.ThumbnailId, playList.PostIds);
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
                Posts = x.PlayListItems.Select(x => x.PostId).ToList()
            }).ToListAsync();

            return Result<IReadOnlyList<PlayListViewModel>>.Success(result);
        }

        public async Task<Result<PlayListDetailViewModel>> GetPlayListDetailAsync(Guid id)
        {
            var playListKey = new PlayListCacheKey(id);
            var playlist = await _cacheService.GetOrAddDataAsync(playListKey, async () =>
            {
                var data = await _repository.Get<PlayList>()
                .Where(x => x.Id == id)
                .Include(x => x.PlayListItems)
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
                var result = new PlayListDetailViewModel
                {
                    Id = playlist.Id,
                    Title = playlist.Title,
                    ThumbnailUrl = playlist.ThumbnailId != null ? await fileStorage.GetFileUrlAsync(userId, playlist.ThumbnailId) : null,
                    Posts = await playlist.PlayListItems.ToAsyncEnumerable().SelectAwait(async x => await _postService.GetDetailPostByIdAsync(x.PostId)).ToListAsync(),
                };
                return result;
            });
            return Result<PlayListDetailViewModel>.Success(playlistViewModel);
        }
    }
}
