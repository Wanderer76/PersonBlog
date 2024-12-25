using Minio;

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

        public async Task ReadFileAsync(Guid userId, Guid id, Stream output)
        {
            await _client.GetObjectAsync(new Minio.DataModel.Args.GetObjectArgs()
                .WithBucket(userId.ToString())
                .WithObject(GeFileNameFromId(id))
                .WithCallbackStream(async stream => await stream.CopyToAsync(output)));
        }

        public async Task PutFileAsync(Guid userId, Guid id, Stream input)
        {
            if (!await _client.BucketExistsAsync(new Minio.DataModel.Args.BucketExistsArgs().WithBucket(userId.ToString())))
            {
                await _client.MakeBucketAsync(new Minio.DataModel.Args.MakeBucketArgs().WithBucket(userId.ToString()));
            }

            await _client.PutObjectAsync(
                       new Minio.DataModel.Args.PutObjectArgs()
                       .WithBucket(userId.ToString())
                       .WithObject(GeFileNameFromId(id))
                       .WithObjectSize(input.Length)
                       .WithStreamData(input)
                       );
        }

        public Task RemoveFileAsync(Guid userId, Guid id)
        {
            throw new NotImplementedException();
        }

        private string GeFileNameFromId(Guid fileId)
        {
            return $"video-{fileId}";
        }

        public async Task<long> ReadFileByChunksAsync(Guid userId, Guid fileId, long offset, long length, byte[] buffer)
        {
            var size = 0L;
            await _client.GetObjectAsync(new Minio.DataModel.Args.GetObjectArgs()
                 .WithBucket(userId.ToString())
                 .WithObject(GeFileNameFromId(fileId))
                 .WithOffsetAndLength(offset, length)
                 .WithCallbackStream(stream =>
                 {
                     int bytesRead;
                     while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                     {
                         size += bytesRead;
                     }
                 }));
            return size;
        }


        public async Task<long> ReadFileByChunksAsync(Guid userId, Guid id, long offset, long length, Stream buffer)
        {
            await _client.GetObjectAsync(new Minio.DataModel.Args.GetObjectArgs()
                 .WithBucket(userId.ToString())
                 .WithObject(GeFileNameFromId(id))
                 .WithOffsetAndLength(offset, length)
                 .WithCallbackStream(stream =>
                 {
                     stream.CopyTo(buffer);
                 }));
            return buffer.Length;
        }

        public async Task<long> ReadFileByChunksAsync(Guid userId, Guid id, long offset, long length, string resolution, Stream output)
        {
            await _client.GetObjectAsync(new Minio.DataModel.Args.GetObjectArgs()
                        .WithBucket(GeFileNameFromId(id))
                        .WithObject(resolution)
                        .WithOffsetAndLength(offset, length)
                        .WithCallbackStream(stream =>
                        {
                            stream.CopyTo(output);
                        }));

            return output.Length;
        }

        public async Task PutFileAsync(Guid userId, Guid fileId, string resolution, Stream input)
        {
            if (!await _client.BucketExistsAsync(new Minio.DataModel.Args.BucketExistsArgs().WithBucket(GeFileNameFromId(fileId))))
            {
                await _client.MakeBucketAsync(new Minio.DataModel.Args.MakeBucketArgs().WithBucket(GeFileNameFromId(fileId)));
            }

            await _client.PutObjectAsync(
                       new Minio.DataModel.Args.PutObjectArgs()
                       .WithBucket(GeFileNameFromId(fileId))
                       .WithObject(resolution)
                       .WithObjectSize(input.Length)
                       .WithStreamData(input)
                       );
        }
    }
}
