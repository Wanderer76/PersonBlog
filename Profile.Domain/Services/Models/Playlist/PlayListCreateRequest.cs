namespace Blog.Domain.Services.Models.Playlist;

public class PlayListCreateRequest
{
    public required string Title { get; set; }
    //public required Guid BlogId { get; set; }
    public string? ThumbnailId { get; set; }
    public List<Guid> PostIds { get; set; } = [];
}

