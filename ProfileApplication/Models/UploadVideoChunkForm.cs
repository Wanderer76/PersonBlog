namespace ProfileApplication.Models
{
    public class UploadVideoChunkForm
    {
        public required Guid PostId { get; set; }
        public required long ChunkNumber { get; set; }
        public required long TotalChunkCount { get; set; }
        public required byte[] ChunkData { get; set; }
    }
}