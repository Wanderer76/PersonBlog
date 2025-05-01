using Conference.Domain.Models;

namespace Conference.Domain.Services
{
    public interface IConferenceChatService
    {
        Task<MessageModel> CreateMessageAsync(Guid sessionId, CreateMessageForm messageForm);
        Task<IReadOnlyList<MessageModel>> GetLastMessagesAsync(Guid conferenceId, int offset,int limit);
    }
}
