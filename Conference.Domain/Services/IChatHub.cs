namespace Conference.Domain.Services
{
    public interface IChatHub
    {
        Task OnConferenceConnect(string message);
    }
}
