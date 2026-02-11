using Blog.Contracts.Models.File;
using Shared.Models;
using Shared.Utils;

namespace Blog.Contracts.Services;
public interface IVideoService
{
    Task<Result<FileMetadata>> GetOrCreateVideoMetadata(UploadVideoChunkModel uploadVideoChunk);
    Task<Result<UploadVideoProgress>> CreateUploadVideoMetadata(CreateUploadVideoProgressRequest uploadVideoChunk);
    Task<Result<UploadVideoProgress>> GetUploadVideoMetadata(Guid fileId);
    Task<Result> CreateFileMetadataAsync(InitiateUploadRequest initiateUploadRequest);
}
public class InitiateUploadRequest
{
    public Guid PostId { get; set; }
    public string ObjectName { get; set; } = null!;
    public long Size { get; set; }
    public string ContentType { get; set; } = null!;
    public string FileExtension { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public double Duration { get; set; }
}