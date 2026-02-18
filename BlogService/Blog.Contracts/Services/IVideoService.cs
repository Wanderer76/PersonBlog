using Blog.Contracts.Models.File;
using Shared.Models;
using Shared.Utils;

namespace Blog.Contracts.Services;
public interface IVideoService
{
    [Obsolete("", true)]
    Task<Result<BaseFileMetadataEntity>> GetOrCreateVideoMetadata(UploadVideoChunkModel uploadVideoChunk);
    [Obsolete("", true)]

    Task<Result<UploadVideoProgress>> CreateUploadVideoMetadata(CreateUploadVideoProgressRequest uploadVideoChunk);
    [Obsolete("", true)]
    Task<Result<UploadVideoProgress>> GetUploadVideoMetadata(Guid fileId);
    Task<Result> InitVideoUploadAsync(InitiateUploadRequest initiateUploadRequest);
    Task CompleteUploadAsync(Guid postId);

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