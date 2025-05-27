namespace Blog.Domain.Services.Models.Playlist;

public class PlayListItemAddRequest
{
    public required Guid PlayListId { get; set; }

    public required List<PlayListAddItem> Items { get; set; }
}

public class PlayListAddItem
{
    public required Guid PostId { get; set; }
    public int? Position { get; set; }
}

public class ChangePostPositionRequest
{
    public required Guid PlaylistId { get; set; }
    public required Guid PostId { get; set; }
    public required int Destination { get; set; }
}

public class PlayListItemRemoveRequest
{
    public required Guid PlayListId { get; set; }
    public required Guid PostId { get; set; }
}

