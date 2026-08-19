using FileStorage.Service.Models;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.ILM;
using Shared.Services;
using System.Runtime.CompilerServices;

namespace FileStorage.Service.Service;

internal class MinioFileStorage : IFileStorage
{
    private readonly IMinioClient _client;
    private readonly ILogger<MinioFileStorage> _logger;
    private readonly int _presignedUrlExpirySeconds;

    public MinioFileStorage(IOptions<FileStorageOptions> options, ILogger<MinioFileStorage> logger)
    {
        _logger = logger;
        _presignedUrlExpirySeconds = options.Value.PresignedUrlExpirySeconds;
        _client = new MinioClient()
            .WithEndpoint(options.Value.Endpoint)
            .WithCredentials(options.Value.AccessKey, options.Value.SecretKey)
            .WithSSL(false)
            .Build();
    }

    public async Task ReadFileAsync(
        Guid bucketId,
        string objectName,
        Stream output,
        CancellationToken cancellationToken = default)
    {
        await _client.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(bucketId.ToString())
                .WithObject(objectName)
                .WithCallbackStream((stream, callbackCancellationToken) =>
                    stream.CopyToAsync(output, callbackCancellationToken)),
            cancellationToken);
    }

    public async Task RemoveFileAsync(Guid bucketId, string objectName, CancellationToken cancellationToken = default)
    {
        await _client.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(bucketId.ToString())
            .WithObject(objectName), cancellationToken);
    }

    public async Task RemoveFilesByPrefixAsync(
        Guid bucketId,
        string prefix,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        var objects = _client.ListObjectsEnumAsync(new ListObjectsArgs()
            .WithBucket(bucketId.ToString())
            .WithPrefix(prefix)
            .WithRecursive(true), cancellationToken);

        await foreach (var item in objects.WithCancellation(cancellationToken))
        {
            await _client.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(bucketId.ToString())
                .WithObject(item.Key), cancellationToken);
        }
    }

    private static string GetFileNameFromId(Guid fileId, VideoResolution videoResolution)
    {
        return $"video-{fileId}-{(int)videoResolution}";
    }

    public async Task<long> ReadFileByChunksAsync(
        Guid bucketId,
        string objectName,
        long offset,
        long length,
        Stream buffer,
        CancellationToken cancellationToken = default)
    {
        await _client.GetObjectAsync(new GetObjectArgs()
             .WithBucket(bucketId.ToString())
             .WithObject(objectName)
             .WithOffsetAndLength(offset, length)
             .WithCallbackStream((stream, callbackCancellationToken) =>
                 stream.CopyToAsync(buffer, callbackCancellationToken)), cancellationToken);
        return length;
    }

    public async Task<string> GetFileUrlAsync(
        Guid bucketId,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await _client.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                         .WithBucket(bucketId.ToString())
                         .WithExpiry(_presignedUrlExpirySeconds)
                         .WithObject(objectName));
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    public async Task<string> PutFileChunkAsync(
        Guid bucketId,
        Guid id,
        Stream input,
        ChunkUploadingInfo options,
        CancellationToken cancellationToken = default)
    {
        await CreateBucketIfNotExistAsync(bucketId, cancellationToken);

        var objectSize = GetRemainingLength(input);

        var result = await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucketId.ToString())
            .WithObject(GetFileNameFromId(id, VideoResolution.Original))
            .WithObjectSize(objectSize)
            .WithHeaders(new Dictionary<string, string>
            {
                { "FileId", options.FileId.ToString() },
                { "ChunkNumber", options.ChunkNumber.ToString() },
                { "ChunkSize", objectSize.ToString() }
            })
            .WithStreamData(input), cancellationToken);

        return result.ObjectName;
    }

    private async Task CreateBucketIfNotExistAsync(Guid bucketId, CancellationToken cancellationToken)
    {
        var bucketName = bucketId.ToString();
        var bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
        if (await _client.BucketExistsAsync(bucketExistsArgs, cancellationToken))
        {
            return;
        }

        try
        {
            await _client.MakeBucketAsync(new MakeBucketArgs()
                .WithBucket(bucketName), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            if (!await _client.BucketExistsAsync(bucketExistsArgs, cancellationToken))
            {
                throw;
            }
        }
    }

    public async IAsyncEnumerable<(string ObjectName, IReadOnlyDictionary<string, string> Headers)> GetAllBucketObjects(
        Guid bucketId,
        ChunkUploadingInfo options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var objects = _client.ListObjectsEnumAsync(new ListObjectsArgs()
            .WithBucket(bucketId.ToString())
            .WithIncludeUserMetadata(true), cancellationToken);
        await foreach (var item in objects.WithCancellation(cancellationToken))
        {
            var belongsToFile = item.UserMetadata.Any(metadata =>
                string.Equals(metadata.Key, "FileId", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(metadata.Value, options.FileId.ToString(), StringComparison.OrdinalIgnoreCase));
            if (!belongsToFile)
            {
                continue;
            }

            yield return (item.Key, item.UserMetadata.AsReadOnly());
        }
    }

    public async Task<string> PutFileAsync(
        Guid bucketId,
        string objectName,
        Stream input,
        CancellationToken cancellationToken = default)
    {
        await CreateBucketIfNotExistAsync(bucketId, cancellationToken);

        var result = await _client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(bucketId.ToString())
                .WithObject(objectName)
                .WithObjectSize(GetRemainingLength(input))
                .WithStreamData(input),
            cancellationToken);

        return result.ObjectName;
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public async Task RemoveBucketAsync(Guid bucketId, CancellationToken cancellationToken = default)
    {
        await _client.RemoveBucketAsync(
            new RemoveBucketArgs().WithBucket(bucketId.ToString()),
            cancellationToken);
    }

    public async Task CreateTempBucketAsync(Guid bucketId, CancellationToken cancellationToken = default)
    {
        await CreateBucketIfNotExistAsync(bucketId, cancellationToken);
        await SetBucketLifecycleAsync(
            bucketId.ToString(),
            DateTimeService.Now().AddDays(1).DateTime,
            cancellationToken);
    }

    private async Task SetBucketLifecycleAsync(
        string bucketName,
        DateTime expirationDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var lifecycleConfig = new LifecycleConfiguration(
            [
                new LifecycleRule
                {
                    ID = "auto-delete-temp-objects",
                    Status = "Enabled",
                    Filter = new RuleFilter(),
                    Expiration = new Expiration(expirationDate)
                }
            ]
            );

            await _client.SetBucketLifecycleAsync(
                new SetBucketLifecycleArgs()
                    .WithBucket(bucketName)
                    .WithLifecycleConfiguration(lifecycleConfig), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure lifecycle for temporary bucket {BucketName}", bucketName);
            throw;
        }
    }

    private static long GetRemainingLength(Stream input) =>
        input.CanSeek ? input.Length - input.Position : -1;
}
