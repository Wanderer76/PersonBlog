namespace Conference.Domain.Services
{
    public interface IConferenceHub
    {
        Task OnConferenceConnect(string message);
    }
}
