using Shared.Models;

namespace Blog.Contracts.Models.File;

[Obsolete("Наверное бесполезно")]
public class PostFileMetadataModel : BaseFileMetadataEntity
{
    public string PreviewUrl { get; }
    public Guid PostId { get; }

    public PostFileMetadataModel(string contentType, long length, string name, DateTimeOffset createdAt, Guid id, string objectName, string previewUrl, Guid postId)
    {
        PreviewUrl = previewUrl;
        PostId = postId;
        ContentType = contentType;
        Id = id;
        Length = length;
        Name = name;
        CreatedAt = createdAt;
        ObjectName = objectName;
    }
}
