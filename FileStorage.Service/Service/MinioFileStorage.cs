using Minio;

namespace FileStorage.Service.Service
{
    internal class MinioFileStorage : IFileStorage
    {
        private readonly IMinioClient _client;
        private readonly FileStorageOptions _minioOptions;

        public MinioFileStorage(FileStorageOptions options)
        {
            _minioOptions = options;
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

        public async Task<long> ReadFileByChunksAsync(Guid userId, Guid id, long offset, long length, byte[] buffer)
        {
            var size = 0L;
            var a = await _client.GetObjectAsync(new Minio.DataModel.Args.GetObjectArgs()
                 .WithBucket(userId.ToString())
                 .WithObject(GeFileNameFromId(id))
                   .WithOffsetAndLength(offset, length)
                 .WithCallbackStream(async stream =>
                 {
                     int bytesRead;
                     while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                     {
                         size += bytesRead;
                         // Здесь можно обрабатывать прочитанные данные
                     }
                     stream.Dispose();
                 }));
            return size;
        }


        public async Task<long> ReadFileByChunksAsync(Guid userId, Guid id, long offset, long length, Stream buffer)
        {
            var size = 0L;
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
    }
}
