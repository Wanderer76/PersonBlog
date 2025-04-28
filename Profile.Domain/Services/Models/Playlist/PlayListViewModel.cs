using Blog.Domain.Services.Models;

namespace Blog.Domain.Services.Models.Playlist;

public class PlayListViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string ThumbnailUrl { get; set; }
    public List<Guid> Posts { get; set; }
}
