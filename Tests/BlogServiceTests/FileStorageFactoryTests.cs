using FileStorage.Service;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace BlogServiceTests;

public sealed class FileStorageFactoryTests
{
    [Fact]
    public void DisposingStorageDisposesCreatedScope()
    {
        var tracker = new DisposalTracker();
        var services = new ServiceCollection();
        services.AddSingleton(tracker);
        services.AddScoped<ScopeDependency>();
        services.AddScoped<IFileStorage, StubFileStorage>();

        using var provider = services.BuildServiceProvider();
        var factory = new DefaultFileStorageFactory(provider.GetRequiredService<IServiceScopeFactory>());

        var storage = factory.CreateFileStorage();

        Assert.False(tracker.StorageDisposed);
        Assert.False(tracker.DependencyDisposed);

        storage.Dispose();

        Assert.True(tracker.StorageDisposed);
        Assert.True(tracker.DependencyDisposed);
    }

    [Fact]
    public void ResolutionFailureDisposesCreatedScope()
    {
        var tracker = new DisposalTracker();
        var services = new ServiceCollection();
        services.AddSingleton(tracker);
        services.AddScoped<ScopeDependency>();
        services.AddScoped<IFileStorage>(serviceProvider =>
        {
            _ = serviceProvider.GetRequiredService<ScopeDependency>();
            throw new InvalidOperationException("Storage construction failed");
        });

        using var provider = services.BuildServiceProvider();
        var factory = new DefaultFileStorageFactory(provider.GetRequiredService<IServiceScopeFactory>());

        Assert.Throws<InvalidOperationException>(() => factory.CreateFileStorage());
        Assert.True(tracker.DependencyDisposed);
    }

    private sealed class DisposalTracker
    {
        public bool StorageDisposed { get; set; }
        public bool DependencyDisposed { get; set; }
    }

    private sealed class ScopeDependency(DisposalTracker tracker) : IDisposable
    {
        public void Dispose() => tracker.DependencyDisposed = true;
    }

    private sealed class StubFileStorage(ScopeDependency dependency, DisposalTracker tracker) : IFileStorage
    {
        public Task<string> PutFileAsync(Guid bucketId, string objectName, Stream input, CancellationToken cancellationToken = default) =>
            Task.FromResult(objectName);

        public Task ReadFileAsync(Guid bucketId, string objectName, Stream output, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<string> GetFileUrlAsync(Guid bucketId, string objectName, CancellationToken cancellationToken = default) =>
            Task.FromResult(objectName);

        public Task RemoveFileAsync(Guid bucketId, string objectName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveFilesByPrefixAsync(Guid bucketId, string prefix, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveBucketAsync(Guid bucketId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task CreateTempBucketAsync(Guid bucketId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<string> PutFileChunkAsync(
            Guid bucketId,
            Guid id,
            Stream input,
            ChunkUploadingInfo options,
            CancellationToken cancellationToken = default) => Task.FromResult(id.ToString());

        public Task<long> ReadFileByChunksAsync(
            Guid bucketId,
            string objectName,
            long offset,
            long length,
            Stream output,
            CancellationToken cancellationToken = default) => Task.FromResult(length);

        public async IAsyncEnumerable<(string ObjectName, IReadOnlyDictionary<string, string> Headers)> GetAllBucketObjects(
            Guid bucketId,
            ChunkUploadingInfo options,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public void Dispose()
        {
            _ = dependency;
            tracker.StorageDisposed = true;
        }
    }
}
