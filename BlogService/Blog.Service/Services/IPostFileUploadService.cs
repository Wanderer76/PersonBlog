//namespace Blog.Service.Services;

//public interface IPostFileUploadService
//{
//    Task CreateTempStorage(Guid postId);
//    Task RemoveTempStorage(Guid postId);
//    Task UploadChunkFile(Guid postId, Stream input, VideoChunkUploadingInfo options);
//    IAsyncEnumerable<(string Objectname, IDictionary<string, string> Headers)> GetAllBucketObjects(Guid postId, VideoChunkUploadingInfo options);
//}

//public class VideoChunkUploadingInfo
//{
//    public Guid FileId { get; set; }
//    public long ChunkNumber { get; set; }
//}