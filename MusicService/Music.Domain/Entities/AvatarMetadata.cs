using Shared.Models;
using Shared.Services;

namespace Music.Domain.Entities
{
    public class AvatarMetadata : FileMetadata, IMusicEntity
    {
        public AvatarMetadata() { }

        public AvatarMetadata(Guid id, string name, string extension, long length, string contentType, string objectName)
        {
            Id = id;
            Name = name;
            FileExtension = extension;
            Length = length;
            ContentType = contentType;
            ObjectName = objectName;
            CreatedAt = DateTimeService.Now();
        }
    }
}
