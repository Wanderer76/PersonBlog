namespace Infrastructure.Services
{
    public interface IFileStorage : IDisposable
    {
        [Obsolete]
        Task<string> PutFileAsync(Guid userId, Guid id, Stream input);
        [Obsolete]
        Task<string> PutFileInBucketAsync(Guid bucketId, Guid id, Stream input);
        Task<string> PutFileAsync(Guid bucketId, string objectName, Stream input);
        Task<string> PutTempFileAsync(Guid bucketId, string objectName, Stream input);
        Task<string> PutFileChunkAsync(Guid bucketId, Guid id, Stream input, VideoChunkUploadingInfo options);
        Task<string> GetFileUrlAsync(Guid bucketId, string objectName);

        [Obsolete]
        Task<string> GetUrlToUploadFileAsync(Guid userId, Guid fileId);
        Task ReadFileAsync(Guid bucketId, string objectName, Stream output);
        Task<long> ReadFileByChunksAsync(Guid bucketId, string objectName, long offset, long length, Stream output);
        Task RemoveFileAsync(Guid bucketId, string objectName);
        Task RemoveBucketAsync(string bucketId);
        IAsyncEnumerable<(string Objectname, IDictionary<string, string> Headers)> GetAllBucketObjects(Guid bucketId, VideoChunkUploadingInfo options);
    }

    public class VideoChunkUploadingInfo
    {
        public Guid FileId { get; set; }
        public long ChunkNumber { get; set; }
    }
}
