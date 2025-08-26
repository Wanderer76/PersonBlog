using Music.Domain.Entities;
using Shared.Utils;

namespace Music.Domain.Repositories;

public interface ITempFileMetadataRepository
{
    Task<Result<TrackMetadata>> CreateTempMetadataAsync(TrackMetadata trackMetadata, long ttlInMinutes = 120);
    Task<Result<ThumbnailMetadata>> CreateTempMetadataAsync(ThumbnailMetadata thumbnailMetadata, long ttlInMinutes = 120);
    Task<Result> UpdateTempMetadataAsync(TrackMetadata trackMetadata, long ttlInMinutes = 120);
    Task<Result> UpdateTempMetadataAsync(ThumbnailMetadata trackMetadata, long ttlInMinutes = 120);
    Task<Result<TrackMetadata>> GetTempTrackMetadataAsync(Guid id);
    Task<Result<ThumbnailMetadata>> GetTempThumbnailMetadataAsync(Guid id);
    Task<Result> ClearTempMetadataAsync(Guid id);
}
