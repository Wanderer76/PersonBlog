namespace VideoProcessing.Cli.Hubs
{
    public interface IVideoProcessHub
    {
        Task OnVideoConvertProgress(string title, double percent);
    }
}
