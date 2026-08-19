namespace Infrastructure.Services;

public interface IFileStorage : IDisposable
{
    Task<string> PutFileAsync(Guid bucketId, string objectName, Stream input, CancellationToken cancellationToken = default);
    Task ReadFileAsync(Guid bucketId, string objectName, Stream output, CancellationToken cancellationToken = default);
    Task<string> GetFileUrlAsync(Guid bucketId, string objectName, CancellationToken cancellationToken = default);
    Task RemoveFileAsync(Guid bucketId, string objectName, CancellationToken cancellationToken = default);
    Task RemoveFilesByPrefixAsync(Guid bucketId, string prefix, CancellationToken cancellationToken = default);
    Task RemoveBucketAsync(Guid bucketId, CancellationToken cancellationToken = default);
    Task CreateTempBucketAsync(Guid bucketId, CancellationToken cancellationToken = default);

    [Obsolete($"Используйте {nameof(IMultipartFileUpload)}")]
    Task<string> PutFileChunkAsync(
        Guid bucketId,
        Guid id,
        Stream input,
        ChunkUploadingInfo options,
        CancellationToken cancellationToken = default);

    [Obsolete($"Используйте {nameof(IMultipartFileUpload)}")]
    Task<long> ReadFileByChunksAsync(
        Guid bucketId,
        string objectName,
        long offset,
        long length,
        Stream output,
        CancellationToken cancellationToken = default);

    [Obsolete($"Используйте {nameof(IMultipartFileUpload)}")]
    IAsyncEnumerable<(string ObjectName, IReadOnlyDictionary<string, string> Headers)> GetAllBucketObjects(
        Guid bucketId,
        ChunkUploadingInfo options,
        CancellationToken cancellationToken = default);
}

public sealed record ChunkUploadingInfo(Guid FileId, long ChunkNumber);
