using System.ComponentModel.DataAnnotations.Schema;

namespace Profile.Domain.Entities
{
    public class FileMetadata : IProfileEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public required string FileExtension { get; set; }
        public long Length { get; set; }
        public string ContentType { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid PostId { get; set; }
        public string ObjectName { get; set; }

        [ForeignKey(nameof(PostId))]
        public Post Post { get; set; }
    }

    public class FileTypePrefix
    {
        public const string Video = "video";
        public const string Photo = "image";
    }
}
