namespace Conference.Domain.Models
{
    public class MessageModel
    {
        public string CreatorUserName { get; }
        public string Message { get; }
        public DateTime CreatedAt { get; }
        public Guid? ReplyToMessage { get; }

        public MessageModel(string creatorUserName, string message, DateTime createdAt, Guid? replyToMessage)
        {
            CreatorUserName = creatorUserName;
            Message = message;
            CreatedAt = createdAt;
            ReplyToMessage = replyToMessage;
        }
    }
}
