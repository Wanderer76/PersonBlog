using Shared.Models;
using Shared.Services;

namespace Music.Domain.Entities
{
    public class ThumbnailMetadata : FileMetadata, IMusicEntity
    {
        public Guid TrackId { get; set; }

        public ThumbnailMetadata()
        {

        }

        public ThumbnailMetadata(Guid id, string name, string extension, long length, string contentType, string objectName, Guid trackId)

        {
            Id = id;
            Name = name;
            FileExtension = extension;
            TrackId = trackId;
            Length = length;
            ContentType = contentType;
            ObjectName = objectName;
            CreatedAt = DateTimeService.Now();
        }
    }
}
