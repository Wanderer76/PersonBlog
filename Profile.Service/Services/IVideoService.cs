using Blog.Domain.Entities;
using Blog.Service.Models.File;

namespace Blog.Service.Services
{
    public interface IVideoService
    {
        Task<VideoMetadata> GetOrCreateVideoMetadata(UploadVideoChunkModel uploadVideoChunk);
    }
}
