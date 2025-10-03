namespace Music.Contract.Models;

public class TrackFileInfo
{
    public string Url { get; }
    public double Duration { get; }

    public TrackFileInfo(string url, double duration)
    {
        Url = url;
        Duration = duration;
    }
}
