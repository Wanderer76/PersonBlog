using Profile.Domain.Entities;
using Profile.Service.Models.File;

namespace Profile.Service.Services
{
    public interface IVideoService
    {
        Task<VideoMetadata> GetOrCreateVideoMetadata(UploadVideoChunkModel uploadVideoChunk);
    }
}
