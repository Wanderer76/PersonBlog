using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blog.Domain.Entities;

public sealed class TextPostInfo : IBlogEntity
{
    [Key]
    public Guid Id { get; private set; }

    public string? Text { get; private set; }

    [ForeignKey(nameof(Id))]
    public Post Post { get; private set; } = null!;

    public List<PostFile> Files { get; private set; } = [];

    public TextPostInfo()
    {

    }

    public TextPostInfo(Guid id, string? text, List<PostFile> files)
    {
        Id = id;
        Text = string.IsNullOrWhiteSpace(text) ? null : text;
        Files = files;
    }

    public void UpdateFiles(IEnumerable<PostFile> files)
    {
        foreach (var file in files)
        {
            if (!this.Files.Any(x => x.Id == file.Id))
            {
                this.Files.Add(file);
            }
        }
    }
    public void RemoveFiles(IEnumerable<Guid> ids)
    {
        var filesToRemove = Files.Where(x => ids.Contains(x.Id));

        foreach (var file in filesToRemove)
        {
            Files.Remove(file);
        }
    }
}
