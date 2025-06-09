namespace ViewReacting.Domain.Models;

public class HistoryViewItem
{
    public Guid Id { get; set; }
    public DateTime LastWatched { get; set; }
    public Guid PostId { get; set; }
    public double WatchedTime { get; set; }
}


public class ReactionHistoryViewItem
{
    public DateTime? LastWatched { get; set; }
    public Guid PostId { get; set; }
    public bool? IsLike { get; set; }
    public bool HasSubscription { get; set; }
    public double? WatchedTime { get; set; }
}


