using Shared.Models;

namespace Blog.Domain.Entities;

public sealed class PostFile : BaseFileMetadataEntity, IBlogEntity
{
    public Guid PostId { get; set; }
}
