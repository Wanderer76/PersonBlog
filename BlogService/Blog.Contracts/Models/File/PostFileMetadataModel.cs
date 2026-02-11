namespace Blog.Contracts.Models.File
{
    public class PostFileMetadataModel : FileMetadataModel
    {
        public string PreviewUrl { get; }
        public Guid PostId { get; }

        public PostFileMetadataModel(string contentType, long length, string name, DateTimeOffset createdAt, Guid id, string objectName, string previewUrl, Guid postId)
            : base(contentType, length, name, createdAt, id, objectName)
        {
            PreviewUrl = previewUrl;
            PostId = postId;
        }
    }
}
