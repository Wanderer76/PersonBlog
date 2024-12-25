namespace ProfileApplication.Models
{
    public class UploadVideoFrom
    {
        public required IFormFile Chunk { get; set; }
        public required long ChunkNumber { get; set; }
    }
}
