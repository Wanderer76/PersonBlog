using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Music.Contract.Models;
using Music.Contract.Models.Artist;
using Music.Contract.Models.Search;
using Music.Domain.Entities;
using Music.Domain.Services;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;

namespace Music.Service.Services
{
    internal class DefaultTrackSearchService : ITrackSearchService
    {
        private readonly IReadRepository<IMusicEntity> _repository;
        private readonly IFileStorageFactory _fileStorageFactory;

        public DefaultTrackSearchService(IReadRepository<IMusicEntity> repository, IFileStorageFactory fileStorageFactory)
        {
            _repository = repository;
            _fileStorageFactory = fileStorageFactory;
        }

        public async Task<PagedListViewModel<TrackViewItem>> GetTrackByFilterAsync(SearchFilter filter, int page, int size)
        {
            var count = await _repository.Get<Track>().CountAsync();
            var query = await _repository.Get<Track>()
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.ThumbnailMetadata,
                    x.Metadata,
                    x.AlbumId,
                    Artists = x.ArtistTrackLinks.Select(artist => new { artist.ArtistId, artist.Artist.Name }).ToList(),
                    x.CreatedAt
                })
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            using var fileStorage = _fileStorageFactory.CreateFileStorage();

            var items = await query
                .ToAsyncEnumerable()
                .SelectAwait(async item =>
                {
                    var data = new TrackViewItem(
                    item.Id,
                    item.Title,
                    item.ThumbnailMetadata == null ? null : await fileStorage.GetFileUrlAsync(item.ThumbnailMetadata.Id, item.ThumbnailMetadata.ObjectName),
                    item.AlbumId,
                    new TrackFileInfo(await fileStorage.GetFileUrlAsync(item.Metadata.Id, item.Metadata.ObjectName), item.Metadata.Duration),
                    item.Artists.Select(x => new ArtistInfo(x.ArtistId, x.Name)).ToList()
                    );
                    return data;
                })
                .ToListAsync();

            return new PagedListViewModel<TrackViewItem>((int)Math.Ceiling((double)count / size), size, items);
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
