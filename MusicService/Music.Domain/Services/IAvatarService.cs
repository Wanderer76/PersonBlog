using Shared.Models;
using Shared.Utils;

namespace Music.Domain.Services
{
    public interface IAvatarService
    {
        Task<Result<Guid>> UploadAvatarAsync(BaseFileMetadataEntity fileMetadata,Stream stream);
    }
}
