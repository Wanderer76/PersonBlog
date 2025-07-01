using Blog.Domain.Entities;
using Blog.Service.Models.File;
using Shared.Utils;

namespace Blog.Service.Services
{
    public interface IVideoService
    {
        Task<Result<VideoMetadata>> GetOrCreateVideoMetadata(UploadVideoChunkModel uploadVideoChunk);
        Task<Result<UploadVideoProgress>> CreateUploadVideoMetadata(CreateUploadVideoProgressRequest uploadVideoChunk);
        Task<Result<UploadVideoProgress>> GetUploadVideoMetadata(Guid fileId);
    }
}
