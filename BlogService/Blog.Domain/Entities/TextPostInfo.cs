using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blog.Domain.Entities;

public sealed class TextPostInfo : IBlogEntity
{
    [Key]
    public Guid Id { get; private set; }

    public string Text { get; private set; }

    [ForeignKey(nameof(Id))]
    public Post Post { get; private set; } = null!;

    public List<PostFile> Files { get; private set; } = [];

    public TextPostInfo()
    {
        
    }

    public TextPostInfo(Guid id, string text, List<PostFile> files)
    {
        Id = id;
        Text = text;
        Files = files;
    }
}
