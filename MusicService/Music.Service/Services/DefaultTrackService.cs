using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Music.Contract.Models;
using Music.Contract.Models.Artist;
using Music.Domain.Entities;
using Music.Domain.Repositories;
using Music.Domain.Services;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace Music.Service.Services
{
    internal class DefaultTrackService : ITrackService
    {
        private readonly IReadWriteRepository<IMusicEntity> _repository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileStorageFactory _fileStorageFactory;
        private readonly ITempFileMetadataRepository _tempTrackMetadataRepository;

        public DefaultTrackService(IReadWriteRepository<IMusicEntity> repository, ICurrentUserService currentUserService, IFileStorageFactory fileStorageFactory, ITempFileMetadataRepository tempTrackMetadataRepository)
        {
            _repository = repository;
            _currentUserService = currentUserService;
            _fileStorageFactory = fileStorageFactory;
            _tempTrackMetadataRepository = tempTrackMetadataRepository;
        }

        public async Task<Result> CreateTrackAsync(TrackCreateRequest createRequest)
        {
            if (createRequest.ArtistId == null && createRequest.ArtistName == null)
            {
                return Result.Failure(new Error("Artist not found"));
            }

            var currentUser = await _currentUserService.GetCurrentUserAsync();
            var artistQuery = _repository.Get<Artist>();
            if (createRequest.ArtistId.HasValue)
            {
                artistQuery = artistQuery.Where(x => x.Id == createRequest.ArtistId.Value);
            }
            else
            {
                artistQuery = artistQuery.Where(x => x.Name == createRequest.ArtistName);
            }
            var artist = await artistQuery.FirstOrDefaultAsync();

            if (artist == null && !string.IsNullOrWhiteSpace(createRequest.ArtistName))
            {
                artist = Artist.CreateExternal(createRequest.ArtistName, null);
                _repository.Add(artist);
            }
            if ((artist == null && string.IsNullOrWhiteSpace(createRequest.ArtistName)))
            {
                return Result.Failure(new Error("Artist not found"));
            }

            var tempTrackFile = await _tempTrackMetadataRepository.GetTempTrackMetadataAsync(createRequest.TrackFileId);
            if (tempTrackFile.IsFailure)
            {
                return Result.Failure(new Error("File not found"));
            }

            ThumbnailMetadata? thumbnailMetadata = null;

            if (createRequest.ThumbnailId.HasValue)
            {
                var thumbnail = await _tempTrackMetadataRepository.GetTempThumbnailMetadataAsync(createRequest.ThumbnailId.Value);

                if (thumbnail.IsFailure)
                {
                    return Result.Failure(new Error("Thumbnail  not found"));
                }
                thumbnailMetadata = thumbnail.Value;
            }

            var track = new Track(
                createRequest.Name, 
                currentUser.UserId.Value, 
                createRequest.AlbumId, 
                createRequest.PostId, 
                createRequest.ThumbnailId, 
                createRequest.TrackFileId, 
                createRequest.Year);

            track.AddArtist(artist.Id);
            var file = tempTrackFile.Value;
            _repository.Attach(file);
            file.TrackId = track.Id;
            if (thumbnailMetadata != null)
            {
                thumbnailMetadata.TrackId = track.Id;
                _repository.Attach(thumbnailMetadata);
            }
            _repository.Add(track);

            await _repository.SaveChangesAsync();
            await _tempTrackMetadataRepository.ClearTempMetadataAsync(createRequest.TrackFileId);
            if (createRequest.ThumbnailId.HasValue)
            {
                await _tempTrackMetadataRepository.ClearTempMetadataAsync(createRequest.ThumbnailId.Value);
            }
            return Result.Success();
        }


        public async Task<Result<PagedListViewModel<TrackViewItem>>> GetTrackPagedListAsync(int page, int size)
        {
            var totalCount = await _repository.Get<Track>().CountAsync();
            var trackList = await _repository.Get<Track>()
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Metadata,
                    x.AlbumId,
                    x.ThumbnailMetadata,
                    x.CreatedAt,
                    Aritsts = x.ArtistTrackLinks.Select(artist => new
                    {
                        artist.ArtistId,
                        artist.Artist.Name
                    })
                    .ToList(),
                })
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            using var fileStorage = _fileStorageFactory.CreateFileStorage();

            var result = await trackList.ToAsyncEnumerable().SelectAwait(async track =>
            {
                var thumbnail = track.ThumbnailMetadata != null
                ? await fileStorage.GetFileUrlAsync(track.ThumbnailMetadata.Id, track.ThumbnailMetadata.ObjectName)
                : null;
                var song = await fileStorage.GetFileUrlAsync(track.Metadata.Id, track.Metadata.ObjectName);
                var trackFileInfo = new TrackFileInfo(song, track.Metadata.Duration);
                var artists = track.Aritsts.Select(x => new ArtistInfo(x.ArtistId, x.Name)).ToList();
                return new TrackViewItem(track.Id, track.Title, thumbnail, track.AlbumId, trackFileInfo, artists);
            }).ToListAsync();

            return new PagedListViewModel<TrackViewItem>(totalCount / size, size, result);
        }

        public async Task<Result<Guid>> UploadThumbnailFileAsync(UploadThumbnailFile createRequest)
        {
            var trackMetadata = new ThumbnailMetadata(
                GuidService.GetNewGuid(),
                createRequest.Name,
                createRequest.Name,
                createRequest.Length,
                createRequest.ContentType,
                createRequest.ObjectName,
                Guid.Empty);

            await _tempTrackMetadataRepository.CreateTempMetadataAsync(trackMetadata);
            using var fileStorage = _fileStorageFactory.CreateFileStorage();
            await fileStorage.PutTempFileAsync(trackMetadata.Id, trackMetadata.ObjectName, createRequest.Stream);
            return trackMetadata.Id;
        }

        public async Task<Result<TrackFileMetadata>> UploadTrackFileAsync(UploadTrackFile createRequest, AudioFileMetadata audioFileMetadata)
        {
            var trackMetadata = new TrackMetadata(
                GuidService.GetNewGuid(),
                audioFileMetadata.OriginalFileName,
                Path.GetExtension(audioFileMetadata.OriginalFileName),
                createRequest.Length,
                createRequest.ContentType,
                createRequest.ObjectName,
                Guid.Empty,
                audioFileMetadata.Duration);

            var artistId = audioFileMetadata.Artist == null
                ? null
                : await _repository.Get<Artist>()
                .Where(x => x.Name == audioFileMetadata.Artist)
                .FirstOrDefaultAsync();


            await _tempTrackMetadataRepository.CreateTempMetadataAsync(trackMetadata);
            using var fileStorage = _fileStorageFactory.CreateFileStorage();
            await fileStorage.PutFileAsync(trackMetadata.Id, trackMetadata.ObjectName, createRequest.Stream);
            return new TrackFileMetadata
            {
                TrackFileId = trackMetadata.Id,
                OriginalFileName = audioFileMetadata.OriginalFileName,
                Album=audioFileMetadata.Album,
                Artist = audioFileMetadata.Artist,
                Bitrate = audioFileMetadata.Bitrate,
                CoverBase64 = audioFileMetadata.CoverBase64,
                CoverMimeType = audioFileMetadata.CoverMimeType,
                Duration = audioFileMetadata.Duration,
                FileSize = audioFileMetadata.FileSize,
                Genre = audioFileMetadata.Genre,
                HasCover = audioFileMetadata.HasCover,
                Title = audioFileMetadata.Title,
                Year = audioFileMetadata.Year,
                ArtistId = artistId?.Id
            };
        }
    }
}
