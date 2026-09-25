using MessageBus;

namespace Authentication.Contract.Events
{
    [EventPublish(Exchange = "user-events", RoutingKey = "profile.register")]
    public class ProfileRegisterEvent
    {
        public string? Name { get; set; }
        public string UserName { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? PhotoUrl { get; set; }

        public ProfileRegisterEvent(
            string? name,
            string userName,
            Guid userId,
            DateTimeOffset createdAt,
            string? photoUrl = null)
        {
            Name = name;
            UserName = userName;
            UserId = userId;
            CreatedAt = createdAt;
            PhotoUrl = photoUrl;
        }
    }
}
