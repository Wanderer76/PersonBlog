using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Music.Contract.Models;
using Music.Contract.Models.Artist;
using Music.Contract.Models.Search;
using Music.Domain.Entities;
using Music.Domain.Services;
using MusicRecommendation.Services;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;

namespace Music.Service.Services
{
    internal class DefaultTrackSearchService : ITrackSearchService
    {
        private readonly IReadRepository<IMusicEntity> _repository;
        private readonly IFileStorageFactory _fileStorageFactory;
        private readonly ICurrentUserService _currentUserService;
        private readonly MusicRecommendationHttpClient _musicRecommendations;

        public DefaultTrackSearchService(IReadRepository<IMusicEntity> repository, IFileStorageFactory fileStorageFactory, ICurrentUserService currentUserService, MusicRecommendationHttpClient musicRecommendations)
        {
            _repository = repository;
            _fileStorageFactory = fileStorageFactory;
            _currentUserService = currentUserService;
            _musicRecommendations = musicRecommendations;
        }

        public async Task<PagedListViewModel<TrackViewItem>> GetRecommendationTracksAsync(int page, int size)
        {
            var recommendations = await _musicRecommendations.GetRecommendationFotUser(page, size);
            if (recommendations.IsSuccess)
            {
                return await GetTrackByFilterAsync(new SearchFilter { Ids = recommendations.Value }, page, size);
            }
            return new(0, 0, 0, []);
        }

        public async Task<PagedListViewModel<TrackViewItem>> GetTrackByFilterAsync(SearchFilter filter, int page, int size)
        {
            var user = await _currentUserService.GetCurrentUserAsync();

            var query = _repository.Get<Track>();

            if (!string.IsNullOrWhiteSpace(filter.Title))
            {
                query = query.Where(x => EF.Functions.ILike(x.Title, $"%{filter.Title}%"));
            }

            if (filter.Ids != null && filter.Ids.Any())
            {
                query = query.Where(x => filter.Ids.Contains(x.Id));
            }

            var count = await query.CountAsync();
            var tracks = await query
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.ThumbnailMetadata,
                    x.Metadata,
                    x.AlbumId,
                    IsLike = _repository.Get<PlayListTrack>()
                    .Where(x => x.PlayList.UserId == user.UserId && x.PlayList.Type == ConstPlayListType.Liked)
                    .Where(a => a.TrackId == x.Id).Any(),
                    Artists = x.ArtistTrackLinks.Select(artist => new { artist.ArtistId, artist.Artist.Name }).ToList(),
                    x.CreatedAt
                })
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            using var fileStorage = _fileStorageFactory.CreateFileStorage();

            var items = await tracks
                .ToAsyncEnumerable()
                .SelectAwait(async item =>
                {
                    var data = new TrackViewItem(
                    item.Id,
                    item.Title,
                    item.ThumbnailMetadata == null ? null : await fileStorage.GetFileUrlAsync(item.ThumbnailMetadata.Id, item.ThumbnailMetadata.ObjectName),
                    item.AlbumId,
                    new TrackFileInfo(await fileStorage.GetFileUrlAsync(item.Metadata.Id, item.Metadata.ObjectName), item.Metadata.Duration),
                    item.Artists.Select(x => new ArtistInfo(x.ArtistId, x.Name)).ToList(),
                    item.IsLike
                    );
                    return data;
                })
                .ToListAsync();

            return PagedListViewModel.Create(items, size, count);
        }
    }
}
file class TrackViewItemCacheKey : ICacheKey
{
    public const string Key = nameof(TrackViewItemCacheKey);
    private readonly Guid id;

    public TrackViewItemCacheKey(Guid id)
    {
        this.id = id;
    }

    public string GetKey() => $"{Key}:{id}";
}
