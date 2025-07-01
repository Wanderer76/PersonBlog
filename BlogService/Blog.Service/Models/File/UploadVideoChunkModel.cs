using Shared.Services;

namespace Blog.Service.Models.File
{
    public class UploadVideoChunkModel
    {
        public required Guid PostId { get; set; }
        public required long ChunkNumber { get; set; }
        public required long TotalChunkCount { get; set; }
        public required string ContentType { get; set; }
        public required string FileExtension { get; set; }
        public required string FileName { get; set; }
        public required long TotalSize { get; set; }
        public required double Duration { get; set; }
        public required Guid FileId { get; set; }
    }

    public class CreateUploadVideoProgressRequest
    {
        public required Guid PostId { set; get; }
        public required long TotalChunkCount { get; set; }
        public required long TotalSize { get; set; }
    }

    public class UploadVideoProgress : ICacheKey
    {
        public Guid FileId { get; set; }
        public Guid PostId { get; set; }
        public long TotalChunkCount { get; set; }
        public long TotalSize { get; set; }
        public int LastUploadChunkNumber { get; set; }

        public UploadVideoProgress(Guid fileId)
        {
            FileId = fileId;
        }

        public string GetKey() => $"{nameof(UploadVideoProgress)}:{FileId}";
    }
}
