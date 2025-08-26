using Infrastructure.Services;
using Music.Domain.Entities;
using Music.Domain.Repositories;
using Shared.Services;
using Shared.Utils;

namespace Music.Persistence.Repositories
{
    internal class DefaultTempTrackMetadataRepository : ITempFileMetadataRepository
    {
        private readonly ICacheService _cacheService;

        public DefaultTempTrackMetadataRepository(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public async Task<Result> ClearTempMetadataAsync(Guid id)
        {
            await _cacheService.RemoveCachedDataAsync(new TempFileMetadataCacheKey(id));
            return Result.Success();
        }

        public async Task<Result<TrackMetadata>> CreateTempMetadataAsync(TrackMetadata trackMetadata, long ttlInMinutes = 120)
        {
            var key = new TempFileMetadataCacheKey(trackMetadata.Id);
            var existTempData = await GetTempTrackMetadataAsync(trackMetadata.Id);
            if (existTempData.IsSuccess)
            {
                return new Error("Duplicate entity");
            }
            await _cacheService.SetCachedDataAsync(key, trackMetadata, TimeSpan.FromMinutes(ttlInMinutes));
            return trackMetadata;
        }

        public async Task<Result<ThumbnailMetadata>> CreateTempMetadataAsync(ThumbnailMetadata thumbnailMetadata, long ttlInMinutes = 120)
        {

            var key = new TempFileMetadataCacheKey(thumbnailMetadata.Id);
            var existTempData = await GetTempTrackMetadataAsync(thumbnailMetadata.Id);
            if (existTempData.IsSuccess)
            {
                return new Error("Duplicate entity");
            }
            await _cacheService.SetCachedDataAsync(key, thumbnailMetadata, TimeSpan.FromMinutes(ttlInMinutes));
            return thumbnailMetadata;
        }

        public async Task<Result<ThumbnailMetadata>> GetTempThumbnailMetadataAsync(Guid id)
        {
            var key = new TempFileMetadataCacheKey(id);
            var result = await _cacheService.GetCachedDataAsync<ThumbnailMetadata>(key);
            if (result != null)
            {
                return result;
            }
            return new Error("Not found");
        }

        public async Task<Result<TrackMetadata>> GetTempTrackMetadataAsync(Guid id)
        {
            var key = new TempFileMetadataCacheKey(id);
            var result = await _cacheService.GetCachedDataAsync<TrackMetadata>(key);
            if (result != null)
            {
                return result;
            }
            return new Error("Not found");
        }

        public async Task<Result> UpdateTempMetadataAsync(TrackMetadata trackMetadata, long ttlInMinutes = 120)
        {

            var key = new TempFileMetadataCacheKey(trackMetadata.Id);
            var existTempData = await GetTempTrackMetadataAsync(trackMetadata.Id);
            if (existTempData.IsSuccess)
            {
                return Result.Failure(new Error("Not found"));
            }
            await _cacheService.SetCachedDataAsync(key, trackMetadata, TimeSpan.FromMinutes(ttlInMinutes));
            return Result.Success();
        }

        public async Task<Result> UpdateTempMetadataAsync(ThumbnailMetadata trackMetadata, long ttlInMinutes = 120)
        {
            var key = new TempFileMetadataCacheKey(trackMetadata.Id);
            var existTempData = await GetTempThumbnailMetadataAsync(trackMetadata.Id);
            if (existTempData.IsSuccess)
            {
                return Result.Failure(new Error("Not found"));
            }
            await _cacheService.SetCachedDataAsync(key, trackMetadata, TimeSpan.FromMinutes(ttlInMinutes));
            return Result.Success();
        }
    }

    file class TempFileMetadataCacheKey : ICacheKey
    {
        private const string Key = nameof(TempFileMetadataCacheKey);
        private readonly Guid id;

        public TempFileMetadataCacheKey(Guid id)
        {
            this.id = id;
        }

        public string GetKey() => $"{Key}:{id}";
    }
}
