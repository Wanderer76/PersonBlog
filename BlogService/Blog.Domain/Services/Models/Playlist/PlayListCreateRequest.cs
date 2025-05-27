namespace Blog.Domain.Services.Models.Playlist;

public class PlayListCreateRequest
{
    public required string Title { get; set; }
    //public required Guid BlogId { get; set; }
    public string? ThumbnailId { get; set; }
    public required List<Guid> PostIds { get; set; } = [];
}

public class PlayListUpdateRequest
{
    public required Guid PlayListId {  get; set; }
    public string? Title { get; set; }
    public string? ThumbnailId { get; set; }
}