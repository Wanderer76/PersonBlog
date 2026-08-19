namespace Infrastructure.Services;

public interface IFileStorage : IDisposable
{
    Task<string> PutFileAsync(Guid bucketId, string objectName, Stream input);
    Task ReadFileAsync(Guid bucketId, string objectName, Stream output, CancellationToken cancellationToken = default);
    Task<string> GetFileUrlAsync(Guid bucketId, string objectName);
    Task RemoveFileAsync(Guid bucketId, string objectName);
    Task RemoveFilesByPrefixAsync(Guid bucketId, string prefix, CancellationToken cancellationToken = default);
    Task RemoveBucketAsync(string bucketId);
    Task CreateTempBucketAsync(Guid bucketId);
    Task<string> PutFileChunkAsync(Guid bucketId, Guid id, Stream input, ChunkUploadingInfo options);
    Task<long> ReadFileByChunksAsync(Guid bucketId, string objectName, long offset, long length, Stream output);
    IAsyncEnumerable<(string Objectname, IReadOnlyDictionary<string, string> Headers)> GetAllBucketObjects(Guid bucketId, ChunkUploadingInfo options);
}

public sealed record ChunkUploadingInfo(Guid FileId, long ChunkNumber);
