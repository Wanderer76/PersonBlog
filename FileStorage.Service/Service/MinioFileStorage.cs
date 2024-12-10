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
    }
}
