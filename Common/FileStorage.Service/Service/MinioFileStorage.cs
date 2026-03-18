using FileStorage.Service.Models;
using Infrastructure.Services;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.ILM;
using Shared.Services;

namespace FileStorage.Service.Service;

internal class MinioFileStorage : IFileStorage
{
    private const int Expiry = 604800;
    private readonly IMinioClient _client;

    public MinioFileStorage(IOptions<FileStorageOptions> options)
    {
        _client = new MinioClient()
            .WithEndpoint(options.Value.Endpoint)
            .WithCredentials(options.Value.AccessKey, options.Value.SecretKey)
            .WithSSL(false)
            .Build();
    }

    public async Task ReadFileAsync(Guid bucketId, string objectName, Stream output)
    {
        await _client.GetObjectAsync(new GetObjectArgs()
            .WithBucket(bucketId.ToString())
            .WithObject(objectName)
            .WithCallbackStream(stream => stream.CopyTo(output)));
    }

    public async Task RemoveFileAsync(Guid bucketId, string objectName)
    {
        await _client.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(bucketId.ToString())
            .WithObject(objectName));
    }

    private string GeFileNameFromId(Guid fileId, VideoResolution videoResolution)
    {
        return $"video-{fileId}-{(int)videoResolution}";
    }

    public async Task<long> ReadFileByChunksAsync(Guid bucketId, string objectName, long offset, long length, Stream buffer)
    {
        await _client.GetObjectAsync(new GetObjectArgs()
             .WithBucket(bucketId.ToString())
             .WithObject(objectName)
             .WithOffsetAndLength(offset, length)
             .WithCallbackStream(stream =>
             {
                 stream.CopyTo(buffer);
             }));
        return buffer.Length;
    }

    public async Task<string> GetFileUrlAsync(Guid bucketId, string objectName)
    {
        var result = await _client.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                         .WithBucket(bucketId.ToString())
                         .WithExpiry(Expiry)
                         .WithObject(objectName));
        return result;
    }

    public async Task<string> PutFileChunkAsync(Guid bucketId, Guid id, Stream input, ChunkUploadingInfo options)
    {
        await CreateBucketIfNotExistAsync(bucketId);

        var result = await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucketId.ToString())
            .WithObject(GeFileNameFromId(id, VideoResolution.Original))
            .WithObjectSize(input.Length)
            .WithHeaders(new Dictionary<string, string>
            {
                { "FileId", options.FileId.ToString() },
                { "ChunkNumber", options.ChunkNumber.ToString() },
                { "ChunkSize", input.Length.ToString() }
            })
            .WithStreamData(input));

        return result.ObjectName;
    }

    public async Task CreateBucketIfNotExistAsync(Guid bucketId)
    {
        if (!await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketId.ToString())))
        {
            await _client.MakeBucketAsync(new MakeBucketArgs()
                .WithBucket(bucketId.ToString()));
        }
    }

    public async IAsyncEnumerable<(string Objectname, IReadOnlyDictionary<string, string> Headers)> GetAllBucketObjects(Guid bucketId, ChunkUploadingInfo options)
    {
        var objects = _client.ListObjectsEnumAsync(new ListObjectsArgs()
            .WithBucket(bucketId.ToString())
            .WithIncludeUserMetadata(true)
            .WithHeaders(new Dictionary<string, string>
                      {
                          { "FileId", options.FileId.ToString() },
                      }));
        await foreach (var item in objects)
        {
            yield return (item.Key, item.UserMetadata.AsReadOnly());
        }
    }

    public async Task<string> PutFileAsync(Guid bucketId, string objectName, Stream input)
    {
        await CreateBucketIfNotExistAsync(bucketId);

        var result = await _client.PutObjectAsync(
                      new PutObjectArgs()
                      .WithBucket(bucketId.ToString())
                      .WithObject(objectName)
                      .WithObjectSize(input.Length)
                      .WithStreamData(input)
                      );

        return result.ObjectName;
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public async Task RemoveBucketAsync(string bucketId)
    {
        await _client.RemoveBucketAsync(new RemoveBucketArgs().WithBucket(bucketId));
    }

    public async Task CreateTempBucketAsync(Guid bucketId)
    {
        await CreateBucketIfNotExistAsync(bucketId);
        await SetBucketLifecycleAsync(bucketId.ToString(), DateTimeService.Now().AddDays(1).DateTime);
    }

    private async Task SetBucketLifecycleAsync(string bucketName, DateTime expirationDate)
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
                    .WithLifecycleConfiguration(lifecycleConfig));
        }
        catch (Exception ex)
        {
        }
    }
}
