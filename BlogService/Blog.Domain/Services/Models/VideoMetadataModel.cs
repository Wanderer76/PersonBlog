namespace Blog.Domain.Services.Models
{
    public class VideoMetadataModel
    {
        public Guid Id { get; }
        public long Length { get; }
        public string ContentType { get; }
        public string ObjectName { get; }
        public double Duration { get; }

        public VideoMetadataModel(Guid id, long length, double duration, string contentType, string objectName)
        {
            Id = id;
            Length = length;
            ContentType = contentType;
            ObjectName = objectName;
            Duration = duration;
        }

        public override bool Equals(object? obj)
        {
            return obj is VideoMetadataModel other &&
                   Id.Equals(other.Id) &&
                   Length == other.Length &&
                   ContentType == other.ContentType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Length, ContentType);
        }
    }
}
