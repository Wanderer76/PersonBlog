namespace Blog.Service.Models.File
{
    public class FileMetadataModel
    {
        public Guid Id { get; }
        public string ObjectName { get; }
        public string ContentType { get; }
        public long Length { get; }
        public string Name { get; }
        public DateTimeOffset CreatedAt { get; }

        public FileMetadataModel(string contentType, long length, string name, DateTimeOffset createdAt, Guid id, string objectName)
        {
            ContentType = contentType;
            Length = length;
            Name = name;
            CreatedAt = createdAt;
            Id = id;
            ObjectName = objectName;
        }
    }
}
