using Shared.Models;

namespace Blog.Domain.Entities;

public sealed class PostFile : FileMetadata, IBlogEntity
{
    public Guid PostId { get; set; }
}
