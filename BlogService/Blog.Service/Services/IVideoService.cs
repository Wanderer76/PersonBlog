using Blog.Domain.Entities;
using Blog.Service.Models.File;
using Blog.Service.Services.Implementation;
using Shared.Utils;

namespace Blog.Service.Services
{
    public interface IVideoService
    {
        Task<Result<VideoFile>> GetOrCreateVideoMetadata(UploadVideoChunkModel uploadVideoChunk);
        Task<Result<UploadVideoProgress>> CreateUploadVideoMetadata(CreateUploadVideoProgressRequest uploadVideoChunk);
        Task<Result<UploadVideoProgress>> GetUploadVideoMetadata(Guid fileId);
        Task<Result> CreateFileMetadataAsync(InitiateUploadRequest initiateUploadRequest);
    }
}
