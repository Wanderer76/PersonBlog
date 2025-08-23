using Music.Domain.Entities;
using Shared.Utils;

namespace Music.Domain.Repositories;

public interface ITempTrackMetadataRepository
{
    Task<Result<TrackMetadata>> CreateTempMetadataAsync(TrackMetadata trackMetadata, long ttlInMinutes = 120);
    Task<Result> UpdateTempMetadataAsync(TrackMetadata trackMetadata, long ttlInMinutes = 120);
    Task<Result<TrackMetadata>> GetTempMetadataAsync(Guid id);
    Task<Result> ClearTempMetadataAsync(Guid id);
}
