
namespace Profile.Service.Models.File
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
    }

}
