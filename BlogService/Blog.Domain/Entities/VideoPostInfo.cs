using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blog.Domain.Entities;

public sealed class VideoPostInfo : IBlogEntity
{
    [Key]
    public Guid Id { get; set; }

    public string? Description { get; set; }
    public Guid? PreviewId { get; set; }
    public Guid? VideoFileId { get; set; }

    [ForeignKey(nameof(Id))]
    public Post Post { get; private set; } = null!;


    [ForeignKey(nameof(VideoFileId))]
    public VideoFile? VideoFile { get; set; }

    [ForeignKey(nameof(PreviewId))]
    public PostFile? PreviewFile { get; set; }
    public List<PostCategory> PostCategories { get; set; } = [];
}
