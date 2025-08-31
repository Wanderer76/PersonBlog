using Infrastructure.Models;
using Music.Contract.Models;
using Shared.Models;
using Shared.Utils;

namespace Music.Domain.Services;

public interface ITrackService
{
    Task<Result> CreateTrackAsync(TrackCreateRequest createRequest);
    Task<Result> RemoveTrackAsync(Guid id);
    Task<Result<TrackFileMetadata>> UploadTrackFileAsync(UploadTrackFile createRequest, AudioFileMetadata audioFileMetadata);
    Task<Result<Guid>> UploadThumbnailFileAsync(UploadThumbnailFile createRequest);
    Task<Result<PagedListViewModel<TrackViewItem>>> GetTrackPagedListAsync(int page, int size);
}

public class TrackFileMetadata : AudioFileMetadata
{
    public Guid TrackFileId { get; set; }
    public Guid? ArtistId {  get; set; }
}