namespace Conference.Domain.Services;

public interface IConferenceStateStore
{
    Task AddConnectionAsync(Guid conferenceId, Guid userId, string connectionId);
    Task<bool> RemoveConnectionAsync(Guid conferenceId, Guid userId, string connectionId);
    Task SetCurrentTimeAsync(Guid conferenceId, double time);
    Task SetCurrentTimeIfGreaterAsync(Guid conferenceId, double time);
}
