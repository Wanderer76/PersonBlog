using FileStorage.Service.Models;

namespace Profile.Domain.Entities
{
    public class VideoMetadata : FileMetadata, IProfileEntity
    {
        public VideoResolution Resolution { get; set; }
    }
}
