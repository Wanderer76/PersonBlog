using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Music.Contract.Models;
using Music.Contract.Models.PlayList;
using Music.Domain.Entities;
using Music.Domain.Services;
using Shared.Persistence;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music.Service.Services
{
    internal class DefaultPlayListService : IMusicPlayListService
    {
        private readonly IReadWriteRepository<IMusicEntity> _repository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileStorageFactory _fileStorageFactory;
        private readonly ITrackService _trackService;

        public DefaultPlayListService(IReadWriteRepository<IMusicEntity> repository, ICurrentUserService currentUserService, IFileStorageFactory fileStorageFactory, ITrackService trackService)
        {
            _repository = repository;
            _currentUserService = currentUserService;
            _fileStorageFactory = fileStorageFactory;
            _trackService = trackService;
        }

        public async Task<Result> AddTrackToPlayList(Guid trackId, ConstPlayListType liked)
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            var playList = await _repository.Get<PlayList>()
                .Where(x => x.UserId == user.UserId.Value && x.Type == liked)
                .FirstAsync();
            _repository.Attach(playList);
            await playList.AddTrackAsync(_repository, trackId);
            await _repository.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result<IReadOnlyList<PlayListViewModel>>> CreateDefaultUserPlayListsAsync()
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            return await CreateDefaultUserPlayListsAsync(currentUser.UserId.Value);
        }

        public async Task<Result<IReadOnlyList<PlayListViewModel>>> CreateDefaultUserPlayListsAsync(Guid userId)
        {
            var defaultPlayList = await _repository.Get<PlayList>()
                .Where(x => x.UserId == userId)
                .Where(x => x.Type == ConstPlayListType.Liked || x.Type == ConstPlayListType.Upload)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    TracksCount = x.Tracks.Count,
                    x.Type
                }
                )
                .ToListAsync();

            var result = defaultPlayList
                    .Select(x => new PlayListViewModel(x.Id, x.Name, x.TracksCount, null, x.Type.ToString(), true, false))
                    .ToList();

            if (result.Count == 2)
            {
                return result;
            }

            if (!defaultPlayList.Any(x => x.Type == ConstPlayListType.Liked))
            {
                var liked = new PlayList(PlayListConstants.LikedTracksPlayList, userId, ConstPlayListType.Liked, []);
                _repository.Add(liked);
                result.Add(new PlayListViewModel(liked.Id, liked.Name, 0, null, liked.Type.ToString(), true, false));
            }
            if (!defaultPlayList.Any(x => x.Type == ConstPlayListType.Upload))
            {

                var upload = new PlayList(PlayListConstants.UploadPlaylistName, userId, ConstPlayListType.Upload, []);
                _repository.Add(upload);

                result.Add(new PlayListViewModel(upload.Id, upload.Name, 0, null, upload.Type.ToString(), true, false));
            }
            await _repository.SaveChangesAsync();

            return result;
        }

        public async Task<Result<PlayListViewModel>> CreatePlayListsAsync(CreatePlayListRequest createPlayList)
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            var isPlaylistExists = await _repository.Get<PlayList>()
                .Where(x => x.UserId == user.UserId)
                .Where(x => EF.Functions.ILike(x.Name, createPlayList.Title))
                .AnyAsync();
            if (isPlaylistExists)
            {
                return Result<PlayListViewModel>.Failure(new Error("Плейлист с таким названием уже существует"));
            }
            var playList = new PlayList(createPlayList.Title, user.UserId.Value, ConstPlayListType.Created, []);
            _repository.Add(playList);
            await _repository.SaveChangesAsync();
            return new PlayListViewModel(playList.Id, playList.Name, 0, null, playList.Type.ToString(), true, true);
        }

        public async Task<Result<IReadOnlyList<PlayListViewModel>>> GetCurrentUserPlayListsAsync()
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            var result = await _repository.Get<PlayList>()
                         .Where(x => x.UserId == user.UserId.Value)
                         .Select(x => new
                         {
                             x.Id,
                             x.Name,
                             TracksCount = x.Tracks.Count,
                             x.Type
                         })
                         .AsAsyncEnumerable()
                         .Select(x =>
                         {
                             var canDelete = !(x.Type == ConstPlayListType.Liked || x.Type == ConstPlayListType.Upload);
                             return new PlayListViewModel(x.Id, x.Name, x.TracksCount, null, x.Type.ToString(), true, canDelete);
                         })
                         .ToListAsync();
            if (result.Count > 0)
                return result;
            if (result.Count == 0)
            {
                var playLists = await CreateDefaultUserPlayListsAsync(user.UserId.Value);
                return playLists;
            }
            return Result<IReadOnlyList<PlayListViewModel>>.Failure(new Error("Somethig went wrong"));
        }


        public async Task<Result<PlayListViewModel>> GetPlayListInfoAsync(Guid id)
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            var result = await _repository.Get<PlayList>()
                       .Where(x => x.Id == id)
                       .Select(x => new
                       {
                           x.Id,
                           x.Name,
                           TracksCount = x.Tracks.Count,
                           x.Type,
                           x.UserId
                       })
                       .AsAsyncEnumerable()
                       .Select(x =>
                       {
                           var canEdit = user.UserId == x.UserId;
                           var canDelete = user.UserId == x.UserId && !(x.Type == ConstPlayListType.Upload || x.Type == ConstPlayListType.Liked);
                           return new PlayListViewModel(x.Id, x.Name, x.TracksCount, null, x.Type.ToString(), canEdit, canDelete);
                       })
                       .FirstAsync();
            return result;
        }

        public async Task<Result<IReadOnlyList<TrackViewItem>>> GetPlayListTrackListAsync(Guid id, int page, int size)
        {
            using var fileStorage = _fileStorageFactory.CreateFileStorage();

            var user = await _currentUserService.GetCurrentUserAsync();

            var playlistTracks = _repository.Get<PlayListTrack>()
                .Where(x => x.PlayListId == id)
                .Select(x => new
                {
                    x.Track,
                    x.Track.Metadata,
                    x.Track.ThumbnailMetadata,
                    x.PlayList.Type,
                    x.CreatedAt,
                    IsLike = x.PlayList.Type == ConstPlayListType.Liked || _repository.Get<PlayListTrack>()
                    .Where(x => x.PlayList.UserId == user.UserId && x.PlayList.Type == ConstPlayListType.Liked)
                    .Where(a => a.TrackId == x.TrackId).Any(),
                    Artists = x.Track.ArtistTrackLinks.Select(artist => new { artist.Artist.Name, artist.ArtistId }).ToList()
                })
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToList();

            //var playlistTracks = await _repository.Get<PlayListTrack>()
            //    .Where(x => x.PlayListId == id)
            //    .Select(x => new
            //    {
            //        x.Track,
            //        x.Track.Metadata,
            //        x.Track.ThumbnailMetadata,
            //        x.PlayList.Type,
            //        IsLike = x.PlayList.Type == ConstPlayListType.Liked,
            //        Artists = x.Track.ArtistTrackLinks.Select(artist => new { artist.Artist.Name, artist.ArtistId })
            //    })
            //    .Skip((page - 1) * size)
            //    .Take(size)
            //    .ToListAsync();

            return await playlistTracks
                .ToAsyncEnumerable()
                .SelectAwait(async x =>
                new TrackViewItem(
                    x.Track.Id,
                    x.Track.Title,
                    await fileStorage.GetFileUrlAsync(x.Track.ThumbnailMetadata.Id, x.Track.ThumbnailMetadata.ObjectName),
                    x.Track.AlbumId,
                    new TrackFileInfo(await fileStorage.GetFileUrlAsync(x.Track.Metadata.Id, x.Track.Metadata.ObjectName), x.Track.Metadata.Duration),
                    [.. x.Artists.Select(artist => new Contract.Models.Artist.ArtistInfo(artist.ArtistId, artist.Name))],
                    x.IsLike
                    ))
                .ToListAsync();
        }

        public async Task<Result> RemovePlayListAsync(Guid id)
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            var playlist = await _repository.Get<PlayList>()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (playlist == null)
            {
                return Result.Failure(new Error("Плейлист не найден"));
            }
            if (playlist.UserId != user.UserId.Value)
            {
                return Result.Failure(new Error("Вы не можете удалить чужой плейлист"));
            }
            if (playlist.Type == ConstPlayListType.Liked || playlist.Type == ConstPlayListType.Upload)
            {
                return Result.Failure(new Error("Вы не можете удалить стандартный плейлист"));
            }

            _repository.Remove(playlist);
            await _repository.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> RemoveTrackFromPlayListAsync(Guid id, Guid trackId)
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            var playList = await _repository.Get<PlayList>()
                .FirstAsync(x => x.Id == id);
            if (user.UserId.Value != playList.UserId)
            {
                return Result.Failure(new Error("Вы не можете удалять треки не из своего плейлиста"));
            }
            using var transaction = await _repository.BeginTransactionAsync();

            playList.RemoveTrack(_repository, trackId);
            await _repository.SaveChangesAsync();
            if (playList.Type == ConstPlayListType.Upload)
            {
                await _trackService.RemoveTrackAsync(trackId);
            }
            await transaction.CommitAsync();
            return Result.Success();
        }
    }
}
