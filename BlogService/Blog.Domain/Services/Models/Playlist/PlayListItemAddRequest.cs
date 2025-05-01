namespace Blog.Domain.Services.Models.Playlist;

public class PlayListItemAddRequest
{
    //public required Guid BlogId { get; set; }
    public required Guid PlayListId { get; set; }
    public required Guid PostId { get; set; }
    public int? Position { get; set; }
}

public class PlayListItemRemoveRequest
{
    public required Guid PlayListId { get; set; }
    public required Guid PostId { get; set; }
}

