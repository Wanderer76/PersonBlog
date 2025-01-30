using FileStorage.Service.Models;
using Minio;
using Minio.ApiEndpoints;
using Minio.DataModel;

namespace FileStorage.Service.Service
{
    internal class MinioFileStorage : IFileStorage
    {
        private readonly IMinioClient _client;

        public MinioFileStorage(FileStorageOptions options)
        {
            _client = new MinioClient()
                .WithEndpoint(options.Endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey)
                .WithSSL(false)
                .Build();
        }

        public async Task ReadFileAsync(Guid bucketId, string objectName, Stream output)
        {
            await _client.GetObjectAsync(new Minio.DataModel.Args.GetObjectArgs()
                .WithBucket(bucketId.ToString())
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(output)));
        }

        public async Task RemoveFileAsync(Guid bucketId, string objectName)
        {
            await _client.RemoveObjectAsync(new Minio.DataModel.Args.RemoveObjectArgs()
                .WithBucket(bucketId.ToString())
                .WithObject(objectName));
        }

        private string GeFileNameFromId(Guid fileId)
        {
            return $"video-{fileId}";
        }
        private string GeFileNameFromId(Guid fileId, VideoResolution videoResolution)
        {
            return $"video-{fileId}-{(int)videoResolution}";
        }

        public async Task<long> ReadFileByChunksAsync(Guid bucketId, string objectName, long offset, long length, Stream buffer)
        {
            await _client.GetObjectAsync(new Minio.DataModel.Args.GetObjectArgs()
                 .WithBucket(bucketId.ToString())
                 .WithObject(objectName)
                 .WithOffsetAndLength(offset, length)
                 .WithCallbackStream(stream =>
                 {
                     stream.CopyTo(buffer);
                 }));
            return buffer.Length;
        }

        public async Task<string> PutFileWithResolutionAsync(Guid bucketId, Guid fileId, Stream input, VideoResolution resolution = VideoResolution.Original)
        {
            await CreateBucketIfNotExistAsync(bucketId);

            var result = await _client.PutObjectAsync(
                          new Minio.DataModel.Args.PutObjectArgs()
                          .WithBucket(bucketId.ToString())
                          .WithObject(GeFileNameFromId(fileId, resolution))
                          .WithObjectSize(input.Length)
                          .WithStreamData(input)
                          );

            return result.ObjectName;
        }

        public async Task<string> PutFileAsync(Guid bucketId, Guid id, Stream input)
        {
            await CreateBucketIfNotExistAsync(bucketId);

            var result = await _client.PutObjectAsync(
                                    new Minio.DataModel.Args.PutObjectArgs()
                                    .WithBucket(bucketId.ToString())
                                    .WithObject(GeFileNameFromId(id))
                                    .WithObjectSize(input.Length)
                                    .WithStreamData(input)
                                    );

            return result.ObjectName;
        }

        public async Task<string> GetFileUrlAsync(Guid bucketId, string objectName)
        {
            var result = await _client.PresignedGetObjectAsync(new Minio.DataModel.Args.PresignedGetObjectArgs()
                             .WithBucket(bucketId.ToString())
                             .WithExpiry(604800)
                             .WithObject(objectName));
            return result;
        }

        public async Task<string> GetUrlToUploadFileAsync(Guid bucketId, Guid fileId, VideoResolution resolution)
        {
            var result = await _client.PresignedPutObjectAsync(
                                    new Minio.DataModel.Args.PresignedPutObjectArgs()
                                    .WithBucket(bucketId.ToString())
                                    .WithObject(GeFileNameFromId(fileId, resolution))
                                    .WithExpiry(604800));
            return result;
        }

        public async Task<string> PutFileChunkAsync(Guid bucketId, Guid id, Stream input, VideoChunkUploadingInfo options, VideoResolution resolution = VideoResolution.Original)
        {
            await CreateBucketIfNotExistAsync(bucketId);

            var result = await _client.PutObjectAsync(
                          new Minio.DataModel.Args.PutObjectArgs()
                          .WithBucket(bucketId.ToString())
                          .WithObject(GeFileNameFromId(id, resolution))
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

        private async Task CreateBucketIfNotExistAsync(Guid bucketId)
        {
            if (!await _client.BucketExistsAsync(new Minio.DataModel.Args.BucketExistsArgs().WithBucket(bucketId.ToString())))
            {
                await _client.MakeBucketAsync(new Minio.DataModel.Args.MakeBucketArgs()
                    .WithBucket(bucketId.ToString()));
            }
        }

        public async IAsyncEnumerable<(string Objectname, IDictionary<string, string> Headers)> GetAllBucketObjects(Guid bucketId, VideoChunkUploadingInfo options)
        {
            var a = _client.ListObjectsEnumAsync(new Minio.DataModel.Args.ListObjectsArgs()
                .WithBucket(bucketId.ToString())
                .WithIncludeUserMetadata(true)
                .WithHeaders(new Dictionary<string, string>
                          {
                              { "FileId", options.FileId.ToString() },
                          }));
            await foreach (var item in a)
            {
                yield return (item.Key, item.UserMetadata);
            }
        }

        public async Task<string> PutFileWithResolutionAsync(Guid bucketId, string objectName, Stream input)
        {
            await CreateBucketIfNotExistAsync(bucketId);

            var result = await _client.PutObjectAsync(
                          new Minio.DataModel.Args.PutObjectArgs()
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
    }
}
