namespace Profile.Domain.Entities
{
    public class FileMetadata : IProfileEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public long Length { get; set; }
        public string ContentType { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
