using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FileStorage.Service;

public sealed class DefaultFileStorageFactory(IServiceScopeFactory serviceScopeFactory) : IFileStorageFactory
{
    public IFileStorage CreateFileStorage()
    {
        var scope = serviceScopeFactory.CreateScope();

        try
        {
            var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
            return new ScopedFileStorage(scope, storage);
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    private sealed class ScopedFileStorage(IServiceScope scope, IFileStorage storage) : IFileStorage
    {
        private bool _disposed;

        public Task<string> PutFileAsync(Guid bucketId, string objectName, Stream input, CancellationToken cancellationToken = default) =>
            storage.PutFileAsync(bucketId, objectName, input, cancellationToken);

        public Task ReadFileAsync(Guid bucketId, string objectName, Stream output, CancellationToken cancellationToken = default) =>
            storage.ReadFileAsync(bucketId, objectName, output, cancellationToken);

        public Task<string> GetFileUrlAsync(Guid bucketId, string objectName, CancellationToken cancellationToken = default) =>
            storage.GetFileUrlAsync(bucketId, objectName, cancellationToken);

        public Task RemoveFileAsync(Guid bucketId, string objectName, CancellationToken cancellationToken = default) =>
            storage.RemoveFileAsync(bucketId, objectName, cancellationToken);

        public Task RemoveFilesByPrefixAsync(Guid bucketId, string prefix, CancellationToken cancellationToken = default) =>
            storage.RemoveFilesByPrefixAsync(bucketId, prefix, cancellationToken);

        public Task RemoveBucketAsync(Guid bucketId, CancellationToken cancellationToken = default) =>
            storage.RemoveBucketAsync(bucketId, cancellationToken);

        public Task CreateTempBucketAsync(Guid bucketId, CancellationToken cancellationToken = default) =>
            storage.CreateTempBucketAsync(bucketId, cancellationToken);

#pragma warning disable CS0618
        public Task<string> PutFileChunkAsync(
            Guid bucketId,
            Guid id,
            Stream input,
            ChunkUploadingInfo options,
            CancellationToken cancellationToken = default) =>
            storage.PutFileChunkAsync(bucketId, id, input, options, cancellationToken);

        public Task<long> ReadFileByChunksAsync(
            Guid bucketId,
            string objectName,
            long offset,
            long length,
            Stream output,
            CancellationToken cancellationToken = default) =>
            storage.ReadFileByChunksAsync(bucketId, objectName, offset, length, output, cancellationToken);

        public IAsyncEnumerable<(string ObjectName, IReadOnlyDictionary<string, string> Headers)> GetAllBucketObjects(
            Guid bucketId,
            ChunkUploadingInfo options,
            CancellationToken cancellationToken = default) =>
            storage.GetAllBucketObjects(bucketId, options, cancellationToken);
#pragma warning restore CS0618

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            scope.Dispose();
            _disposed = true;
        }
    }
}
