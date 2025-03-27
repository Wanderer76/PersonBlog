namespace Conference.Domain.Services
{
    public interface IConferenceHub
    {
        Task OnConferenceConnect(string message);
        Task OnTimeSeek(double time);
        Task OnPause(double time);
        Task OnPlay();
    }
}
