namespace ViewReacting.Domain.Models;

public class HistoryViewItem
{
    public DateTime LastWatched { get; set; }
    public Guid PostId { get; set; }
    public double WatchedTime {  get; set; }
}


