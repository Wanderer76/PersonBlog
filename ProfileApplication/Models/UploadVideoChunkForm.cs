using Profile.Service.Models.File;

namespace ProfileApplication.Models
{
    public class UploadVideoChunkForm
    {
        public required Guid PostId { get; set; }
        public required long ChunkNumber { get; set; }
        public required long TotalChunkCount { get; set; }
        public required IFormFile ChunkData { get; set; }
        public required string FileExtension { get; set; }
        public required string FileName { get; set; }
        public required long TotalSize { get; set; }
    }
    public static class UploadVideoChunkFormExtensions
    {
        public static UploadVideoChunkModel ToUploadVideoChunkModel(this UploadVideoChunkForm form)
        {
            return new UploadVideoChunkModel
            {
                PostId = form.PostId,
                ChunkNumber = form.ChunkNumber,
                TotalChunkCount = form.TotalChunkCount,
                ContentType = form.ChunkData.ContentType,
                FileExtension = form.FileExtension,
                FileName = form.FileName,
                TotalSize = form.TotalSize,
            };
        }
    }
}