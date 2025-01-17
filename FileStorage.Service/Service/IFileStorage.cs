using FileStorage.Service.Models;

namespace FileStorage.Service.Service
{
    public interface IFileStorage
    {
        Task<string> PutFileAsync(Guid userId, Guid id, Stream input);
        Task<string> PutFileWithResolutionAsync(Guid bucketId, Guid id, Stream input, VideoResolution resolution = VideoResolution.Original);
        Task<string> PutFileWithResolutionAsync(Guid bucketId, string objectName, Stream input);
        Task<string> PutFileChunkAsync(Guid bucketId, Guid id, Stream input, VideoChunkUploadingInfo options,VideoResolution resolution = VideoResolution.Original);
        Task<string> GetFileUrlAsync(Guid userId, string objectName);
        Task<string> GetUrlToUploadFileAsync(Guid userId, Guid fileId, VideoResolution resolution = VideoResolution.Original);
        Task ReadFileAsync(Guid userId, string objectName, Stream output);
        Task<long> ReadFileByChunksAsync(Guid userId, string objectName, long offset, long length, Stream output);
        Task RemoveFileAsync(Guid bucketId, string objectName);
        IAsyncEnumerable<(string Objectname, IDictionary<string, string> Headers)> GetAllBucketObjects(Guid bucketId, VideoChunkUploadingInfo options);
    }

    public class VideoChunkUploadingInfo
    {
        public Guid FileId { get; set;}
        public long ChunkNumber { get; set;}
    }
}
