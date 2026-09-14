using Infrastructure.Models;

namespace Profile.Application.Services;
public interface IProfilePictureStore
{
    Task<Result<string>> UpdatePictureAsync(long profileId, FileMetadataModel fileMetadata, CancellationToken cancellationToken = default);
    Task<string> GetPictureUrlAsync(long profileId, CancellationToken cancellationToken = default);
}
