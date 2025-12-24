namespace PlayListService.Services.Models;

public sealed class PlayListListItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
    public int PostCount { get; set; }
}
