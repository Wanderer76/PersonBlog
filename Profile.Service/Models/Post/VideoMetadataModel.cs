namespace Profile.Service.Models.Post
{
    public class VideoMetadataModel
    {
        public Guid Id { get; }
        public long Length { get; }
        public string ContentType { get; }

        public VideoMetadataModel(Guid id, long length, string contentType)
        {
            Id = id;
            Length = length;
            ContentType = contentType;
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
