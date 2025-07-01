namespace Blog.Service.Models.File
{
    public class UploadVideoChunkDto
    {
        public required Guid PostId { get; set; }
        public required long ChunkNumber { get; set; }
        public required long TotalChunkCount { get; set; }
        public required Stream ChunkData { get; set; }
    }
}
