using Music.Contract.Models;
using Shared.Models;
using Shared.Utils;

namespace Music.Domain.Services;

public interface ITrackService
{
    Task<Result> CreateTrackAsync(TrackCreateRequest createRequest);
    Task<Result<Guid>> UploadTrackFileAsync(UploadTrackFile createRequest);
    Task<Result<Guid>> UploadThumbnailFileAsync(UploadThumbnailFile createRequest);
    Task<Result<PagedListViewModel<TrackViewItem>>> GetTrackPagedListAsync(int page, int size);
}
